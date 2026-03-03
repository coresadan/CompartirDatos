using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CompartirDatos
{
    public static class EstadosPieza
    {
        public const string Terminado = "Terminado";
        public const string Falta = "FALTA/RECHAZO";
        public const string Pendiente = "Pendiente";
        public const string Urgente = "Urgente";
    }

    public class CaracteristicasDePiezas : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // --- PROPIEDADES CON NOTIFICACIÓN (UI) ---

        private bool _estaTerminada;
        public bool EstaTerminada
        {
            get => _estaTerminada;
            set { if (_estaTerminada != value) { _estaTerminada = value; OnPropertyChanged(); } }
        }

        private bool _piezaurgente;
        public bool Piezaurgente
        {
            get => _piezaurgente;
            set { if (_piezaurgente != value) { _piezaurgente = value; OnPropertyChanged(); } }
        }

        private string _estado = EstadosPieza.Pendiente;
        public string Estado
        {
            get => _estado;
            set { if (_estado != value) { _estado = value; OnPropertyChanged(); } }
        }

        // --- DATOS DE IDENTIFICACIÓN Y MEDIDAS ---

        public int Id { get; set; }

        // El Index ayuda a CsvHelper a leer el Excel aunque cambien nombres
        [Index(0)] public string Nombre { get; set; }
        [Index(1)] public string Color { get; set; }
        [Index(2)] public decimal Largo { get; set; }
        [Index(3)] public decimal Ancho { get; set; }

        // --- OBJETOS RELACIONADOS ---

        public CaracteristicasDePiezas2 Datos { get; set; }
        public List<Fabricacion> Fabricaciones { get; set; }

        public CaracteristicasDePiezas()
        {
            Datos = new CaracteristicasDePiezas2();
            Fabricaciones = new List<Fabricacion>();
        }
    }

    public class CaracteristicasDePiezas2 : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool _falta;
        public bool Falta { get => _falta; set { _falta = value; OnPropertyChanged(); } }

        private bool _error;
        public bool Error { get => _error; set { _error = value; OnPropertyChanged(); } }

        // Recuperamos esta propiedad para evitar errores de compilación
        private bool _esFaltaParcial;
        public bool EsFaltaParcial { get => _esFaltaParcial; set { _esFaltaParcial = value; OnPropertyChanged(); } }
    }

    public class Fabricacion
    {
        public DateTime Fecha { get; set; }
        public string Maquina { get; set; }
        public string EstadoDeLaPieza { get; set; }
        public string Operario { get; set; }
        // Añadimos esto por si tu lógica de taller lo necesita
        public int OperarioId { get; set; }
    }
}