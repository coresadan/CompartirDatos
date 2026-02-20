using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CompartirDatos
{
    public interface ApiPiezas
    {

        //   [Get("/Piezas/")]                                              //Error 500
        //   Task<List<CaracteristicasDePiezas>> ObtenerTodasLasPiezas();

        [Get("/DimeListas/")]
        Task<List<string>> ObtenerNombresDeArchivos();

        [Get("/DimePiezasLista/{nombre}")]
        Task<List<CaracteristicasDePiezas>> ObtenerPiezasDeArchivo(string nombre);


        [Get("/Usuarios")]
        Task<List<Usuario>> ObtenerUsuariosDelServidor();

    }
        public class ConexionConMiAPI
        {
            private ApiPiezas web;

            public ConexionConMiAPI()
            {
                web = RestService.For<ApiPiezas>("http://localhost:5106");
            }


            //    public async Task<List<CaracteristicasDePiezas>> CargarPiezasDelTutor()
            //    {
            //        return await web.ObtenerTodasLasPiezas();
            //    }

            public async Task<List<string>> CargarNombresDeArchivos()
            {
                return await web.ObtenerNombresDeArchivos();
        }
        public async Task<List<CaracteristicasDePiezas>> CargarPiezasPorNombre(string nombreArchivo)
        {
            return await web.ObtenerPiezasDeArchivo(nombreArchivo);
        }
        public async Task<List<Usuario>> CargarUsuariosDelServidor()
        {
            return await web.ObtenerUsuariosDelServidor();
        }
    }
}