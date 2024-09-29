using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Python.Runtime;

namespace Whispr.Services
{
    public class WhisperModelService : IWhisperModelService
    {
        public async Task DownloadAndConvertModelAsync(string modelName)
        {
            string cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "whisper_models");

            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                sys.path.append("Assets");
                var voicetotext = Py.Import("voice_to_text");
                var modelname = new PyString(modelName);
                var cacheDirPy = new PyString(cacheDir);

                await Task.Run(() =>
                {
                    voicetotext.InvokeMethod("download_model", new PyObject[] { modelname, cacheDirPy });
                    voicetotext.InvokeMethod("convert_model", new PyObject[] { modelname, cacheDirPy });
                });
            }
        }

        public async Task LoadModelAsync(string modelName)
        {
            string cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "whisper_models");

            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                sys.path.append("Assets");
                var voicetotext = Py.Import("voice_to_text");
                var modelname = new PyString(modelName);
                var cacheDirPy = new PyString(cacheDir);

                await Task.Run(() =>
                {
                    voicetotext.InvokeMethod("load_model", new PyObject[] { modelname, cacheDirPy });
                });
            }
        }

        public async Task<string> TranscribeAsync(byte[] audioData)
        {
            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                sys.path.append("Assets");
                var voicetotext = Py.Import("voice_to_text");

                dynamic builtins = Py.Import("builtins");
                PyObject audioDataPy = builtins.bytearray(audioData);

                var result = await Task.Run(() =>
                {
                    return voicetotext.InvokeMethod("transcribe", new PyObject[] { audioDataPy }).ToString();
                });

                return "Transcription result: " + result;
            }
        }
    }
}