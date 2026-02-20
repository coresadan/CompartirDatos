using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace CompartirDatos
{
    public class AnalizadorDatos
    {
        private List<CaracteristicasDePiezas> _piezas;

        public AnalizadorDatos(List<CaracteristicasDePiezas> piezas)
        {
            _piezas = piezas ?? new List<CaracteristicasDePiezas>();
        }

        // UNIFICADOR DE NOMBRES: Si no hay nombre, lo llamamos "Sin Asignar" siempre
        private string Limpiar(string nombre)
            => string.IsNullOrWhiteSpace(nombre) ? "Sin Asignar" : nombre.Trim();

        public Dictionary<string, int> ObtenerRankingPiezas() =>
            _piezas.SelectMany(p => p.Fabricaciones)
                   .Where(f => f.EstadoDeLaPieza == "TERMINADO")
                   .GroupBy(f => Limpiar(f.Operario))
                   .ToDictionary(g => g.Key, g => g.Count());

        public Dictionary<string, int> ObtenerRankingFaltas() =>
            _piezas.SelectMany(p => p.Fabricaciones)
                   .Where(f => f.EstadoDeLaPieza == "FALTA/RECHAZO")
                   .GroupBy(f => Limpiar(f.Operario))
                   .ToDictionary(g => g.Key, g => g.Count());

        public Dictionary<string, double> ObtenerPorcentajeError() =>
            _piezas.SelectMany(p => p.Fabricaciones)
                   .GroupBy(f => Limpiar(f.Operario))
                   .ToDictionary(g => g.Key, g => {
                       double total = g.Count();
                       double faltas = g.Count(f => f.EstadoDeLaPieza == "FALTA/RECHAZO");
                       return total > 0 ? Math.Round((faltas / total) * 100, 2) : 0;
                   });

        public Dictionary<string, int> ObtenerDiasTrabajadosReales() =>
            _piezas.SelectMany(p => p.Fabricaciones)
                   .GroupBy(f => Limpiar(f.Operario))
                   .ToDictionary(g => g.Key, g => g.Select(f => f.Fecha.Date).Distinct().Count());

        public Dictionary<string, double> ObtenerPiezasPorHora() =>
            _piezas.SelectMany(p => p.Fabricaciones)
                   .GroupBy(f => Limpiar(f.Operario))
                   .ToDictionary(g => g.Key, g => {
                       double piezas = g.Count(f => f.EstadoDeLaPieza == "TERMINADO");
                       double dias = g.Select(f => f.Fecha.Date).Distinct().Count();
                       return dias > 0 ? Math.Round(piezas / (dias * 8), 2) : 0;
                   });

        public Dictionary<string, int> ObtenerAlertasBajaProduccion() =>
            _piezas.SelectMany(p => p.Fabricaciones)
                   .GroupBy(f => Limpiar(f.Operario))
                   .ToDictionary(g => g.Key, g => g.GroupBy(f => f.Fecha.Date)
                                                  .Count(dia => dia.Count(f => f.EstadoDeLaPieza == "TERMINADO") < 5));
    }
}