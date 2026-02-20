using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; 
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace CompartirDatos
{
    public class GestionUsuarios
    {
        public static ObservableCollection<Usuario> ListaUsuarios { get; set; } = new ObservableCollection<Usuario>();

        public static string RutaArchivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Control Fabrica", "usuarios.json");

        public static async Task CargarUsuariosSincronizados()
        {
            try
            {
                using (var cliente = new HttpClient())
                {
                    cliente.Timeout = TimeSpan.FromSeconds(5);
                    var respuesta = await cliente.GetAsync("http://localhost:5106/Usuarios");

                    if (respuesta.IsSuccessStatusCode)
                    {
                        var listaServidor = await respuesta.Content.ReadFromJsonAsync<List<Usuario>>();
                        if (listaServidor != null)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                ListaUsuarios.Clear();
                                foreach (var u in listaServidor) ListaUsuarios.Add(u);
                            });
                            Log.Information("🌐 Usuarios cargados desde API.");
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("⚠️ Fallo API, cargando local...");
                System.Windows.Application.Current.Dispatcher.Invoke(() => CargarUsuariosLocal());
            }
        }

        private static void CargarUsuariosLocal()
        {
            if (File.Exists(RutaArchivo))
            {
                string json = File.ReadAllText(RutaArchivo);
                var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var listaTemp = JsonSerializer.Deserialize<List<Usuario>>(json, opciones);

                ListaUsuarios.Clear();
                if (listaTemp != null)
                {
                    foreach (var u in listaTemp) ListaUsuarios.Add(u);
                    Log.Information($"💾 Cargados {listaTemp.Count} usuarios desde local.");
                }
            }
            else
            {
                Log.Warning("⚠️ No existe archivo local de usuarios.");
            }
        }

        public static async Task GuardarUsuarios()
        {
            try
            {
                var opciones = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };
                string json = JsonSerializer.Serialize(ListaUsuarios, opciones);

                if (!Directory.Exists(Path.GetDirectoryName(RutaArchivo)))
                    Directory.CreateDirectory(Path.GetDirectoryName(RutaArchivo));

                File.WriteAllText(RutaArchivo, json);

                using (var cliente = new HttpClient())
                {
                    var respuesta = await cliente.PostAsJsonAsync("http://localhost:5106/usuarios/guardar", ListaUsuarios);
                    if (respuesta.IsSuccessStatusCode)
                        Log.Information("🌐 Servidor sincronizado correctamente.");
                    else
                        Log.Warning("⚠️ API no pudo guardar, pero tienes copia local.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ Fallo crítico en sincronización.");
            }
        }

        public static async Task EnviarInicioTurno(Usuario usuario)
        {
            try
            {
                using (var cliente = new HttpClient())
                {
                    var respuesta = await cliente.PostAsJsonAsync("http://localhost:5106/turnos/iniciar", usuario);

                    if (respuesta.IsSuccessStatusCode)
                        Log.Information($"🚀 API: Inicio de turno enviado para {usuario.Nombre}");
                    else
                        Log.Warning("⚠️ La API recibió la llamada pero dio error.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ No se pudo conectar con la API para iniciar turno: {ex.Message}");
            }
        }

        public static async Task EnviarFinTurno(Usuario usuario)
        {
            try
            {
                using (var cliente = new HttpClient())
                {
                    var respuesta = await cliente.PostAsJsonAsync("http://localhost:5106/turnos/finalizar", usuario);

                    if (respuesta.IsSuccessStatusCode)
                        Log.Information($"🚪 API: Fin de turno enviado para {usuario.Nombre}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ Error al enviar fin de turno: {ex.Message}");
            }
        }

    }
}