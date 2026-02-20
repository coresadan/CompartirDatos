using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows;
using System.Text.Json;
using System.Threading.Tasks;
using CompartirDatos;

namespace CompartirDatos
{
    public class ServicioPipeEmisor
    {
        private const string PipePiezas = "PipeSantosPiezas";
        private const string PipeRespuestas = "PipeSantosRespuesta";

        // MÉTODO A: Para enviar la pieza completa (JSON)
        public async Task EnviarPiezaAsync(CaracteristicasDePiezas pieza)
        {
            await EnviarTextoAsync(pieza != null ? JsonSerializer.Serialize(pieza) : "", PipePiezas);
        }

        // MÉTODO B: Para enviar comandos simples (como el aviso de nueva lista)
        public async Task EnviarRespuestaOficinaAsync(string mensaje)
        {
            await EnviarTextoAsync(mensaje, PipeRespuestas);
        }

        private async Task EnviarTextoAsync(string contenido, string nombrePipe)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", nombrePipe, PipeDirection.Out);
                await client.ConnectAsync(2000);

                using var writer = new StreamWriter(client, Encoding.UTF8);
                await writer.WriteLineAsync(contenido);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Pipe ({nombrePipe}): {ex.Message}");
            }
        }
    }
}
