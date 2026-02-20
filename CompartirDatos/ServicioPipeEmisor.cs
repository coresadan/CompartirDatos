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
        private const string NombrePipe = "PipeSantosPiezas";

        public async Task EnviarPiezaAsync(CaracteristicasDePiezas pieza)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", NombrePipe, PipeDirection.Out);

                await client.ConnectAsync(2000);

                using var writer = new StreamWriter(client);

                string json = pieza != null ? JsonSerializer.Serialize(pieza) : "";

                await writer.WriteLineAsync(json);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar: {ex.Message}");
            }
        }
    }
}
