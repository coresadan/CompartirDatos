using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CompartirDatos
{
    public class CaracteristicasDePiezas : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool _estaTerminada;
        public bool EstaTerminada
        {
            get => _estaTerminada;
            set
            {
                if (_estaTerminada != value)
                {
                    _estaTerminada = value;
                    OnPropertyChanged();
                }
            }
        }
        public string EstadoTerminada { get; set; }
        public bool DimeEstado(bool estado)
        {
            _estaTerminada = estado;
            return _estaTerminada;
        }
        public CaracteristicasDePiezas()
        {
            Datos = new CaracteristicasDePiezas2();
            Fabricaciones = new List<Fabricacion>();
        }
        public int Id { get; set; }
        [Index(0)] // Lee la primera columna sin importar cómo se llame
        public string Nombre { get; set; }

        [Index(1)]
        public string Color { get; set; }

        [Index(2)]
        public decimal Largo { get; set; }

        [Index(3)]
        public decimal Ancho { get; set; }

        public CaracteristicasDePiezas2 Datos { get; set; }

        public List<Fabricacion> Fabricaciones { get; set; }


    }

    public class CaracteristicasDePiezas2 : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool _falta;
        public bool Falta { get => _falta; set { _falta = value; OnPropertyChanged(); } }

        private bool _error;
        public bool Error { get => _error; set { _error = value; OnPropertyChanged(); } }

        private bool _esFaltaParcial;
        public bool EsFaltaParcial { get => _esFaltaParcial; set { _esFaltaParcial = value; OnPropertyChanged(); } }


        public string Nombre { get; set; }
        public string Color { get; set; }
        public decimal Largo { get; set; }
        public decimal Ancho { get; set; }

        public override string ToString() => $"Nombre: {Nombre}, Error: {Error}";
    }
    public class Fabricacion
    {
        public DateTime Fecha { get; set; }
        public string Maquina { get; set; }
        public string EstadoDeLaPieza { get; set; }
        public string Operario { get; set; }

    }
}
