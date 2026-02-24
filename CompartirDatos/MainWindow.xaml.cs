using Refit;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace CompartirDatos
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        // 1. SEMÁFOROS
        public int? _idPiezaEnviadaActual;
        // 2. OBJETOS DE SESIÓN
        public Usuario _usuarioActivo;
        public ConexionConMiAPI miConexion;
        // 3. LA LISTA
        public ObservableCollection<CaracteristicasDePiezas> listaDePiezas { get; set; } = new ObservableCollection<CaracteristicasDePiezas>();

        public void ComprobacionPiezasTerminadas()
        {
            if (ConfiguracionApp.misAjustes.ListaPiezasTerminadas != null)
            {
                foreach (var pieza in ConfiguracionApp.misAjustes.ListaPiezasTerminadas)
                {
                    ActualizarEstadoGlobalDePieza(pieza);
                }
            }
        }
        public async Task SincronizarYGuardarProgreso()
        {
            ConfiguracionApp.misAjustes.ListaPiezasTerminadas = listaDePiezas.ToList();

            ConfiguracionApp.misAjustes.GuardarConfiguracionEnDisco();
            Log.Information("SISTEMA💾 Progreso guardado correctamente.");
         // LANZAMOS LA SINCRONIZACIÓN AL BOT TAMBIÉN
            await SincronizarConAPI();
        }

        public async Task EnviarSiguientePiezaDisponible()
        {
            var piezaParaEnviar = listaDePiezas.FirstOrDefault(p =>
                !p.EstaTerminada &&
                !p.Datos.Falta &&
                !p.Datos.Error);

            if (piezaParaEnviar != null)
            {
                _idPiezaEnviadaActual = piezaParaEnviar.Id;
                await Task.Delay(100);
                await _emisor.EnviarPiezaAsync(piezaParaEnviar);


                Log.Information($"PIPE🚀 Enviadas {piezaParaEnviar.Id}{piezaParaEnviar.Nombre} piezas al visor del trabajador.");
            }
            else
            {
                _idPiezaEnviadaActual = null;
                await _emisor.EnviarPiezaAsync(null);
                Log.Warning("PIPE⚠️ No hay piezas pendientes para enviar.");
            }
        }

        public readonly ServicioPipeEmisor _emisor = new ServicioPipeEmisor();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _ = SincronizarConAPI();

            SetVisibilidadControles(Visibility.Hidden);

            DatabaseService.InicializarBaseDeDatos();

            var piezasGuardadas = DatabaseService.CargarPiezasDesdeBD();

            dgPiezas.ItemsSource = listaDePiezas;

            if (piezasGuardadas.Count > 0)
            {
                listaDePiezas.Clear();

                foreach (var p in piezasGuardadas)
                {
                    listaDePiezas.Add(p);
                }
                ArrastrarElArchivoLabel.Visibility = Visibility.Hidden;
                Log.Information($"SISTEMA💾 Se han recuperado {piezasGuardadas.Count} piezas de la base de datos.");
            }

            miConexion = new ConexionConMiAPI();
            ConfiguracionApp.misAjustes = ConfiguracionApp.CargarConfiguracion();

            cbUsuarios.ItemsSource = GestionUsuarios.ListaUsuarios;

            string resGuardada = ConfiguracionApp.misAjustes.UltimaResolucion;
            this.MaquinaActual = ConfiguracionApp.misAjustes.UltimaMaquinaSeleccionada;
            string rutaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string carpetaLogs = System.IO.Path.Combine(rutaDocumentos, "Control Fabrica", "logs");

            if (!Directory.Exists(carpetaLogs))
            {
                Directory.CreateDirectory(carpetaLogs);
            }

            string rutaArchivo = System.IO.Path.Combine(carpetaLogs, "registro_.log");

            Log.Logger = new LoggerConfiguration()
       .MinimumLevel.Information()
       .Enrich.WithProperty("NombrePrograma", "🪵 TRAZA-WOOD")
       .WriteTo.Async(x => x.File(
           path: rutaArchivo,
           rollingInterval: RollingInterval.Day,
           encoding: Encoding.UTF8))
       .WriteTo.Telegram(
           botToken: "8572448307:AAEpWviIJ0qqd1YPBXysRjl2SpsXmUprVIw",
           chatId: "5688537233",
           restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
           outputTemplate: "{NombrePrograma} {Level:u3}: {Message:lj}{NewLine}{Exception}"
       )
       .CreateLogger();



            if (!string.IsNullOrEmpty(resGuardada))
            {
                if (resGuardada == "Maximizada")
                {
                    this.WindowState = WindowState.Maximized;
                }
                else
                {
                    try
                    {
                        // Extraemos los números del string "1920 x 1080..."
                        string[] partes = resGuardada.Split('x');
                        if (partes.Length >= 2)
                        {
                            this.Width = double.Parse(partes[0].Trim());
                            // El split(' ') quita el texto extra como "(4K)"
                            this.Height = double.Parse(partes[1].Trim().Split(' ')[0]);
                            this.WindowState = WindowState.Normal;
                        }
                    }
                    catch { /* Si falla el formato, mantiene el tamaño por defecto */ }
                }
            }

            foreach (ComboBoxItem item in comboBoxResoluciones.Items)
            {
                if (item.Content.ToString() == resGuardada)
                {
                    comboBoxResoluciones.SelectedItem = item;
                    break;
                }
            }

            if (ConfiguracionApp.misAjustes.ListaPiezasTerminadas != null && ConfiguracionApp.misAjustes.ListaPiezasTerminadas.Count > 0)
            {
                listaDePiezas.Clear();
                foreach (var pieza in ConfiguracionApp.misAjustes.ListaPiezasTerminadas)
                {
                    listaDePiezas.Add(pieza);
                }

                ArrastrarElArchivoLabel.Visibility = Visibility.Hidden;
                ComprobacionPiezasTerminadas();
            }

            TextBlockNombreMaquina();
            var receptorRespuesta = new ServicioPipeReceptor();
            receptorRespuesta.PiezaRecibida += AlRecibirRespuestaDelTrabajador;
            _ = receptorRespuesta.IniciarEscuchaAsync(CancellationToken.None);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {

            int intentos = 0;

            while (string.IsNullOrEmpty(MaquinaActual) && intentos < 3)
            {
                Window2 ventanaSeleccion = new Window2();
                ventanaSeleccion.Owner = this;
                ventanaSeleccion.WindowStartupLocation = WindowStartupLocation.CenterScreen;


                if (ventanaSeleccion.ShowDialog() == true)
                {
                    ConfiguracionApp.misAjustes.UltimaMaquinaSeleccionada = MaquinaActual;
                    ConfiguracionApp.misAjustes.GuardarConfiguracionEnDisco();
                    TextBlockNombreMaquina();
                    Log.Information($"CAMBIO DE MÁQUINA⚙️ El usuario seleccionó {this.MaquinaActual}");
                }

                else
                {
                    intentos++;

                    if (intentos < 3)
                    {
                        MessageBox.Show($"Selección requerida. Intento {intentos} de 3", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show("Has agotado los intentos. El programa se cerrará por seguridad.", "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                        return;
                    }
                }
            }
        }


        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BotonImportarDatosClick(object sender, RoutedEventArgs e)
        {
            // RESET DE SEGURIDAD: 
            _idPiezaEnviadaActual = null;

            Log.Information("📂 Abriendo menú de importación. Sistema reseteado.");

            // Abrimos la ventana de opciones
            Window1 ventanaOpcionesImportacion = new Window1();
            ventanaOpcionesImportacion.Owner = this;
            ventanaOpcionesImportacion.ShowDialog();
        }

        private void BotonModificarRutaGuardadoClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog archivo = new Microsoft.Win32.OpenFileDialog();
            archivo.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            if (archivo.ShowDialog() == true)
            {
                string rutaArchivo = archivo.FileName;
            }

        }


        private void BotonBorrarDatosClick(object sender, RoutedEventArgs e)
        {

            if (listaDePiezas != null && listaDePiezas.Count > 0)
            {
                MessageBoxResult respuesta = MessageBox.Show("Está seguro que de quiere eliminar la lista de piezas", "Advertencia", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (respuesta == MessageBoxResult.Yes)
                {
                    ConfiguracionApp.misAjustes.ListaPiezasTerminadas.Clear();
                    listaDePiezas.Clear();
                    ArrastrarElArchivoLabel.Visibility = Visibility.Visible;

                    Log.Warning("=💾❌= Lista de Piezas Borrada =💾❌=");
                }

            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DatabaseService.InicializarBaseDeDatos();
            try
            {
                // Cargamos los usuarios sin bloquear la UI
                await GestionUsuarios.CargarUsuariosSincronizados();

                // Si después de cargar la lista sigue vacía, el problema es la API/JSON
                if (GestionUsuarios.ListaUsuarios.Count == 0)
                {
                    Log.Warning("⚠️ Lista de usuarios vacía tras sincronización.");
                }

                cbUsuarios.SelectedIndex = -1;
                Log.Information("SISTEMA⚙️ Interfaz lista y usuarios cargados.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ Error en el evento Loaded");
            }
        }

        private void ArrastrarelArchivo(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void ArrastrarelArchivoDrop(object sender, DragEventArgs e)
        {
            string[] archivos = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (archivos != null && archivos.Length > 0)
            {
                string rutaArchivo = archivos[0];
                ArrastrarElArchivoLabel.Visibility = Visibility.Hidden;
                FuncionImportarArchivo(rutaArchivo);
            }
        }

        private void MostrarErrorImportacion(string mensaje)
        {
            MessageBox.Show(mensaje, "Error al cargar piezas", MessageBoxButton.OK, MessageBoxImage.Error);

            ArrastrarElArchivoLabel.Visibility = Visibility.Hidden;

            Log.Warning($"Intento de importación fallido: {mensaje}");
        }

        public async void FuncionImportarArchivo(string rutaArchivo)
        {
            try
            {
                var contenidoFichero = System.IO.File.ReadAllLines(rutaArchivo);
                if (contenidoFichero == null || contenidoFichero.Length == 0)
                {
                    MostrarErrorImportacion("El archivo está vacío.");
                    return;
                }

                int semillaId = (int)(DateTime.Now.Ticks & 0x7FFFFFFF);
                int piezasNuevasContador = 0;

                foreach (var linea in contenidoFichero)
                {
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    var partes = linea.Split(';');
                    if (partes.Length == 4)
                    {
                        var pieza = new CaracteristicasDePiezas
                        {
                            Id = semillaId + piezasNuevasContador,
                            Nombre = partes[0].Trim(),
                            Color = partes[1].Trim(),
                            Largo = decimal.TryParse(partes[2], out var l) ? l : 0,
                            Ancho = decimal.TryParse(partes[3], out var a) ? a : 0,
                            EstaTerminada = false,
                            Datos = new CaracteristicasDePiezas2 { Falta = false, Error = false },
                            Fabricaciones = new List<Fabricacion>()
                        };

                        listaDePiezas.Add(pieza);
                        piezasNuevasContador++;
                    }
                }

                ConfiguracionApp.misAjustes.UltimaRutaArchivo = rutaArchivo;
                ConfiguracionApp.misAjustes.ListaPiezasTerminadas = listaDePiezas.ToList();
                ConfiguracionApp.misAjustes.GuardarConfiguracionEnDisco();

                dgPiezas.ItemsSource = null;
                dgPiezas.ItemsSource = listaDePiezas;

                await Task.Delay(150);

                if (_idPiezaEnviadaActual == null)
                {
                    await EnviarSiguientePiezaDisponible();
                }

                Log.Information($"IMPORTACIÓN💾 Se han sumado {piezasNuevasContador} piezas. Total: {listaDePiezas.Count}");
                MessageBox.Show($"Se han añadido {piezasNuevasContador} piezas correctamente.", "Éxito");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "🚨 Error en FuncionImportarArchivo");
                MostrarErrorImportacion($"Error crítico: {ex.Message}");
            }
        }

        public async Task EnviarSeñalLimpiezaAlTrabajador()
        {
            var señal = new CaracteristicasDePiezas { Id = -1 };
            await _emisor.EnviarPiezaAsync(señal);
        }

        private async void PiezaTerminadaBotonClick(object sender, RoutedEventArgs e)
        {
            if (cbUsuarios.SelectedItem is not Usuario usuarioActivo)
            {
                MessageBox.Show("¡ATENCIÓN! Debe seleccionar un operario antes de marcar una pieza como TERMINADA.",
                                "Identificación requerida",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            if (dgPiezas.SelectedItem is CaracteristicasDePiezas piezaSeleccionada)
            {
                if (_idPiezaEnviadaActual != null)
                {
                    // Comparamos el ID de la fila seleccionada con el ID que tiene el trabajador
                    if (piezaSeleccionada.Id.ToString() == _idPiezaEnviadaActual.ToString())
                    {
                        MessageBox.Show($"🛑 INTERFERENCIA BLOQUEADA\n\nEl trabajador tiene abierta la pieza: {piezaSeleccionada.Nombre}",
                                        "Seguridad de Producción", MessageBoxButton.OK, MessageBoxImage.Stop);

                        Log.Warning($"🚫 Bloqueado intento de modificar pieza activa: {piezaSeleccionada.Nombre}");

                        return; // Detenemos la ejecución aquí
                    }
                }

                // 4. REGISTRO DE LA FABRICACIÓN (Solo llega aquí si pasa el bloqueo)
                piezaSeleccionada.Fabricaciones.Add(new Fabricacion
                {
                    Fecha = DateTime.Now,
                    Maquina = MaquinaActual,
                    EstadoDeLaPieza = "TERMINADO",
                    Operario = usuarioActivo.Nombre
                });

                // 5. ACTUALIZACIÓN Y PERSISTENCIA
                ActualizarEstadoGlobalDePieza(piezaSeleccionada);
                await SincronizarYGuardarProgreso();
                GenerarArchivoFaltas();

                // 6. REFRESH VISUAL
                dgPiezas.Items.Refresh();

                // 7. SALTO AUTOMÁTICO A LA SIGUIENTE
                var siguiente = listaDePiezas.FirstOrDefault(p => !p.EstaTerminada && !p.Datos.Falta);
                if (siguiente != null)
                {
                    dgPiezas.SelectedItem = siguiente;
                    dgPiezas.ScrollIntoView(siguiente);
                }

                Log.Information($"✅ Oficina: Pieza {piezaSeleccionada.Nombre} registrada.");
                dgPiezas.Focus();
            }
        }

        public async Task ReiniciarCicloDeProduccion()
        {
            // 1. LIMPIEZA: Quitamos el ID de la pieza enviada porque ya terminó
            _idPiezaEnviadaActual = null;

            Log.Information("🔓 Sistema de oficina listo. Esperando envío manual del usuario.");

            // 2. REFRESH VISUAL: Actualizamos los colores y estados en el DataGrid
            dgPiezas.Items.Refresh();

            await Task.CompletedTask;
        }

        private void RetrocederBotonClick(object sender, RoutedEventArgs e)
        {
            // 1. VALIDACIÓN DE USUARIO (Mantenemos tu bloqueo perfecto)
            if (cbUsuarios.SelectedItem is not Usuario usuarioActivo)
            {
                MessageBox.Show("¡ATENCIÓN! Debe seleccionar un operario...", "Identificación requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgPiezas.SelectedItem is CaracteristicasDePiezas piezaSeleccionada)
            {
                // 2. BUSQUEDA DEL REGISTRO LOCAL
                var registroDeEstaMaquina = piezaSeleccionada.Fabricaciones
                    .LastOrDefault(x => x.Maquina == MaquinaActual);

                // 3. PROTECCIÓN CONTRA EDICIÓN EN CURSO
                if (piezaSeleccionada.Id == _idPiezaEnviadaActual)
                {
                    MessageBox.Show($"⚠️ ACCESO DENEGADO\n\nEl operario está trabajando actualmente en: {piezaSeleccionada.Nombre}.", "Conflicto", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                // 4. LÓGICA DE BORRADO
                if (registroDeEstaMaquina != null)
                {
                    piezaSeleccionada.Fabricaciones.Remove(registroDeEstaMaquina);
                    ActualizarEstadoGlobalDePieza(piezaSeleccionada);

                    if (_idPiezaEnviadaActual == null)
                    {

                        Log.Information("🔓 Lista reiniciada. El sistema está listo para un nuevo envío.");
                    }

                    SincronizarYGuardarProgreso();
                    GenerarArchivoFaltas();

                    dgPiezas.Items.Refresh();
                }
                else
                {
                    MessageBox.Show($"Esta pieza no tiene estados de fabricación en {MaquinaActual}.", "Aviso");
                }

                // 5. SALTO VISUAL
                int indiceActual = listaDePiezas.IndexOf(piezaSeleccionada);
                if (indiceActual > 0)
                {
                    dgPiezas.SelectedItem = listaDePiezas[indiceActual - 1];
                    dgPiezas.ScrollIntoView(dgPiezas.SelectedItem);
                }
            }
            dgPiezas.Focus();
        }

        private void BotonGuardarDatosClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var todasLasPiezas = listaDePiezas.ToList();

                if (todasLasPiezas == null || !todasLasPiezas.Any())
                {
                    MessageBox.Show("No hay datos en la lista para guardar. ¡Importa algo primero!", "Lista vacía", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Log.Verbose($"CORRECCIÓN🛠️❌: Intento de guardado sin una lista abierta.");
                    return;
                }

                string misDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string carpetaApp = System.IO.Path.Combine(misDocumentos, "Importador de datos");
                string fechaHoy = DateTime.Now.ToString("yyyy-MM-dd");
                string nombreArchivo = $"{fechaHoy}_Lista_Piezas_terminadas.json";
                string rutaCompleta = System.IO.Path.Combine(carpetaApp, nombreArchivo);
                string contenidoJson = System.Text.Json.JsonSerializer.Serialize(todasLasPiezas, new System.Text.Json.JsonSerializerOptions());


                if (!Directory.Exists(carpetaApp))
                {
                    Directory.CreateDirectory(carpetaApp);
                }


                File.WriteAllText(rutaCompleta, contenidoJson);
                MessageBox.Show($"Progreso actual guardado", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Information("=💾= Lista de Piezas Guardada =💾=");

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico: {ex.Message}");
                Log.Error($"Error crítico❌🔥 {ex.Message}");
            }
        }

        private void ArrastrarListaBotonClickRaton(object sender, MouseButtonEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog buscar = new Microsoft.Win32.OpenFileDialog();
            buscar.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            if (buscar.ShowDialog() == true)
            {
                string rutaArchivo = buscar.FileName;
                ArrastrarElArchivoLabel.Visibility = Visibility.Hidden;
                FuncionImportarArchivo(rutaArchivo);

            }
        }

        private void BotonCambiarDeMaquinaClick(object sender, RoutedEventArgs e)
        {
            Window2 seleccionaPuesto = new Window2();

            seleccionaPuesto.Owner = this;

            seleccionaPuesto.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            seleccionaPuesto.ShowDialog();

            TextBlockNombreMaquina();
            ConfiguracionApp.misAjustes.UltimaMaquinaSeleccionada = MaquinaActual;
            Log.Verbose($"CAMBIO DE MÁQUINA👤⚙️ El usuario seleccionó {MaquinaActual}");

            ConfiguracionApp.misAjustes.GuardarConfiguracionEnDisco();

            ComprobacionPiezasTerminadas();
        }

        public string MaquinaActual { get; set; } = "";


        public void TextBlockNombreMaquina()
        {
            textBlockNombreDeMaquina.Text = MaquinaActual;
        }

        public void ratonDobleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgPiezas.SelectedItem is CaracteristicasDePiezas pieza)
            {
                Window3 ventanaTrazabilidad = new Window3(pieza);
                ventanaTrazabilidad.Owner = this;
                ventanaTrazabilidad.ShowDialog();

                ActualizarEstadoGlobalDePieza(pieza);
                SincronizarYGuardarProgreso();
            }
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_usuarioActivo != null)
            {
                await GestionUsuarios.EnviarFinTurno(_usuarioActivo);
            }
            SincronizarYGuardarProgreso();
        }


        public void GenerarArchivoFaltas()
        {
            var listaFaltas = ConfiguracionApp.misAjustes.ListaPiezasTerminadas
                                .Where(p => p.Datos.Falta || p.Datos.EsFaltaParcial)
                                .ToList();

            string carpeta = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Control Fabrica");
            string ruta = System.IO.Path.Combine(carpeta, "Piezas_Faltantes.json");

            if (listaFaltas.Any())
            {
                try
                {
                    if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);

                    string json = System.Text.Json.JsonSerializer.Serialize(listaFaltas, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(ruta, json);

                    Log.Information($"SISTEMA💾✅ Generado archivo de faltas con {listaFaltas.Count} piezas.");
                }
                catch (Exception ex)
                {
                    Log.Error($"ERROR ❌💾 al generar archivo: {ex.Message}");
                }
            }
            else
            {
                if (File.Exists(ruta))
                {
                    File.Delete(ruta);
                    Log.Information("SISTEMA⚙️ No hay faltas, archivo eliminado❌💾");
                }
            }
        }

        private async void BotonFaltaPieza(object sender, RoutedEventArgs e)
        {
            // 1. Validación de Usuario
            if (cbUsuarios.SelectedItem is not Usuario usuario)
            {
                MessageBox.Show("¡ATENCIÓN! Debe seleccionar un trabajador antes de marcar una pieza como FALTA.",
                                "Usuario Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Validación de Selección
            if (dgPiezas.SelectedItem is not CaracteristicasDePiezas pieza) return;

            // --- BLOQUEO DE SEGURIDAD (Mantenemos el ID intacto) ---
            if (pieza.Id == _idPiezaEnviadaActual)
            {
                MessageBox.Show($"⚠️ ACCESO DENEGADO\n\nEl operario de fábrica está trabajando actualmente en la pieza: {pieza.Nombre}.\n\nNo puedes marcar una falta desde la oficina mientras el trabajador la tenga abierta.",
                                "Conflicto de Producción", MessageBoxButton.OK, MessageBoxImage.Stop);

                Log.Warning($"🚫 Intento de marcar FALTA bloqueado: {pieza.Nombre} está en uso.");
                return;
            }

            // 3. Registro del Movimiento
            pieza.Fabricaciones.Add(new Fabricacion
            {
                Fecha = DateTime.Now,
                Maquina = MaquinaActual,
                EstadoDeLaPieza = "FALTA/RECHAZO",
                Operario = usuario.Nombre
            });

            // 4. Actualización de Lógica y Persistencia
            ActualizarEstadoGlobalDePieza(pieza);
            Log.Warning($"ALERTA🚨❌ Falta en pieza {pieza.Nombre} (MÁQ: {MaquinaActual}) por {usuario.Nombre}");

            SincronizarYGuardarProgreso();
            GenerarArchivoFaltas();

            // 5. Refrescamos visualmente la lista de la oficina.
            dgPiezas.Items.Refresh();

            // Buscamos la siguiente para mover el foco en tu pantalla de oficina
            var siguiente = listaDePiezas.FirstOrDefault(p => !p.EstaTerminada && !p.Datos.Falta);
            if (siguiente != null)
            {
                dgPiezas.SelectedItem = siguiente;
                dgPiezas.ScrollIntoView(siguiente);
            }
            dgPiezas.Focus();
        }

        private void ActualizarEstadoGlobalDePieza(CaracteristicasDePiezas pieza)
        {
            if (pieza.Fabricaciones == null || !pieza.Fabricaciones.Any())
            {
                ResetearEstados(pieza);
                return;
            }

            // 1. Estados específicos de ESTA máquina
            bool terminadaAqui = pieza.Fabricaciones.Any(f => f.Maquina == MaquinaActual && f.EstadoDeLaPieza == "TERMINADO");
            bool faltaAqui = pieza.Fabricaciones.Any(f => f.Maquina == MaquinaActual && f.EstadoDeLaPieza == "FALTA/RECHAZO");

            // 2. Estados globales (sumando todas las máquinas)
            bool tieneAlgunaFalta = pieza.Fabricaciones.Any(f => f.EstadoDeLaPieza == "FALTA/RECHAZO");
            bool tieneAlgunaTerminada = pieza.Fabricaciones.Any(f => f.EstadoDeLaPieza == "TERMINADO");

            // --- LÓGICA DE DECISIÓN ---
            if (tieneAlgunaTerminada && tieneAlgunaFalta)
            {
                pieza.Datos.EsFaltaParcial = true;
                pieza.EstaTerminada = true;
                pieza.Datos.Falta = false;
            }
            else if (faltaAqui)
            {
                pieza.Datos.Falta = true;
                pieza.EstaTerminada = true;
                pieza.Datos.EsFaltaParcial = false;
            }
            else if (terminadaAqui)
            {
                pieza.EstaTerminada = true;
                pieza.Datos.Falta = false;
                pieza.Datos.EsFaltaParcial = false;
            }
            else
            {
                ResetearEstados(pieza);
            }
        }

        private void ResetearEstados(CaracteristicasDePiezas pieza)
        {
            pieza.EstaTerminada = false;
            pieza.Datos.Falta = false;
            pieza.Datos.EsFaltaParcial = false;
        }

        public void comboBoxResoluciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxResoluciones.SelectedItem is ComboBoxItem item)
            {
                string resolucion = item.Content.ToString();

                if (resolucion == "Maximizada")
                {
                    this.WindowState = WindowState.Maximized;
                    if (dgPiezas != null) dgPiezas.FontSize = 20;
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    string[] partes = resolucion.Split('x');

                    if (partes.Length == 2)
                    {
                        try
                        {
                            double ancho = double.Parse(partes[0].Trim());
                            this.Width = ancho;

                            string altoLimpio = partes[1].Trim().Split(' ')[0];
                            this.Height = double.Parse(altoLimpio);

                            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
                            this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2;

                            if (dgPiezas != null)
                            {
                                double nuevaFuente = ancho / 40;
                                // Ponemos límites para que no sea ilegible ni gigante
                                dgPiezas.FontSize = Math.Clamp(nuevaFuente, 14, 40);
                                dgPiezas.RowHeight = double.NaN;
                            }
                        }

                        catch (Exception ex)
                        {
                            Log.Information("ERROR❌🖥️ al cambiar resolución: " + ex.Message);
                        }
                    }
                }

                ConfiguracionApp.misAjustes.UltimaResolucion = resolucion;
                ConfiguracionApp.misAjustes.GuardarConfiguracionEnDisco();
                Log.Information($"SISTEMA✅🖥️ Resolución guardada: {resolucion}");
            }
        }

        public void btnAdmin_Click(object sender, RoutedEventArgs e)
        {
            string password = Microsoft.VisualBasic.Interaction.InputBox("Introduce el PIN de Administrador:", "Acceso Restringido", "");

            if (password == "120920")
            {
                Log.Error("USUARIOS🚨 Modo edición activado por Administrador 👤");
                botonAnadirUsuario.Visibility = Visibility.Visible;
                botonBorrarUsuario.Visibility = Visibility.Visible;
                botonSalirModoAdministrador.Visibility = Visibility.Visible;
                botonAbrirGraficas.Visibility = Visibility.Visible;
                MessageBox.Show("¡Acceso correcto! Ahora puedes gestionar los usuarios.", "Seguridad", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            else
            {
                MessageBox.Show("PIN Incorrecto. Acceso denegado.", "Seguridad", MessageBoxButton.OK, MessageBoxImage.Stop);
                Log.Error("⚠️ Alguien intentó entrar como Admin sin éxito 🖥️🔒 **BLOQUEO DE ACCESO**");
            }
        }


        private async void ComboBox_SeleccionUsuario(object sender, SelectionChangedEventArgs e)
        {
            var seleccionado = cbUsuarios.SelectedItem as Usuario;
            if (seleccionado == null) return;

            string pinIntroducido = Microsoft.VisualBasic.Interaction.InputBox($"PIN para {seleccionado.Nombre}:", "Acceso de Seguridad");

            if (pinIntroducido == seleccionado.Pin)
            {
                if (_usuarioActivo != null && _usuarioActivo.Pin != seleccionado.Pin)
                {
                    await GestionUsuarios.EnviarFinTurno(_usuarioActivo);
                    Log.Information($"⏱️ Relevo: Turno de {_usuarioActivo.Nombre} finalizado.");
                }

                _usuarioActivo = seleccionado;
                await GestionUsuarios.EnviarInicioTurno(_usuarioActivo);
                SetVisibilidadControles(Visibility.Visible);
                Log.Information($"🚀 Turno iniciado correctamente para {_usuarioActivo.Nombre}");
            }
            else
            {

                Log.Warning($"🚫 Intento de acceso fallido para {seleccionado.Nombre}");

                cbUsuarios.SelectedItem = _usuarioActivo;

                System.Windows.MessageBox.Show("PIN incorrecto. No se ha cambiado el usuario.", "Error de Autenticación", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void botonAnadirUsuarioClick(object sender, RoutedEventArgs e)
        {
            string nuevoNombre = Microsoft.VisualBasic.Interaction.InputBox("Nombre del nuevo Usuario:", "Registro", "");
            if (string.IsNullOrWhiteSpace(nuevoNombre)) return;

            string nuevoPin = Microsoft.VisualBasic.Interaction.InputBox($"PIN para {nuevoNombre}:", "Seguridad", "");
            if (string.IsNullOrWhiteSpace(nuevoPin)) return;

            Usuario nuevo = new Usuario { Nombre = nuevoNombre, Pin = nuevoPin, Rol = "Operario" };

            GestionUsuarios.ListaUsuarios.Add(nuevo);

            await GestionUsuarios.GuardarUsuarios();

            Log.Information($"👤✅ Creado usuario: {nuevoNombre}");
        }

        private async void botonBorrarUsuarioClick(object sender, RoutedEventArgs e)
        {
            if (!(cbUsuarios.SelectedItem is Usuario seleccionado))
            {
                MessageBox.Show("Por favor, selecciona un usuario de la lista para eliminarlo.");
                return;
            }

            var result = MessageBox.Show($"¿Estás seguro de eliminar a {seleccionado.Nombre}?", "Aviso", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                // Al borrar de la lista, desaparece del ComboBox automáticamente
                GestionUsuarios.ListaUsuarios.Remove(seleccionado);
                await GestionUsuarios.GuardarUsuarios();
                Log.Warning($"🗑️ Usuario eliminado: {seleccionado.Nombre}");
            }
        }

        public async Task CargarListaUsuariosDelServidor()
        {
            try
            {
                await Task.Delay(1000);
                await GestionUsuarios.CargarUsuariosSincronizados();

                cbUsuarios.ItemsSource = null;
                cbUsuarios.ItemsSource = GestionUsuarios.ListaUsuarios;
                cbUsuarios.DisplayMemberPath = "Nombre";

                if (cbUsuarios.Items.Count > 0)
                    cbUsuarios.SelectedIndex = 0;
                Log.Information("✅ Interfaz de usuarios actualizada desde el servidor.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar: {ex.Message}", "Fallo de Red en 5106");
                Log.Error("🌐❌ **FALLO DE SERVIDOR** | El programa no pudo conectar. 🚨");
            }

        }

        private void botonSalirAdministradorClick(object sender, RoutedEventArgs e)
        {
            botonAnadirUsuario.Visibility = Visibility.Collapsed;
            botonBorrarUsuario.Visibility = Visibility.Collapsed;
            botonSalirModoAdministrador.Visibility = Visibility.Collapsed;
            botonAbrirGraficas.Visibility = Visibility.Collapsed;

            Log.Information("USUARIOS🔒 Modo edición desactivado. Volviendo a modo lectura.");
            MessageBox.Show("Has salido del modo administrador correctamente.", "Seguridad", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AbrirVentanaDesgloseDeLasPiezas(object sender, RoutedEventArgs e)
        {
            var abrirVentanaDesglose = new DetallesDeLaLista(listaDePiezas.ToList());
            abrirVentanaDesglose.Owner = this;


            abrirVentanaDesglose.WindowStartupLocation = WindowStartupLocation.Manual;
            abrirVentanaDesglose.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            abrirVentanaDesglose.ShowDialog();
        }


        private void BotonAbrirGraficas(object sender, RoutedEventArgs e)
        {
            var abrirventana = new VentanaGraficas();
            abrirventana.Owner = this;

            abrirventana.WindowStartupLocation = WindowStartupLocation.Manual;
            abrirventana.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            abrirventana.ShowDialog();
        }

        private void BotonCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void AlRecibirRespuestaDelTrabajador(object? sender, string respuesta)
        {
            // 1. Separamos el comando del ID (Ej: "ACABADA|101")
            var partes = respuesta.Split('|');
            string comando = partes[0].Trim().ToUpper();
            string? idRecibido = partes.Length > 1 ? partes[1] : null;

            Dispatcher.Invoke(async () =>
            {
                // --- CASO 0: TRABAJADOR LIBRE O CANCELACIÓN ---
                if (comando == "LIBRE" || comando == "LISTA_CANCELADA_POR_TRABAJADOR")
                {
                    _idPiezaEnviadaActual = null;
                    Log.Information($"✅ Sistema liberado por comando: {comando}");
                    if (comando.Contains("CANCELADA")) MessageBox.Show("El operario ha cancelado la recepción.", "Aviso");
                    return;
                }

                // --- CASO 1: SINCRONIZACIÓN (SOLICITAR) ---
                if (comando == "SOLICITAR_PIEZA_ACTUAL")
                {
                    var piezaAEnviar = listaDePiezas.FirstOrDefault(p => !p.EstaTerminada && !p.Datos.Falta);

                    if (piezaAEnviar != null)
                    {
                        _idPiezaEnviadaActual = piezaAEnviar.Id;
                        await _emisor.EnviarPiezaAsync(piezaAEnviar);
                    }
                    return;
                }

                // --- CASO 2: PROCESAR RESULTADO (TERMINADA / FALTA) ---
                var idABuscar = idRecibido ?? _idPiezaEnviadaActual?.ToString();
                var piezaActual = listaDePiezas.FirstOrDefault(p => p.Id.ToString() == idABuscar);

                if (piezaActual != null)
                {
                    var nuevaEntrada = new Fabricacion
                    {
                        Fecha = DateTime.Now,
                        Maquina = MaquinaActual,
                        Operario = "Operario Santos",
                        EstadoDeLaPieza = comando == "ACABADA" ? "TERMINADO" : "FALTA/RECHAZO"
                    };

                    if (piezaActual.Fabricaciones == null) piezaActual.Fabricaciones = new List<Fabricacion>();
                    piezaActual.Fabricaciones.Add(nuevaEntrada);

                    // Actualizamos estados
                    piezaActual.EstaTerminada = (comando == "ACABADA");
                    piezaActual.Datos.Falta = (comando != "ACABADA");

                    // LIBERAMOS EL ID ANTES DE ENVIAR LA SIGUIENTE
                    _idPiezaEnviadaActual = null;

                    Log.Information($"[RECIBIDO] {piezaActual.Nombre} marcada como {nuevaEntrada.EstadoDeLaPieza}");

                    SincronizarYGuardarProgreso();
                    _ = EnviarSiguientePiezaDisponible();
                }
            });
        }

        private async void BtnNotificarCarga_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // RESET DE EMERGENCIA: Forzamos el desbloqueo al pulsar.
                _idPiezaEnviadaActual = null;

                // Buscamos la siguiente pieza (Filtro moderno: no terminada y sin falta)
                var siguiente = listaDePiezas.FirstOrDefault(p => !p.EstaTerminada && !p.Datos.Falta);

                if (siguiente != null)
                {
                    MessageBoxResult respuesta = MessageBox.Show("Solicitando sincronización con la Fáfrica... Continuar?", "Sincronización", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (respuesta == MessageBoxResult.Yes)
                    {
                        // Marcamos como ocupado solo para la nueva pieza
                        _idPiezaEnviadaActual = siguiente.Id;

                        await _emisor.EnviarPiezaAsync(siguiente);
                        Log.Information($"🚀 Sistema liberado y pieza enviada: {siguiente.Nombre}");
                    }
                    if (respuesta == MessageBoxResult.No)
                    {
                        MessageBox.Show("Sincronización cancelada. El sistema sigue esperando.", "Sincronización", MessageBoxButton.OK, MessageBoxImage.Information);
                        Log.Information("🚫 Sincronización cancelada por el usuario. El sistema sigue esperando.");
                    }
                }
                else
                {
                    // Si no hay nada apto, limpiamos el visor del trabajador
                    await _emisor.EnviarPiezaAsync(null);

                    if (!listaDePiezas.Any(p => !p.EstaTerminada))
                    {
                        MessageBox.Show("¡Lista de Piezas finalizada! Has terminado toda la lista.", "Producción");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ Error en el flujo de envío: {ex.Message}");
            }
        }

        private async void botonCerrarSesionClick(object sender, RoutedEventArgs e)
        {
            var usuarioQueSale = _usuarioActivo;

            if (usuarioQueSale != null)
            {
                try
                {
                    await GestionUsuarios.EnviarFinTurno(usuarioQueSale);
                    Log.Information($"🚪 Sesión finalizada: {usuarioQueSale.Nombre}");

                    cbUsuarios.SelectionChanged -= ComboBox_SeleccionUsuario;
                    cbUsuarios.SelectedItem = null;
                    _usuarioActivo = null;
                    cbUsuarios.SelectionChanged += ComboBox_SeleccionUsuario;

                    SetVisibilidadControles(Visibility.Hidden);

                    MessageBox.Show("Sesión cerrada correctamente.", "Santos - Seguridad");
                }
                catch (Exception ex)
                {
                    Log.Error($"❌ Error al cerrar sesión: {ex.Message}");
                }
            }
        }

        private void SetVisibilidadControles(Visibility estado)
        {
            PiezaTerminadaBoton.Visibility = estado;
            botonFalta.Visibility = estado;
            RetrocederBoton.Visibility = estado;
            botonCerrarSesion.Visibility = estado;
        }

        public async Task SincronizarConAPI()
        {
            try
            {
                using var client = new HttpClient();
                // Puerto 5000 como confirmamos antes
                string url = "http://localhost:5106/Produccion/Sincronizar";

                // Enviamos la lista actual a la API
                var response = await client.PostAsJsonAsync(url, listaDePiezas);

                if (response.IsSuccessStatusCode)
                {
                    Log.Information("TELEGRAMBot🤖 Datos sincronizados correctamente.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"API⚠️ No se pudo conectar con la API (¿Está apagada?): {ex.Message}");
            }
        }
    }
}