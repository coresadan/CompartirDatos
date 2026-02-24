using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

namespace CompartirDatos
{
    /// <summary>
    /// Lógica de interacción para VentanaServidorAPI.xaml
    /// </summary>
    public partial class VentanaServidorAPI : Window
    {
        public ConexionConMiAPI miConexion;
        public VentanaServidorAPI()
        {
            InitializeComponent();
            _ = CargarListadoAutomaticoAPI();
        }

        private void botonCancelarCargarAPI(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void botonCargarClick(object sender, RoutedEventArgs e)
        {
            try
            {
                dgAPIListaPiezas.ItemsSource = await miConexion.CargarNombresDeArchivos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private async void botonImportarSeleccion_Click(object sender, RoutedEventArgs e)
        {
            if (dgAPIListaPiezas.SelectedItem is string nombre)
            {
                try
                {
                    // 1. Descarga de datos desde la API
                    var piezasDescargadas = await miConexion.CargarPiezasPorNombre(nombre);

                    if (piezasDescargadas == null || !piezasDescargadas.Any())
                    {
                        MessageBox.Show("El archivo está vacío o no se pudo procesar.", "Aviso");
                        return;
                    }

                    var principal = (MainWindow)Application.Current.MainWindow;

                    // 2. REINICIO AGRESIVO (Evita que queden IDs huérfanos de la lista anterior)
                    principal._idPiezaEnviadaActual = null;

                    // 3. Actualización de la oficina
                    principal.listaDePiezas = new ObservableCollection<CaracteristicasDePiezas>(piezasDescargadas);
                    principal.dgPiezas.ItemsSource = principal.listaDePiezas;

                    // --- 4. BLOQUEO DE SEGURIDAD SINCRONIZADO ---
                    var primeraPieza = principal.listaDePiezas.FirstOrDefault(p => !p.EstaTerminada && !p.Datos.Falta);

                    // Verificamos conexión real para evitar bloqueos innecesarios
                    if (primeraPieza != null && principal._emisor.EstaConectado)
                    {
                        // IMPORTANTE: Nos aseguramos de que el ID se guarde exactamente igual que como se enviará
                        principal._idPiezaEnviadaActual = primeraPieza.Id;

                        // Disparamos el envío
                        _ = principal.EnviarSiguientePiezaDisponible();
                        Log.Information($"🚀 API: Lista '{nombre}' cargada. Pieza {primeraPieza.Nombre} bloqueada y enviada.");
                    }
                    else
                    {
                        // Si no hay trabajador, la oficina queda libre 
                        Log.Information($"ℹ️ API: Lista '{nombre}' cargada en modo local (Trabajador desconectado).");
                    }

                    // 5. Limpieza visual y cierre
                    principal.ArrastrarElArchivoLabel.Visibility = Visibility.Collapsed;

                    this.Close();
                    var w1 = Application.Current.Windows.OfType<Window1>().FirstOrDefault();
                    w1?.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al importar desde el servidor: " + ex.Message);
                    Log.Error(ex, "🌐❌ Error crítico en importación API");
                }
            }
        }

        public async Task CargarListadoAutomaticoAPI()
        {
            if (miConexion == null) miConexion = new ConexionConMiAPI();

            try
            {
                var listadosAPI = await miConexion.CargarNombresDeArchivos();
                dgAPIListaPiezas.ItemsSource = listadosAPI;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}");
                Log.Error(ex, "❌ FALLO DE SERVIDOR");
            }
        }
    }
}

