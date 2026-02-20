using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System;
using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompartirDatos
{
    public static class DatabaseService
    {
        private static string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Control Fabrica", "TrazaWood.db");
        private static string connectionString = $"Data Source={dbPath};";

        public static void InicializarBaseDeDatos()
        {
            string directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // Activar modo WAL para evitar que el programa se congele al escribir
                using (var commandWal = new SqliteCommand("PRAGMA journal_mode=WAL;", connection))
                {
                    commandWal.ExecuteNonQuery();
                }

                var query = @"CREATE TABLE IF NOT EXISTS Piezas (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Nombre TEXT, Color TEXT, Largo REAL, Ancho REAL,
                                EstaTerminada INTEGER DEFAULT 0, 
                                Falta INTEGER DEFAULT 0, 
                                Error INTEGER DEFAULT 0
                             );";
                using (var cmd = new SqliteCommand(query, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
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
                        var query = "INSERT INTO Piezas (Nombre, Color, Largo, Ancho) VALUES (@nom, @col, @lar, @anc)";
                        using (var cmd = new SqliteCommand(query, connection, trans))
                        {
                            cmd.Parameters.AddWithValue("@nom", p.Nombre ?? "");
                            cmd.Parameters.AddWithValue("@col", p.Color ?? "");
                            cmd.Parameters.AddWithValue("@lar", p.Largo);
                            cmd.Parameters.AddWithValue("@anc", p.Ancho);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
        }

        public static List<CaracteristicasDePiezas> CargarPiezasDesdeBD()
        {
            var lista = new List<CaracteristicasDePiezas>();
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT Id, Nombre, Color, Largo, Ancho, EstaTerminada, Falta, Error FROM Piezas";
                using (var cmd = new SqliteCommand(query, connection))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        lista.Add(new CaracteristicasDePiezas
                        {
                            Id = r.GetInt32(0),
                            Nombre = r.GetString(1),
                            Color = r.GetString(2),
                            Largo = r.GetDecimal(3),
                            Ancho = r.GetDecimal(4),
                            EstaTerminada = r.GetInt32(5) == 1,
                            Datos = new CaracteristicasDePiezas2
                            {
                                Falta = r.GetInt32(6) == 1,
                                Error = r.GetInt32(7) == 1
                            }
                        });
                    }
                }
            }
            return lista;
        }

        public static void ActualizarEstadoPieza(CaracteristicasDePiezas p)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                // AQUÍ ESTÁ EL TRUCO: Buscamos por ID, no por nombre
                var query = "UPDATE Piezas SET EstaTerminada = @term, Falta = @fal, Error = @err WHERE Id = @id";
                using (var cmd = new SqliteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@term", p.EstaTerminada ? 1 : 0);
                    cmd.Parameters.AddWithValue("@fal", (p.Datos?.Falta == true) ? 1 : 0);
                    cmd.Parameters.AddWithValue("@err", (p.Datos?.Error == true) ? 1 : 0);
                    cmd.Parameters.AddWithValue("@id", p.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void BorrarBaseDeDatos()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var query = "DELETE FROM Piezas; DELETE FROM sqlite_sequence WHERE name='Piezas';";
                using (var cmd = new SqliteCommand(query, connection)) { cmd.ExecuteNonQuery(); }
            }
        }
    }
}