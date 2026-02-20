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
    /// Lógica de interacción para DetallesDeLaLista.xaml
    /// </summary>
    public partial class DetallesDeLaLista : Window
    {
        List<CaracteristicasDePiezas> listaPiezasMainWindow;

        public DetallesDeLaLista(List<CaracteristicasDePiezas> origen)
        {
            InitializeComponent();

            listaPiezasMainWindow = origen;
        }

        public void RestaurarColumnasPiezas()
        {
            OcultarColumnas();

            colPieza.Visibility = Visibility.Visible;
            colColor.Visibility = Visibility.Visible;
            colLargo.Visibility = Visibility.Visible;
            colAncho.Visibility = Visibility.Visible;

            colPieza.Header = "Pieza";
        }


        public void BotonTerminadasClick(object sender, RoutedEventArgs e)
        {
            dgDetalles.ItemsSource = null;
            RestaurarColumnasPiezas();

            if (listaPiezasMainWindow != null && listaPiezasMainWindow.Count > 0)
            {
                var piezasterminadas = from t in listaPiezasMainWindow
                                       where t.EstaTerminada == true
                                       select t;

                dgDetalles.ItemsSource = piezasterminadas;
            }
            else
            {
                MessageBox.Show("No se han recibido datos de la ventana principal.");
            }
        }

        private void BotonRestantesClick(object sender, RoutedEventArgs e)
        {
            RestaurarColumnasPiezas();

            if (listaPiezasMainWindow != null && listaPiezasMainWindow.Count > 0)
            {
                var piezasRestantes = from r in listaPiezasMainWindow
                                      where r.EstaTerminada == false
                                      select r;

                dgDetalles.ItemsSource = piezasRestantes;
            }
            else
            {
                MessageBox.Show("No se han recibido datos de la ventana principal.");
            }
        }

        private void BotonRechazadasClick(object sender, RoutedEventArgs e)
        {
            RestaurarColumnasPiezas();

            if (listaPiezasMainWindow != null && listaPiezasMainWindow.Count > 0)
            {
                var piezasRechazadas = from f in listaPiezasMainWindow
                                       where f.Datos.Falta == true
                                       select f;

                listaPiezasMainWindow.Where(x => x.Datos.Falta);

                dgDetalles.ItemsSource = piezasRechazadas;
            }
            else
            {
                MessageBox.Show("No se han recibido datos de la ventana principal.");
            }
        }

        public void OcultarColumnas()
        {
            colTurno.Visibility = Visibility.Collapsed;
            colPin.Visibility = Visibility.Collapsed;
            colRol.Visibility = Visibility.Collapsed;
            colNacio.Visibility = Visibility.Collapsed;
            colAnti.Visibility = Visibility.Collapsed;

            colColor.Visibility = Visibility.Collapsed;
            colLargo.Visibility = Visibility.Collapsed;
            colAncho.Visibility = Visibility.Collapsed;
        }


        private void BotonUsuariosClick(object sender, RoutedEventArgs e)
        {
            dgDetalles.ItemsSource = null;
            OcultarColumnas();

            colPieza.Header = "Operario";
            dgDetalles.ItemsSource = null;

            dgDetalles.ItemsSource = GestionUsuarios.ListaUsuarios;
        }


        private void btnAdmin_Click(object sender, RoutedEventArgs e)
        {
            string passwordAdmin = Microsoft.VisualBasic.Interaction.InputBox("🔑 Ingrese clave de Administrador:", "Zona de Seguridad", "");

            if (passwordAdmin == "120920")
            {
                Log.Information("🔓 Modo Administrador activado en ventana de detalles.");
                MessageBox.Show("✅ Acceso concedido. Ahora puede mostrar los PINs.", "Seguridad Santos");
                datosAdministrador.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("🚫 Clave incorrecta. Acceso denegado.", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                Log.Information("🚫 Intento de acceso al modo Administrador denegado. Contraseña errónea.");
            }
        }

        private void OrdenNombre_Click(object sender, RoutedEventArgs e)
        {
            if (dgDetalles.ItemsSource is IEnumerable<CaracteristicasDePiezas> piezas)
            {
                dgDetalles.ItemsSource = piezas.OrderBy(x => x.Nombre).ToList();
            }

            else if (dgDetalles.ItemsSource is IEnumerable<Usuario> usuarios)
            {
                dgDetalles.ItemsSource = usuarios.OrderBy(x => x.Nombre).ToList();
            }
        }

        private void OrdenLargo_Click(object sender, RoutedEventArgs e)
        {
            if (dgDetalles.ItemsSource is IEnumerable<CaracteristicasDePiezas> piezasEnPantalla)
            {
                dgDetalles.ItemsSource =  piezasEnPantalla.OrderBy(p => p.Largo).ToList();
            }
        }

        private void OrdenAncho_Click(object sender, RoutedEventArgs e)
        {
            var piezasActuales = dgDetalles.ItemsSource as IEnumerable<CaracteristicasDePiezas>;
            if (piezasActuales != null)
            {
                dgDetalles.ItemsSource = piezasActuales.OrderBy(variable => variable.Ancho).ToList();
            }
        }

        private void OrdenColor_Click(object sender, RoutedEventArgs e)
        {
            var piezasActuales = dgDetalles.ItemsSource as IEnumerable<CaracteristicasDePiezas>;
            if (piezasActuales != null)
            {
                dgDetalles.ItemsSource = piezasActuales.OrderBy(variable => variable.Color).ToList();
            }
        }


        private void BotonMostrarPINUsuariosClick(object sender, RoutedEventArgs e)
        {
            var listaUsuarios = GestionUsuarios.ListaUsuarios;

            if (dgDetalles.ItemsSource is IEnumerable<Usuario> usuarios)
            {
                OcultarColumnas();
                datosUsuario.Visibility = Visibility.Visible;
            }
        }

        private void OrdenTurno_Click(object sender, RoutedEventArgs e)
        {
            if (dgDetalles.ItemsSource is IEnumerable<Usuario> usuarios)
            {
                OcultarColumnas();
                colTurno.Visibility = Visibility.Visible;
                dgDetalles.ItemsSource = usuarios.OrderBy(u => u.Turno).ToList();
            }
        }

        private void OrdenPin_Click(object sender, RoutedEventArgs e)
        {
            if (dgDetalles.ItemsSource is IEnumerable<Usuario> usuarios)
            {
                OcultarColumnas();
                colPin.Visibility = Visibility.Visible;
                dgDetalles.ItemsSource = usuarios.OrderBy(u => u.Pin).ToList();
            }
        }

        private void OrdenAntiguedad_Click(object sender, RoutedEventArgs e)
        {
            if (dgDetalles.ItemsSource is IEnumerable<Usuario> usuarios)
            {
                OcultarColumnas();
                colAnti.Visibility = Visibility.Visible;
                dgDetalles.ItemsSource = usuarios.OrderBy(u => u.Antiguedad).ToList();
            }
        }

        private void OrdenNacionalidad_Click(object sender, RoutedEventArgs e)
        {
            if (dgDetalles.ItemsSource is IEnumerable<Usuario> usuarios)
            {
                OcultarColumnas();
                colNacio.Visibility = Visibility.Visible;
                dgDetalles.ItemsSource = usuarios.OrderBy(u => u.Nacionalidad).ToList();
            }
        }

        private void OrdenRol_Click(object sender, RoutedEventArgs e)
        {
            if (dgDetalles.ItemsSource is IEnumerable<Usuario> usuarios)
            {
                OcultarColumnas();
                colRol.Visibility = Visibility.Visible;
                dgDetalles.ItemsSource = usuarios.OrderBy(u => u.Rol).ToList();
            }
        }

        private void BotonCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
