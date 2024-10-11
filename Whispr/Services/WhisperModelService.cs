using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace Whispr.Services
{
    public class WhisperModelService : IWhisperModelService, IDisposable
    {
        private readonly string _whisperRuntimePath;
        private bool _isModelLoaded = false;
        private Process? _serverProcess;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private bool _disposed = false;
        private const int Port = 5000;
        private const int MaxRetries = 30;
        private const int RetryDelay = 1000;

        public WhisperModelService()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string assetsDirectory = Path.Combine(baseDirectory, "Assets");
            _whisperRuntimePath = Path.Combine(assetsDirectory, "whisper_api_runtime.exe");
        }

        public async Task<bool> LoadModelAsync()
        {
            if (_isModelLoaded) return true;

            try
            {
                _serverProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _whisperRuntimePath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                _serverProcess.OutputDataReceived += (sender, e) => Debug.WriteLine($"Server output: {e.Data}");
                _serverProcess.ErrorDataReceived += (sender, e) => Debug.WriteLine($"Server error: {e.Data}");

                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();

                for (int attempt = 0; attempt < MaxRetries; attempt++)
                {
                    await Task.Delay(RetryDelay);
                    if (await TryConnectAndCheckModelAsync())
                    {
                        _isModelLoaded = true;
                        return true;
                    }
                }

                Debug.WriteLine("Failed to connect and load the model after multiple attempts.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading model: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TryConnectAndCheckModelAsync()
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync("localhost", Port);
                _stream = _tcpClient.GetStream();

                return await CheckModelReadyAsync();
            }
            catch (SocketException)
            {
                _tcpClient?.Close();
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error connecting to server: {ex.Message}");
                _tcpClient?.Close();
                return false;
            }
        }

        private async Task<bool> CheckModelReadyAsync()
        {
            try
            {
                var request = new { action = "check_ready" };
                var requestJson = JsonSerializer.Serialize(request);
                byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson + "\n");
                await _stream!.WriteAsync(requestBytes);

                using var reader = new StreamReader(_stream, Encoding.UTF8, false, 1024, true);
                var response = await reader.ReadLineAsync();
                if (response != null)
                {
                    var responseObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response);
                    if (responseObj!.TryGetValue("ready", out var readyProp) && readyProp.ValueKind == JsonValueKind.True)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking model readiness: {ex.Message}");
            }
            return false;
        }

        public async Task<string> TranscribeAsync(byte[] audioData, Action<int> progressCallback)
        {
            if (!_isModelLoaded || _stream == null)
            {
                Debug.WriteLine("Model is not loaded. Attempting to load...");
                if (!await LoadModelAsync())
                {
                    return "Error: Unable to load the transcription model.";
                }
            }

            try
            {
                var request = new { action = "transcribe", audio = Convert.ToBase64String(audioData) };
                var requestJson = JsonSerializer.Serialize(request);
                byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson + "\n");
                await _stream!.WriteAsync(requestBytes);

                using var reader = new StreamReader(_stream, Encoding.UTF8, false, 1024, true);
                while (true)
                {
                    var response = await reader.ReadLineAsync();
                    if (response == null) break;

                    try
                    {
                        var responseObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response);
                        if (responseObj!.TryGetValue("progress", out var progressProp) && progressProp.ValueKind == JsonValueKind.Number)
                        {
                            double progressValue = progressProp.GetDouble();
                            int progressPercentage = (int)(progressValue * 100);
                            progressCallback(Math.Min(progressPercentage, 100));
                        }
                        else if (responseObj.TryGetValue("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.String)
                        {
                            progressCallback(100);
                            return resultProp.GetString() ?? string.Empty;
                        }
                        else if (responseObj.TryGetValue("error", out var errorProp) && errorProp.ValueKind == JsonValueKind.String)
                        {
                            return $"Error from transcription service: {errorProp.GetString()}";
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        Debug.WriteLine($"JSON parsing error: {jsonEx.Message}. Response: {response}");
                    }
                }

                return "Error: Unexpected end of communication";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during transcription: {ex.Message}");
                return $"Error during transcription: {ex.Message}";
            }
        }

        public bool IsModelLoaded()
        {
            return _isModelLoaded;
        }

        private void StopServer()
        {
            if (_stream != null && _tcpClient != null)
            {
                try
                {
                    var request = new { action = "shutdown" };
                    var requestJson = JsonSerializer.Serialize(request);
                    byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson + "\n");
                    _stream.Write(requestBytes, 0, requestBytes.Length);
                    _stream.Flush();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error shutting down server gracefully: {ex.Message}");
                }

                _stream.Close();
                _tcpClient.Close();
            }

            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                _serverProcess.WaitForExit(5000);
                if (!_serverProcess.HasExited)
                {
                    _serverProcess.Kill();
                }
                _serverProcess.Dispose();
                _serverProcess = null;
            }

            System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
                .Where(x => x.Port == Port)
                .ToList()
                .ForEach(x => { try { new TcpClient(x.Address.ToString(), x.Port).Close(); } catch { } });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopServer();
                }
                _disposed = true;
            }
        }

        ~WhisperModelService()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}