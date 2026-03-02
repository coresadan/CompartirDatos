using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Data.Sqlite;
using Serilog;

namespace CompartirDatos
{
    public partial class VentanaSeleccionarSemana : Window
    {
        public VentanaSeleccionarSemana()
        {
            InitializeComponent();
            CargarSemanas();
        }

        private void CargarSemanas()
        {
            try
            {
                string rutaDb = @"C:\pruebas\BDPiezas.s3db";
                var semanas = new List<string>();

                using (var conexion = new SqliteConnection($"Data Source={rutaDb}"))
                {
                    conexion.Open();
                    var comando = new SqliteCommand("SELECT DISTINCT Semana FROM RegistroDePiezas", conexion);

                    using (var reader = comando.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                                semanas.Add(reader.GetString(0));
                        }
                    }
                }

                cbSemanas.ItemsSource = semanas;
                if (semanas.Count > 0) cbSemanas.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.Error($"Error oficina leyendo DB: {ex.Message}");
                MessageBox.Show($"Error al acceder a la base de datos: {ex.Message}");
            }
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            if (cbSemanas.SelectedItem != null)
            {
                string semanaElegida = cbSemanas.SelectedItem.ToString();
                var principal = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

                if (principal != null)
                {
                    try
                    {
                        string rutaDb = @"C:\pruebas\BDPiezas.s3db";
                        var piezasDeLaSemana = new List<CaracteristicasDePiezas>();

                        using (var conexion = new SqliteConnection($"Data Source={rutaDb}"))
                        {
                            conexion.Open();
                            var consulta = "SELECT Id, Nombre, Color, Largo, Ancho, Estado FROM RegistroDePiezas WHERE Semana = @sem";
                            var comando = new SqliteCommand(consulta, conexion);
                            comando.Parameters.AddWithValue("@sem", semanaElegida);

                            using (var reader = comando.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var p = new CaracteristicasDePiezas
                                    {
                                        Id = reader.GetInt32(0),
                                        Nombre = reader.GetString(1),
                                        Color = reader.GetString(2),
                                        Largo = reader.GetDecimal(3),
                                        Ancho = reader.GetDecimal(4),
                                        Estado = reader.GetString(5),
                                        EstaTerminada = reader.GetString(5) == "Terminado",

                                        Datos = new CaracteristicasDePiezas2
                                        {
                                            Falta = reader.GetString(5) == "FALTA/RECHAZO"
                                        }
                                    };
                                    piezasDeLaSemana.Add(p);
                                }
                            }
                        }

                        principal.listaDePiezas.Clear();
                        foreach (var pieza in piezasDeLaSemana)
                        {
                            principal.listaDePiezas.Add(pieza);
                        }

                        principal.ArrastrarElArchivoLabel.Visibility = Visibility.Hidden;

                        _ = principal.SincronizarConAPI();

                        Log.Information($"📅 Cargada semana {semanaElegida} con {piezasDeLaSemana.Count} piezas.");
                        this.DialogResult = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al cargar los detalles: " + ex.Message);
                    }
                }
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}