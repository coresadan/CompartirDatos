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

            // --- ESCUDO DE SEGURIDAD ---
            // Si el trabajador tiene una pieza activa, bloqueamos la sustitución
            if (ventanaPrincipal._idPiezaEnviadaActual != null)
            {
                MessageBox.Show("¡ATENCIÓN!\n\nNo puedes sustituir la lista actual porque el trabajador está fabricando una pieza.\n\nEspera a que termine o añade nuevas piezas usando la opción de 'Sumar al actual'.",
                                "Conflicto de Producción", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpenFileDialog archivo = new OpenFileDialog();
            archivo.Filter = "Archivos de Texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";

            if (archivo.ShowDialog() == true)
            {
                string rutaArchivo = archivo.FileName;

                // 1. Limpieza total de rastro anterior
                ventanaPrincipal._idPiezaEnviadaActual = null;
                ventanaPrincipal.listaDePiezas.Clear();

                // 2. Ajustes visuales y de disco
                ConfiguracionApp.misAjustes.GuardarConfiguracionEnDisco();
                ventanaPrincipal.ArrastrarElArchivoLabel.Visibility = Visibility.Hidden;

                // 3. Importación y reinicio de ciclo
                ventanaPrincipal.FuncionImportarArchivo(rutaArchivo);
                _ = ventanaPrincipal.ReiniciarCicloDeProduccion();

                Close();
            }
        }

        private void AbrirWebClick(object sender, RoutedEventArgs e)
        {
            var ventanaPrincipal = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (ventanaPrincipal == null) return;

            // --- MISMA PROTECCIÓN PARA LA WEB ---
            if (ventanaPrincipal._idPiezaEnviadaActual != null)
            {
                MessageBox.Show("No puedes acceder al servidor para cambiar la lista mientras hay una pieza en curso.",
                                "Conflicto de Producción", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var abrirVentanaAPI = new VentanaServidorAPI();
            abrirVentanaAPI.Owner = this;
            abrirVentanaAPI.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            Log.Information($"ACCESO AL SERVIDOR👤🕵️ Acceso a listados.");
            abrirVentanaAPI.ShowDialog();
        }

        private void botonCancelarClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
