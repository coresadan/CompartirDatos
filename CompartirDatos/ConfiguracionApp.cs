using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Text.Json;
using Serilog;

namespace CompartirDatos
{
    public class ConfiguracionApp
    {
        public static ConfiguracionApp misAjustes { get; set; } = CargarConfiguracion();

        public string UltimaRutaArchivo { get; set; } = "";
        public string UltimaMaquinaSeleccionada { get; set; } = "";
        public string UltimoUsuario { get; set; }

        public static string RutaCarpetaFicheros => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Control Fabrica");

        public string NombreArchivoLog { get; set; } = "Historial_maquina.log";
        public string UltimaResolucion { get; set; }

        public List<CaracteristicasDePiezas> ListaPiezasTerminadas { get; set; } = new List<CaracteristicasDePiezas>();

        public string RutaConfig()
        {
            string carpeta = RutaCarpetaFicheros;
            Directory.CreateDirectory(carpeta);
            return Path.Combine(carpeta, "config.json");
        }


       

        public static ConfiguracionApp CargarConfiguracion()
        {
            ConfiguracionApp temp = new ConfiguracionApp();
            string ruta = temp.RutaConfig();

            try
            {
                if (File.Exists(ruta))
                {
                    string jsonRecuperado = File.ReadAllText(ruta);
                    ConfiguracionApp objetoCargado = JsonSerializer.Deserialize<ConfiguracionApp>(jsonRecuperado) ?? new ConfiguracionApp();
                    Log.Information("SISTEMA💾 Memoria recuperada con éxito.");
                    return objetoCargado;
                }
                else
                {
                    Log.Warning("SISTEMA💾 No hay configuración previa. Se inicia una nueva.");
                    return temp;
                }
            }
            catch (Exception ex)
            {
                Log.Error("ERROR❌ al cargar memoria: " + ex.Message);
                return temp;
            }
        }

        public void GuardarConfiguracionEnDisco()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(RutaConfig(), json);

                Log.Information("SISTEMA💾 Configuración guardada en el archivo config.json");
            }
            catch (Exception ex)
            {
                Log.Error($"ERROR❌ No se pudo guardar la configuración: {ex.Message}");
            }
        }




    }
}