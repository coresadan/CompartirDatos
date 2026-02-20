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
    /// Lógica de interacción para Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        public Window2()
        {
            InitializeComponent();
        }


        private void SeleccionMaquinaKuper(object sender, MouseButtonEventArgs e)
        {
            if (this.Owner is MainWindow principal)
            {
                principal.MaquinaActual = "Kuper";
            }
            this.DialogResult = true;
            this.Close();

        }

        private void SeleccionMaquinaCanteadora(object sender, MouseButtonEventArgs e)
        {
            if (this.Owner is MainWindow principal)
            {
                principal.MaquinaActual = "Canteadora";
            }
            this.DialogResult = true;
            this.Close();
        }

    }
}
