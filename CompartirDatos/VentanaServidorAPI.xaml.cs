using Serilog;
using System;
using System.Collections.Generic;
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
                    string nombreSinExtension = nombre.Replace(".txt", "");

                    var piezasDescargadas = await miConexion.CargarPiezasPorNombre(nombre);
                    if (piezasDescargadas == null || !piezasDescargadas.Any())
                    {
                        MessageBox.Show("El archivo está vacío o no se pudo procesar.", "Aviso");
                        return;
                    }
                    var principal = (MainWindow)Application.Current.MainWindow;
                    principal.dgPiezas.ItemsSource = piezasDescargadas;
                    principal.ArrastrarElArchivoLabel.Visibility = Visibility.Collapsed;

                    this.Close();
                    if (this.Owner is Window1 v)
                    {
                        v.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Vaya, el servidor dice: " + ex.Message);
                    Log.Error("🌐❌ **FALLO DE SERVIDOR** | El programa no pudo conectar. 🚨");
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

