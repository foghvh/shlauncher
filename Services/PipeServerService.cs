using System.Threading.Tasks;
using System.Security.Principal;
using System.Diagnostics;
using System.IO.Pipes;
using System.IO;
using System.Text;
using System.Threading;

namespace shlauncher.Services
{
    public class PipeServerService
    {
        public const string RequestTokenCommand = "GET_TOKEN";
        public const string TokenPrefix = "TOKEN:";
        public const string ErrorPrefix = "ERROR:";
        public const string AckCommand = "TOKEN_RECEIVED_ACK";
        public const string EndOfMessage = "\n"; // Cliente usa ReadLineAsync, así que necesita esto

        public async Task<bool> SendTokenAsync(string pipeName, string tokenToSend, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(pipeName) || string.IsNullOrEmpty(tokenToSend))
            {
                Debug.WriteLine("[PipeServerService] Pipe name or token is null or empty.");
                return false;
            }

            NamedPipeServerStream? serverStream = null;
            try
            {
                Debug.WriteLine($"[PipeServerService] Starting pipe server on: {pipeName}");
                serverStream = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.InOut,
                    1, // MaxNumberOfServerInstances
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                Debug.WriteLine("[PipeServerService] Waiting for connection...");
                await serverStream.WaitForConnectionAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine("[PipeServerService] Connection cancelled before client connected.");
                    serverStream.Dispose();
                    return false;
                }
                Debug.WriteLine("[PipeServerService] Client connected.");

                // Usar leaveOpen: false para que los writers/readers se dispongan con el stream
                await using var writer = new StreamWriter(serverStream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true) { AutoFlush = false }; // AutoFlush false
                using var reader = new StreamReader(serverStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

                string? clientRequest = await reader.ReadLineAsync(cancellationToken);
                Debug.WriteLine($"[PipeServerService] Received from client: {clientRequest}");

                if (cancellationToken.IsCancellationRequested) return false;

                if (clientRequest == RequestTokenCommand)
                {
                    Debug.WriteLine($"[PipeServerService] Sending token to client: {tokenToSend.Substring(0, Math.Min(tokenToSend.Length, 20))}...");
                    await writer.WriteAsync($"{TokenPrefix}{tokenToSend}{EndOfMessage}");
                    await writer.FlushAsync(cancellationToken); // Flush explícito
                    Debug.WriteLine($"[PipeServerService] Token sent and flushed. Waiting for ACK from client...");

                    string? ackResponse = await reader.ReadLineAsync(cancellationToken);
                    Debug.WriteLine($"[PipeServerService] Received ACK from client: {ackResponse}");
                    if (cancellationToken.IsCancellationRequested) return false;

                    if (ackResponse == AckCommand)
                    {
                        Debug.WriteLine("[PipeServerService] ACK received. Token exchange successful.");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine($"[PipeServerService] Invalid or missing ACK from client: {ackResponse}. Token exchange failed.");
                        return false;
                    }
                }
                else
                {
                    Debug.WriteLine($"[PipeServerService] Invalid request from client: {clientRequest}");
                    await writer.WriteAsync($"{ErrorPrefix}Invalid request.{EndOfMessage}");
                    await writer.FlushAsync(cancellationToken); // Flush explícito
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[PipeServerService] Operation cancelled.");
                return false;
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[PipeServerService] IOException: {ex.Message}. Pipe may have been closed or broken.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PipeServerService] Unexpected error: {ex.Message}");
                return false;
            }
            finally
            {
                if (serverStream != null)
                {
                    if (serverStream.IsConnected)
                    {
                        // serverStream.Disconnect(); // Desconectar antes de Dispose si sigue conectado, aunque Dispose debería manejarlo.
                    }
                    await serverStream.DisposeAsync();
                }
                Debug.WriteLine($"[PipeServerService] Server for pipe {pipeName} is shutting down.");
            }
        }
    }
}
