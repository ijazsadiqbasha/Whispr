using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Python.Runtime;
using Whispr.Models;

namespace Whispr.Services
{
    public class WhisperModelService : IWhisperModelService, IDisposable
    {
        private readonly string _cacheDir;
        private readonly SynchronizationContext _pythonContext;
        private readonly AppSettings _appSettings;
        private bool _isPythonInitialized = false;
        private dynamic _sys;
        private dynamic _voice_to_text;
        private dynamic _model;
        private bool _isModelLoaded = false;

        public WhisperModelService(AppSettings appSettings)
        {
            _appSettings = appSettings;
            _pythonContext = SynchronizationContext.Current ?? new SynchronizationContext();
            _cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "whisper_models");
            Directory.CreateDirectory(_cacheDir);

            if (_appSettings.IsPythonInstalled)
            {
                InitializePythonEnvironment();
            }
        }

        private void InitializePythonEnvironment()
        {
            if (!_isPythonInitialized)
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string pythonHome = Path.Combine(baseDirectory, "python");
                string pythonDll = Path.Combine(pythonHome, "python311.dll");

                Debug.WriteLine($"Setting PYTHONNET_PYDLL to: {pythonDll}");
                Debug.WriteLine($"Setting PYTHONHOME to: {pythonHome}");

                Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDll);
                Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome);

                Runtime.PythonDLL = pythonDll;
                PythonEngine.Initialize();
                _isPythonInitialized = true;
                Debug.WriteLine("Python runtime initialized successfully.");

                using (Py.GIL())
                {
                    _sys = Py.Import("sys");
                    string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets").Replace("\\", "/");
                    _sys.path.append(scriptPath);
                    _voice_to_text = Py.Import("voice_to_text");
                    Debug.WriteLine("Voice to text module imported successfully.");
                }
            }
        }

        public void Dispose()
        {
            if (_isPythonInitialized)
            {
                PythonEngine.Shutdown();
                _isPythonInitialized = false;
            }
        }

        private Task<T> RunOnPythonThread<T>(Func<T> action)
        {
            var tcs = new TaskCompletionSource<T>();
            _pythonContext.Post(_ =>
            {
                try
                {
                    using (Py.GIL())
                    {
                        Debug.WriteLine("Acquired Python GIL");
                        var result = action();
                        Debug.WriteLine("Action completed successfully");
                        tcs.SetResult(result);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception in RunOnPythonThread: {ex}");
                    tcs.SetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        public async Task<bool> LoadModelAsync(string modelName)
        {
            return await RunOnPythonThread(() =>
            {
                try
                {
                    if (_isModelLoaded)
                    {
                        Debug.WriteLine("Model already loaded, skipping load operation.");
                        return true;
                    }

                    Debug.WriteLine($"Starting to load model: {modelName}");
                    _model = _voice_to_text.load_model(modelName, _cacheDir, new Action<int>(progress =>
                    {
                        Debug.WriteLine($"Model Load Progress: {progress}%");
                    }));
                    Debug.WriteLine("Model loading completed successfully");
                    _isModelLoaded = true;
                    return true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Exception in LoadModelAsync: {e.Message}");
                    Debug.WriteLine($"Stack Trace: {e.StackTrace}");
                    _isModelLoaded = false;
                    return false;
                }
            });
        }

        public async Task<string> TranscribeAsync(byte[] audioData)
        {
            if (!_isModelLoaded)
            {
                throw new InvalidOperationException("Model is not loaded. Please load the model before transcribing.");
            }

            return await RunOnPythonThread(() =>
            {
                try
                {
                    Debug.WriteLine("Starting transcription");
                    var result = _voice_to_text.transcribe(audioData, _model, new Action<int>(progress =>
                    {
                        Debug.WriteLine($"Transcription Progress: {progress}%");
                    }));
                    Debug.WriteLine("Transcription completed successfully");
                    return result;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Exception in TranscribeAsync: {e.Message}");
                    Debug.WriteLine($"Stack Trace: {e.StackTrace}");
                    throw new Exception($"Failed to transcribe audio: {e.Message}", e);
                }
            });
        }

        public Task DownloadModelAsync(string modelName)
        {
            return RunOnPythonThread(() =>
            {
                try
                {
                    Debug.WriteLine($"Starting to download model: {modelName}");
                    var result = _voice_to_text.download_model(modelName, _cacheDir, new Action<int>(progress =>
                    {
                        Debug.WriteLine($"Download Progress: {progress}%");
                    }));
                    Debug.WriteLine("Model download completed successfully");
                    return "Model downloaded successfully";
                }
                catch (PythonException pe)
                {
                    Debug.WriteLine($"Python Exception in DownloadModelAsync: {pe.Message}");
                    Debug.WriteLine($"Python Traceback: {pe.StackTrace}");
                    throw new Exception($"Failed to download model: {pe.Message}", pe);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Exception in DownloadModelAsync: {e.Message}");
                    Debug.WriteLine($"Stack Trace: {e.StackTrace}");
                    throw new Exception($"Failed to download model: {e.Message}", e);
                }
            });
        }

        public bool IsModelLoaded()
        {
            return _isModelLoaded;
        }
    }
}