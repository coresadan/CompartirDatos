using Refit;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
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
        public Usuario _usuarioActivo;
        public ConexionConMiAPI miConexion;

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
        public void SincronizarYGuardarProgreso()
        {
            ConfiguracionApp.misAjustes.ListaPiezasTerminadas = listaDePiezas.ToList();

            ConfiguracionApp.misAjustes.GuardarConfiguracionEnDisco();
            Log.Information("SISTEMA💾 Progreso guardado correctamente.");
        }

        public int? _idPiezaEnviadaActual;
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
                await _emisor.EnviarPiezaAsync(null);
                Log.Warning("PIPE⚠️ No hay piezas pendientes para enviar.");
            }
        }

        public readonly ServicioPipeEmisor _emisor = new ServicioPipeEmisor();

        public MainWindow()
        {
            InitializeComponent();

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

        private void PiezaTerminadaBotonClick(object sender, RoutedEventArgs e)
        {
            if (!(cbUsuarios.SelectedItem is Usuario usuarioActivo))
            {
                MessageBox.Show("Debe seleccionar un operario antes de continuar.", "Identificación requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                Log.Information($"USER👤❌No se ha seleccionado un usuario previamente antes de marcar la pieza.");
                return;
            }

            if (!(dgPiezas.SelectedItem is CaracteristicasDePiezas piezaSeleccionada))
            {
                MessageBox.Show("No hay ninguna pieza seleccionada. Por favor, seleccione una fila", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                Log.Information($"USER👤🌳⚠️No se ha seleccionado una pieza con la que trabajar.");
                return;
            }

            if (piezaSeleccionada.Id == _idPiezaEnviadaActual)
    {
        MessageBox.Show($"⚠️ ACCESO DENEGADO\n\nEl operario está trabajando actualmente en la pieza: {piezaSeleccionada.Nombre}.\nNo puedes modificarla hasta que él termine.", 
                        "Conflicto de Producción", MessageBoxButton.OK, MessageBoxImage.Stop);
        return; 
    }
            bool tieneFaltaEnEstaMaquina = piezaSeleccionada.Fabricaciones.Any(f =>
        f.Maquina == MaquinaActual && f.EstadoDeLaPieza == "FALTA/RECHAZO");

            if (tieneFaltaEnEstaMaquina)
            {
                MessageBox.Show("Esta pieza tiene un rechazo previo en esta máquina. Debe eliminar el último estado antes de terminarla.",
                                "Pieza Bloqueada", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }


            piezaSeleccionada.Fabricaciones.Add(new Fabricacion
            {
                Fecha = DateTime.Now,
                Maquina = MaquinaActual,
                EstadoDeLaPieza = "TERMINADO",
                Operario = usuarioActivo.Nombre
            });

            ActualizarEstadoGlobalDePieza(piezaSeleccionada);

            Log.Information($"PRODUCCIÓN🏭📦 Pieza {piezaSeleccionada.Nombre} terminada por {usuarioActivo.Nombre}💎");
            SincronizarYGuardarProgreso();
            GenerarArchivoFaltas();

            int indiceActual = listaDePiezas.IndexOf(piezaSeleccionada);

            var siguiente = listaDePiezas.FirstOrDefault(p => !p.EstaTerminada);
            if (siguiente != null)
            {
                dgPiezas.SelectedItem = siguiente;
                dgPiezas.ScrollIntoView(siguiente);
            }
            dgPiezas.Focus();
        }



        private void RetrocederBotonClick(object sender, RoutedEventArgs e)
        {
            if (dgPiezas.SelectedItem is CaracteristicasDePiezas piezaSeleccionada)
            {
                var registroDeEstaMaquina = piezaSeleccionada.Fabricaciones
                    .LastOrDefault(x => x.Maquina == MaquinaActual);

                if (piezaSeleccionada.Id == _idPiezaEnviadaActual)
                {
                    MessageBox.Show($"⚠️ ACCESO DENEGADO\n\nEl operario está trabajando actualmente en la pieza: {piezaSeleccionada.Nombre}.\nNo puedes modificarla hasta que él termine.",
                                    "Conflicto de Producción", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }
                if (registroDeEstaMaquina != null)
                {
                    string fechaOriginal = registroDeEstaMaquina.Fecha.ToString("dd/MM/yyyy HH:mm:ss");
                    Log.Verbose($"[CORRECCIÓN🛠️❌] Eliminado: {piezaSeleccionada.Nombre} ({fechaOriginal}) en {MaquinaActual}");

                    piezaSeleccionada.Fabricaciones.Remove(registroDeEstaMaquina);
                    ActualizarEstadoGlobalDePieza(piezaSeleccionada);

                    SincronizarYGuardarProgreso();
                    GenerarArchivoFaltas();

                    Log.Warning($"CORRECCIÓN🛠️❌: Eliminado último estado de {piezaSeleccionada.Nombre} en {MaquinaActual}");
                }
                else
                {
                    MessageBox.Show($"Esta pieza no tiene estados de fabricación en {MaquinaActual}.", "Aviso");
                }

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

        private void BotonFaltaPieza(object sender, RoutedEventArgs e)
        {
            if (cbUsuarios.SelectedItem is not Usuario usuario)
            {
                MessageBox.Show("¡ATENCIÓN! Debe seleccionar un trabajador antes de marcar una pieza como FALTA.",
                                "Usuario Requerido",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            if (dgPiezas.SelectedItem is CaracteristicasDePiezas pieza)
            {
                if (pieza.Id == _idPiezaEnviadaActual)
                {
                    MessageBox.Show($"⚠️ ACCESO DENEGADO\n\nEl operario está trabajando actualmente en la pieza: {pieza.Nombre}.\nNo puedes marcar una falta desde la oficina mientras el trabajador la tiene abierta.",
                                    "Conflicto de Producción", MessageBoxButton.OK, MessageBoxImage.Stop);

                    Log.Warning($"🚫 Intento de modificación bloqueado: La oficina intentó marcar FALTA en la pieza ID {pieza.Id} que está en uso.");
                    return;
                }
                pieza.Fabricaciones.Add(new Fabricacion
                {
                    Fecha = DateTime.Now,
                    Maquina = MaquinaActual,
                    EstadoDeLaPieza = "FALTA/RECHAZO",
                    Operario = usuario.Nombre
                });

                ActualizarEstadoGlobalDePieza(pieza);

                Log.Warning($"ALERTA🚨❌📦 Falta/Rechazo en pieza {pieza.Nombre} (MÁQ: {MaquinaActual} (USER: {usuario.Nombre})");
                SincronizarYGuardarProgreso();
                GenerarArchivoFaltas();

                int indiceActual = listaDePiezas.IndexOf(pieza);
                if (indiceActual < listaDePiezas.Count - 1)
                {
                    dgPiezas.SelectedItem = listaDePiezas[indiceActual + 1];
                    dgPiezas.ScrollIntoView(dgPiezas.SelectedItem);
                }
            }
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
            string respuestaLimpia = respuesta.Trim().ToUpper();

            Dispatcher.Invoke(async () =>
            {
                // --- CASO 1: SINCRONIZACIÓN ---
                if (respuestaLimpia == "SOLICITAR_PIEZA_ACTUAL")
                {
                    Log.Information("🔄 Sincronización manual: Reordenando búsqueda desde el inicio.");

                    var piezaAEnviar = listaDePiezas.FirstOrDefault(p => p.Id == _idPiezaEnviadaActual && !p.EstaTerminada && !p.Datos.Falta);

                    if (piezaAEnviar == null)
                    {
                        piezaAEnviar = listaDePiezas.OrderBy(p => listaDePiezas.IndexOf(p))
                                                    .FirstOrDefault(p => !p.EstaTerminada && !p.Datos.Falta);
                    }
                    if (piezaAEnviar != null)
                    {
                        _idPiezaEnviadaActual = piezaAEnviar.Id;
                        await _emisor.EnviarPiezaAsync(piezaAEnviar);
                        Log.Information($"🚀 Sincronización exitosa: Iniciando en {piezaAEnviar.Nombre}");
                    }
                    else
                    {
                        await _emisor.EnviarPiezaAsync(null);
                    }
                    return;
                }

                // --- CASO 2: CANCELACIÓN (Usamos respuestaLimpia para evitar fallos) ---
                if (respuestaLimpia == "LISTA_CANCELADA_POR_TRABAJADOR")
                {
                    Log.Warning("⚠️ Lista cancelada por el operario.");
                    _idPiezaEnviadaActual = null;
                    MessageBox.Show("El operario ha cancelado la recepción.", "Aviso");
                    return;
                }

                // --- CASO 3: PROCESAR RESULTADO (TERMINADA / FALTA) ---
                var piezaActual = listaDePiezas.FirstOrDefault(p => p.Id == _idPiezaEnviadaActual);

                if (piezaActual != null)
                {
                    // Creamos el registro con lo que acaba de pasar
                    var nuevaEntrada = new Fabricacion
                    {
                        Fecha = DateTime.Now,
                        Maquina = MaquinaActual,
                        Operario = "Operario Santos",
                        EstadoDeLaPieza = respuestaLimpia == "ACABADA" ? "TERMINADO" : "FALTA/RECHAZO"
                    };

                    if (piezaActual.Fabricaciones == null) piezaActual.Fabricaciones = new List<Fabricacion>();
                    piezaActual.Fabricaciones.Add(nuevaEntrada);

                    // LÓGICA DE ESTADOS
                    // Si la respuesta es ACABADA, priorizamos que la pieza está lista.
                    if (respuestaLimpia == "ACABADA")
                    {
                        piezaActual.EstaTerminada = true;
                        piezaActual.Datos.Falta = false; 
                    }
                    else
                    {
                        piezaActual.EstaTerminada = false;
                        piezaActual.Datos.Falta = true;
                    }

                    Log.Information($"[RECIBIDO] {piezaActual.Nombre} marcada como {nuevaEntrada.EstadoDeLaPieza}");

                    SincronizarYGuardarProgreso();
                    _ = EnviarSiguientePiezaDisponible();
                }
            });
        }


    }
}