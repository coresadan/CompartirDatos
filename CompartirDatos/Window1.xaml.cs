using CsvHelper;
using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;

namespace CompartirDatos
{
    /// <summary>
    /// Lógica de interacción para Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        private void AbrirArchivoLocalClick(object sender, RoutedEventArgs e)
        {
            var ventanaPrincipal = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (ventanaPrincipal == null) return;

            // 1. ESCUDO: Evitamos cargar si ya hay una pieza en curso (seguridad básica)
            if (ventanaPrincipal._idPiezaEnviadaActual != null)
            {
                MessageBox.Show("🛑 No puedes cargar una lista mientras hay una pieza activa.",
                                "Santos - Control", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpenFileDialog archivo = new OpenFileDialog();
            archivo.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";

            if (archivo.ShowDialog() == true)
            {
                string rutaArchivo = archivo.FileName;

                ventanaPrincipal.ArrastrarElArchivoLabel.Visibility = Visibility.Hidden;
                ventanaPrincipal.FuncionImportarArchivo(rutaArchivo);

                var primeraPieza = ventanaPrincipal.listaDePiezas.FirstOrDefault(p => !p.EstaTerminada && !p.Datos.Falta);

                if (primeraPieza != null && ventanaPrincipal._emisor.EstaConectado)
                {
                    _ = ventanaPrincipal.EnviarSiguientePiezaDisponible();
                    Log.Information($"💾 LOCAL: Pieza {primeraPieza.Nombre} enviada al trabajador conectado.");
                }
                else
                {
                    ventanaPrincipal._idPiezaEnviadaActual = null;
                    Log.Information("ℹ️ LOCAL: Cargado en modo oficina (sin conexión con fábrica).");
                }

                Close();
            }
        }

        private void AbrirLibreriaClick(object sender, RoutedEventArgs e)
        {
            var ventanaPrincipal = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (ventanaPrincipal == null) return;

            // 1. ESCUDO DE SEGURIDAD (Solo si ya hay algo en marcha)
            if (ventanaPrincipal._idPiezaEnviadaActual != null)
            {
                MessageBox.Show("🛑 No puedes cambiar la lista mientras el trabajador tiene una pieza abierta.",
                                "Conflicto de Producción", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            OpenFileDialog archivo = new OpenFileDialog();
            if (archivo.ShowDialog() == true)
            {
                // 2. LIMPIEZA PREVIA
                ventanaPrincipal.listaDePiezas.Clear();
                ventanaPrincipal._idPiezaEnviadaActual = null;

                // 3. IMPORTACIÓN
                ventanaPrincipal.FuncionImportarArchivo(archivo.FileName);
                ventanaPrincipal.ArrastrarElArchivoLabel.Visibility = Visibility.Hidden;

                // 4. REINICIO DE CICLO INTELIGENTE (Aquí aplicamos la lógica moderna)
                var primeraPieza = ventanaPrincipal.listaDePiezas.FirstOrDefault(p => !p.EstaTerminada && !p.Datos.Falta);

                // Solo bloqueamos si el trabajador está conectado. Si no, oficina libre.
                if (primeraPieza != null && ventanaPrincipal._emisor.EstaConectado)
                {
                    ventanaPrincipal._idPiezaEnviadaActual = primeraPieza.Id;

                    _ = ventanaPrincipal.EnviarSiguientePiezaDisponible();
                    Log.Information($"📚 LIBRERÍA: Pieza {primeraPieza.Nombre} enviada al trabajador conectado.");
                }
                else
                {
                    // Reset de seguridad: Si no hay conexión, permitimos editar todo
                    ventanaPrincipal._idPiezaEnviadaActual = null;
                    Log.Information("ℹ️ LIBRERÍA: Cargada en modo edición (Trabajador desconectado).");
                }

                Close();
            }
        }

        private void AbrirWebClick(object sender, RoutedEventArgs e)
        {
            var ventanaPrincipal = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (ventanaPrincipal == null) return;

            if (ventanaPrincipal._idPiezaEnviadaActual != null)
            {
                MessageBox.Show("No puedes acceder al servidor para cambiar la lista mientras hay una pieza en curso.",
                                "Conflicto de Producción", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var abrirVentanaAPI = new VentanaServidorAPI();
            abrirVentanaAPI.Owner = this;
            abrirVentanaAPI.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            Log.Information($"ACCESO AL SERVIDOR👤🕵️ Acceso a listados.");
            abrirVentanaAPI.ShowDialog();
        }

        private void botonCancelarClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
