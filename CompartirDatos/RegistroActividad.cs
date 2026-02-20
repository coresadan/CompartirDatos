using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompartirDatos
{
    public class RegistroActividad
    {
        public string UsuarioSesion { get; set; }
        public string Pin { get; set; }
        public DateTime InicioDeSesion { get; set; }
        public DateTime FinDeSesion { get; set; }

        //Los datos para los Gráficos de Producción
        public int PiezasTerminadas { get; set; }
        public int PiezasConFalta { get; set; }
        public double TotalHoras => (FinDeSesion - InicioDeSesion).TotalHours;


        
    }
}
