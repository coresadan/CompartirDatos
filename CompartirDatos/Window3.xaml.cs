using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    public partial class Window3 : Window
    {

        private CaracteristicasDePiezas piezaMostrada;
        public Window3(CaracteristicasDePiezas datosPieza)
        {
            InitializeComponent();

            piezaMostrada = datosPieza;

            if (datosPieza == null)
            {
                MessageBox.Show("La pieza carece de registro", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (datosPieza != null)
            {
                resumenDatosPieza.ItemsSource = datosPieza.Fabricaciones;

                var helper = new System.Windows.Interop.WindowInteropHelper(this);
            }
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void botonSacarCaptura(object sender, RoutedEventArgs e)
        {
            try
            {
                string misDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string carpetaApp = System.IO.Path.Combine(misDocumentos, "Importador de datos", "Capturas");

                if (piezaMostrada.Fabricaciones == null || piezaMostrada.Fabricaciones.Count == 0)
                {
                    MessageBox.Show(this, "La pieza carece de registros de trazabilidad que poder capturar.","Trazabilidad vacía",MessageBoxButton.OK,MessageBoxImage.Warning);
                    return;
                }

                    if (!System.IO.Directory.Exists(carpetaApp)) System.IO.Directory.CreateDirectory(carpetaApp);

                string nombreArchivo = $"Captura_{piezaMostrada.Nombre}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string rutaCompleta = System.IO.Path.Combine(carpetaApp, nombreArchivo);

                double width = this.ActualWidth;
                double height = this.ActualHeight;

                RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
            (int)width, (int)height, 96d, 96d, PixelFormats.Pbgra32);

                renderBitmap.Render(this);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                using (System.IO.FileStream fileStream = new System.IO.FileStream(rutaCompleta, System.IO.FileMode.Create))
                {
                    encoder.Save(fileStream);
                }

                MessageBox.Show(this, "Captura guardada en la carpeta 'Capturas'", "Éxito");
                Log.Verbose($"GUARDADO💾📂: Se ha guardado una captura de la trazabilidad de una pieza.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo tomar la captura: {ex.Message}");
                Log.Verbose($"ERROR DE GUARDADO!💾❌📂: No se pudo guardar una captura de la trazabilidad de una pieza.");
            }
        }
    }
}