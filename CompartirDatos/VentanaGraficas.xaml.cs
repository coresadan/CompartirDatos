using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
    public partial class VentanaGraficas : Window
    {
        private AnalizadorDatos _analizador;

        public VentanaGraficas()
        {
            InitializeComponent();

            var piezasActuales = ConfiguracionApp.misAjustes.ListaPiezasTerminadas;
            _analizador = new AnalizadorDatos(piezasActuales);

            CargarEstadisticas();
        }

        private void CargarEstadisticas()
        {
            ListaPiezas.ItemsSource = _analizador.ObtenerRankingPiezas();
            ListaEficiencia.ItemsSource = _analizador.ObtenerPiezasPorHora();
            ListaFaltas.ItemsSource = _analizador.ObtenerRankingFaltas();
            ListaPorcentajeError.ItemsSource = _analizador.ObtenerPorcentajeError();
            ListaAsistencia.ItemsSource = _analizador.ObtenerDiasTrabajadosReales();
            ListaAlertas.ItemsSource = _analizador.ObtenerAlertasBajaProduccion();
        }

        private void BotonCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
