using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Python.Runtime;
using Whispr.Models;

namespace Whispr.Services
{
    public class WhisperModelService : IWhisperModelService, IDisposable
    {
        private readonly AppSettings _settings;
        private PyModule? _voiceToTextModule;
        private dynamic? _loadedModel;
        private bool _isModelLoaded = false;
        private string _loadedModelName = string.Empty;
        private string? _cacheDir;
        private readonly SynchronizationContext _pythonContext;

        public WhisperModelService(AppSettings settings)
        {
            _settings = settings;
            _cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "whisper_models");
            _pythonContext = SynchronizationContext.Current ?? new SynchronizationContext();
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
                        var result = action();
                        tcs.SetResult(result);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Python execution error: {ex.Message}");
                    tcs.SetException(ex);
                }
            }, null);

            return tcs.Task;
        }

        public void StartPythonRuntime()
        {
            try
            {
                if (PythonEngine.IsInitialized)
                {
                    Debug.WriteLine("Python engine is already initialized.");
                    return;
                }

                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string pythonHome = Path.Combine(baseDirectory, "python");
                string pythonDll = Path.Combine(pythonHome, "python311.dll");

                if (!File.Exists(pythonDll))
                    throw new FileNotFoundException($"Python DLL not found at {pythonDll}");

                Runtime.PythonDLL = pythonDll;
                PythonEngine.PythonHome = pythonHome;
                PythonEngine.Initialize();

                Debug.WriteLine("Python runtime initialized successfully.");

                using (Py.GIL())
                {
                    string scriptPath = Path.Combine(baseDirectory, "Assets").Replace("\\", "/");
                    string pythonCode = $"import sys; sys.path.append('{scriptPath}'); import voice_to_text";
                    PythonEngine.Exec(pythonCode);
                    _voiceToTextModule = (PyModule)Py.Import("voice_to_text");

                    Debug.WriteLine("Voice to text module imported successfully.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Python initialization failed: {ex.Message}");
                throw new Exception("Failed to initialize Python environment", ex);
            }
        }

        public async Task<bool> LoadModelAsync(string modelName)
        {
            if (_isModelLoaded && _loadedModelName == modelName)
            {
                Debug.WriteLine("Model is already loaded.");
                return true;
            }

            return await RunOnPythonThread(() =>
            {
                try
                {
                    using (Py.GIL())
                    {
                        if (_loadedModelName != modelName)
                        {
                            _isModelLoaded = false;

                            if (_loadedModel != null)
                            {
                                dynamic gc = Py.Import("gc");
                                gc.collect();
                            }

                            _loadedModel = _voiceToTextModule?.InvokeMethod("load_model", new PyObject[] { new PyString(modelName), new PyString(_cacheDir!) });

                            if (_loadedModel == null)
                            {
                                Debug.WriteLine($"Failed to load the model: {modelName}");
                                return false;
                            }

                            Debug.WriteLine($"Model '{modelName}' loaded successfully.");
                            _isModelLoaded = true;
                            _loadedModelName = modelName;
                        }
                        
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error loading model '{modelName}': {e.Message}");
                    _isModelLoaded = false;
                    return false;
                }
            });
        }

        public async Task<string> TranscribeAsync(byte[] audioData)
        {
            if (!_isModelLoaded || _loadedModel == null)
            {
                throw new InvalidOperationException("Model is not loaded. Please load the model before transcribing.");
            }

            return await RunOnPythonThread(() =>
            {
                try
                {
                    using (Py.GIL())
                    {
                        Debug.WriteLine("Starting transcription...");

                        using (PyObject pyAudioData = audioData.ToPython())
                        {
                            dynamic result = _voiceToTextModule!.InvokeMethod("transcribe", pyAudioData, _loadedModel);
                            string transcription = result.ToString();

                            if (string.IsNullOrEmpty(transcription))
                            {
                                throw new Exception("Transcription failed: result is empty.");
                            }

                            Debug.WriteLine("Transcription completed successfully.");
                            return transcription;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error during transcription: {e.Message}");
                    throw new Exception($"Failed to transcribe audio: {e.Message}", e);
                }
            });
        }

        public async Task<string> DownloadModelAsync(string modelName)
        {
            return await RunOnPythonThread(() =>
            {
                try
                {
                    using (Py.GIL())
                    {
                        Debug.WriteLine($"Downloading model: {modelName}...");

                        var result = _voiceToTextModule?.InvokeMethod("download_model", new PyObject[] { new PyString(modelName), new PyString(_cacheDir!) });

                        if (result == null)
                        {
                            throw new Exception("Download failed: result is null.");
                        }

                        Debug.WriteLine("Model download completed successfully.");
                        return "Model downloaded successfully";
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error downloading model '{modelName}': {e.Message}");
                    throw new Exception($"Failed to download model: {e.Message}", e);
                }
            });
        }

        public bool IsModelLoaded()
        {
            return _isModelLoaded;
        }

        public void Dispose()
        {
            PythonEngine.Shutdown();
        }
    }
}
