using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CompartirDatos
{
    public class Usuario
    {
        public string Nombre { get; set; }
        public string Rol {  get; set; }
        public string Turno { get; set; }
        public string Pin {  get; set; }

        public string Antiguedad { get; set; }
        public string Nacionalidad { get; set; }
    }
}
