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
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            OpenFileDialog archivo = new OpenFileDialog();
            archivo.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            if (archivo.ShowDialog() == true)
            {
                string rutaArchivo = archivo.FileName;

                Log.Information($"IMPORTACIÓN💾✅ Archivo '{archivo.FileName}' cargado.");
                ConfiguracionApp.misAjustes.UltimaRutaArchivo = rutaArchivo;
                ConfiguracionApp.misAjustes.GuardarConfiguracionEnDisco();
                mainWindow.ArrastrarElArchivoLabel.Visibility = Visibility.Hidden;

                var ventanaPrincipal = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

                if (ventanaPrincipal != null)
                {
                    ventanaPrincipal.FuncionImportarArchivo(rutaArchivo);
                    Close();
                }
            }
        }

        private void AbrirLibreriaClick(object sender, RoutedEventArgs e)
        {
            var ventanaPrincipal = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (ventanaPrincipal == null) return;

            OpenFileDialog archivo = new OpenFileDialog();
            archivo.Filter = "Archivos de Texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";

            if (archivo.ShowDialog() == true)
            {
                string rutaArchivo = archivo.FileName;
                ventanaPrincipal.listaDePiezas.Clear();
                
                ConfiguracionApp.misAjustes.GuardarConfiguracionEnDisco();
                ventanaPrincipal.ArrastrarElArchivoLabel.Visibility = Visibility.Hidden;

                ventanaPrincipal.FuncionImportarArchivo(rutaArchivo);
                Close();
            }
        }
    

        private void AbrirWebClick(object sender, RoutedEventArgs e)
        {
            var abrirVentanaAPI = new VentanaServidorAPI();
            abrirVentanaAPI.Owner = this;
            abrirVentanaAPI.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Log.Information($"ACCESO AL SERVIDOR👤🕵️ El operario accedió a los listados del servidor.");
            abrirVentanaAPI.ShowDialog();
        }

        private void botonCancelarClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
