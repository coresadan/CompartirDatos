using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace CompartirDatos
{
    internal class ServicioPipeReceptor
    {
        private const string NombrePipe = "PipeSantosRespuesta";

        public event EventHandler<string>? PiezaRecibida;

        public async Task IniciarEscuchaAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(NombrePipe, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync(ct);
                    using var reader = new StreamReader(server);
                    string? respuesta = await reader.ReadLineAsync();

                    if (!string.IsNullOrEmpty(respuesta))
                    {
                        PiezaRecibida?.Invoke(this, respuesta);
                    }
                }
                catch (Exception)
                {
                    await Task.Delay(1000, ct);
                }
            }
        }
    }
}
