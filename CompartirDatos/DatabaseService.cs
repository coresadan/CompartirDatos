using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Collections.Generic;

namespace CompartirDatos
{
    public static class DatabaseService
    {
        private static string dbPath = @"C:\pruebas\BDPiezas.s3db";
        private static string connectionString = $"Data Source={dbPath};";

        public static void InicializarBaseDeDatos()
        {
            string directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var query = @"CREATE TABLE IF NOT EXISTS RegistroDePiezas (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Nombre TEXT, Color TEXT, Largo REAL, Ancho REAL,
                                EstaTerminada INTEGER DEFAULT 0, 
                                Falta INTEGER DEFAULT 0, Error INTEGER DEFAULT 0,
                                Piezaurgente INTEGER DEFAULT 0,
                                Estado TEXT DEFAULT 'Pendiente'
                             );";

                using (var cmd = new SqliteCommand(query, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<CaracteristicasDePiezas> CargarPiezasDesdeBD()
        {
            var lista = new List<CaracteristicasDePiezas>();
            InicializarBaseDeDatos();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM RegistroDePiezas";

                using (var cmd = new SqliteCommand(query, connection))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        try
                        {
                            var pieza = new CaracteristicasDePiezas();

                            // 1. Datos básicos
                            pieza.Id = ColumnaExiste(r, "Id") ? r.GetInt32(r.GetOrdinal("Id")) : 0;
                            pieza.Nombre = ColumnaExiste(r, "Nombre") ? (r.IsDBNull(r.GetOrdinal("Nombre")) ? "" : r.GetString(r.GetOrdinal("Nombre"))) : "";
                            pieza.Color = ColumnaExiste(r, "Color") ? (r.IsDBNull(r.GetOrdinal("Color")) ? "" : r.GetString(r.GetOrdinal("Color"))) : "";
                            pieza.Largo = ColumnaExiste(r, "Largo") ? Convert.ToDecimal(r.GetValue(r.GetOrdinal("Largo"))) : 0;
                            pieza.Ancho = ColumnaExiste(r, "Ancho") ? Convert.ToDecimal(r.GetValue(r.GetOrdinal("Ancho"))) : 0;

                            // 2. Booleano de Urgencia (Crítico para el retroceso)
                            if (ColumnaExiste(r, "Piezaurgente"))
                            {
                                // SQLite devuelve 1 o 0, lo convertimos a true/false
                                pieza.Piezaurgente = r.GetInt32(r.GetOrdinal("Piezaurgente")) == 1;
                            }

                            // 3. Booleano de Terminada
                            if (ColumnaExiste(r, "EstaTerminada"))
                            {
                                pieza.EstaTerminada = r.GetInt32(r.GetOrdinal("EstaTerminada")) == 1;
                            }

                            // 4. Estado (Texto) - UNA SOLA VEZ
                            if (ColumnaExiste(r, "Estado"))
                            {
                                pieza.Estado = r.IsDBNull(r.GetOrdinal("Estado")) ? "Pendiente" : r.GetString(r.GetOrdinal("Estado"));
                            }
                            else
                            {
                                pieza.Estado = "Pendiente";
                            }

                            // 5. Sub-objeto Datos
                            pieza.Datos.Falta = ColumnaExiste(r, "Falta") && r.GetInt32(r.GetOrdinal("Falta")) == 1;
                            pieza.Datos.Error = ColumnaExiste(r, "Error") && r.GetInt32(r.GetOrdinal("Error")) == 1;

                            lista.Add(pieza);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error en fila individual: {ex.Message}");
                        }
                    }
                }
            }
            return lista;
        }

        // FUNCIÓN MAESTRA: Evita el error "No such column"
        private static bool ColumnaExiste(SqliteDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static void GuardarPiezasImportadas(List<CaracteristicasDePiezas> piezas)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    foreach (var p in piezas)
                    {
                        var query = "INSERT INTO RegistroDePiezas (Nombre, Color, Largo, Ancho, Piezaurgente, Estado) VALUES (@nom, @col, @lar, @anc, @urg, @est)";
                        using (var cmd = new SqliteCommand(query, connection, trans))
                        {
                            cmd.Parameters.AddWithValue("@nom", p.Nombre ?? "");
                            cmd.Parameters.AddWithValue("@col", p.Color ?? "");
                            cmd.Parameters.AddWithValue("@lar", p.Largo);
                            cmd.Parameters.AddWithValue("@anc", p.Ancho);
                            cmd.Parameters.AddWithValue("@urg", p.Piezaurgente ? 1 : 0);
                            cmd.Parameters.AddWithValue("@est", p.Piezaurgente ? "Urgente" : "Pendiente");
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
        }
    }
}