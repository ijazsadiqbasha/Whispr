using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Whispr.Services
{
    public class PythonInstallationService : IPythonInstallationService
    {
        private readonly IConfiguration _configuration;
        private readonly string _pythonDirectory;
        private readonly string _downloadUrl;
        private readonly string[] _requiredPackages;

        public PythonInstallationService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _pythonDirectory = _configuration["Python:Directory"] ?? throw new InvalidOperationException("Python:Directory is not configured");
            _downloadUrl = _configuration["Python:DownloadUrl"] ?? throw new InvalidOperationException("Python:DownloadUrl is not configured");
            _requiredPackages = _configuration.GetSection("Python:RequiredPackages").Get<string[]>() ?? [];
        }

        public async Task<bool> CheckPythonInstallationAsync()
        {
            Debug.WriteLine("Checking Python installation");
            if (!await IsPythonInstalledAsync())
            {
                Debug.WriteLine("Python is not installed");
                return false;
            }

            var isVerified = await VerifyPythonInstallationAsync();
            if (!isVerified)
            {
                Debug.WriteLine("Python installation failed verification");
                return false;
            }

            foreach (var package in _requiredPackages)
            {
                var isPackageInstalled = await CheckPackageInstalledAsync(package);
                if (!isPackageInstalled)
                {
                    Debug.WriteLine($"Package {package} is not installed");
                    return false;
                }
            }

            Debug.WriteLine("Python installation is complete and verified");
            return true;
        }

        public async Task DownloadPythonAsync(IProgress<double> progress)
        {
            Debug.WriteLine("Starting Python download");
            try
            {
                using var client = new HttpClient();
                using var response = await client.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var downloadedBytes = 0L;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream("python.zip", FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(), default)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), default);
                        downloadedBytes += bytesRead;
                        if (totalBytes > 0)
                            progress.Report((double)downloadedBytes / totalBytes);
                    }
                }

                Debug.WriteLine("Python download completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error downloading Python: {ex.Message}");
                throw;
            }
        }

        public async Task ExtractPythonAsync(IProgress<double> progress)
        {
            Debug.WriteLine("Starting Python extraction");
            try
            {
                Directory.CreateDirectory(_pythonDirectory);

                await Task.Run(() =>
                {
                    using var archive = ZipFile.OpenRead("python.zip");
                    var totalEntries = archive.Entries.Count;
                    var extractedEntries = 0;

                    foreach (var entry in archive.Entries)
                    {
                        var destinationPath = Path.GetFullPath(Path.Combine(_pythonDirectory, entry.FullName));
                        if (destinationPath.StartsWith(Path.GetFullPath(_pythonDirectory), StringComparison.OrdinalIgnoreCase))
                        {
                            if (Path.GetFileName(destinationPath).Length > 0)
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                                entry.ExtractToFile(destinationPath, true);
                            }
                        }
                        extractedEntries++;
                        progress.Report((double)extractedEntries / totalEntries);
                    }
                });

                File.Delete("python.zip");
                Debug.WriteLine("Python extraction completed");

                await EnsurePythonEnvironment();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting Python: {ex.Message}");
                throw;
            }
        }

        public string[] GetRequiredPackages() => _requiredPackages;

        public string GetPythonPath()
        {
            var path = Path.Combine(_pythonDirectory, "python.exe");
            Debug.WriteLine($"Python path: {path}");
            return path;
        }

        public async Task InstallPackagesAsync(IProgress<double> progress, Action<string> updateStatus)
        {
            Debug.WriteLine("Starting package installation");

            try
            {
                if (!await IsPythonInstalledAsync())
                {
                    throw new InvalidOperationException("Python is not installed. Please install Python first.");
                }

                Debug.WriteLine($"Python directory: {_pythonDirectory}");
                Debug.WriteLine($"Python executable: {GetPythonPath()}");

                var totalPackages = _requiredPackages.Length;
                for (int i = 0; i < totalPackages; i++)
                {
                    var package = _requiredPackages[i];
                    updateStatus($"Installing package: {package}");
                    Debug.WriteLine($"Installing package: {package}");
                    try
                    {
                        await RunPythonCommandAsync($"-m pip install {package} --upgrade");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to install package {package}: {ex.Message}");
                        updateStatus($"Failed to install package {package}: {ex.Message}");
                    }
                    progress.Report((double)(i + 1) / totalPackages);
                }

                Debug.WriteLine("Package installation completed");
                updateStatus("Package installation completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during package installation: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsPythonInstalledAsync()
        {
            Debug.WriteLine("Checking if Python is installed");
            return await Task.Run(() => File.Exists(GetPythonPath()));
        }

        public async Task SetupPipAsync(IProgress<double> progress)
        {
            Debug.WriteLine("Setting up pip");

            if (!await IsPythonInstalledAsync())
            {
                throw new InvalidOperationException("Python is not installed. Please install Python first.");
            }

            try
            {
                progress.Report(0.1);
                using var client = new HttpClient();
                var getPipScript = await client.GetStringAsync("https://bootstrap.pypa.io/get-pip.py");
                var getPipPath = Path.Combine(_pythonDirectory, "get-pip.py");
                await File.WriteAllTextAsync(getPipPath, getPipScript);

                progress.Report(0.3);
                var output = await RunPythonCommandAsync("get-pip.py");
                Debug.WriteLine($"get-pip.py output: {output}");

                progress.Report(0.6);
                File.Delete(getPipPath);

                var scriptsPath = Path.Combine(_pythonDirectory, "Scripts");
                var currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process) ?? string.Empty;
                if (!currentPath.Contains(scriptsPath))
                {
                    Environment.SetEnvironmentVariable("PATH", $"{currentPath};{scriptsPath}", EnvironmentVariableTarget.Process);
                    Debug.WriteLine($"Added {scriptsPath} to PATH");
                }

                progress.Report(0.8);
                output = await RunPythonCommandAsync("-m pip --version");
                Debug.WriteLine($"Pip version: {output}");

                progress.Report(1.0);
                Debug.WriteLine("Pip setup completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up pip: {ex.Message}");
                throw new Exception("Failed to set up pip", ex);
            }
        }

        public async Task<bool> VerifyPythonInstallationAsync()
        {
            Debug.WriteLine("Verifying Python installation");
            if (!await IsPythonInstalledAsync())
                return false;

            try
            {
                var output = await RunPythonCommandAsync("--version");
                Debug.WriteLine($"Python version: {output.Trim()}");
                return output.Contains("Python 3.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error verifying Python installation: {ex.Message}");
                return false;
            }
        }

        private async Task<string> RunPythonCommandAsync(string arguments)
        {
            var pythonPath = GetPythonPath();
            Debug.WriteLine($"Running Python command: {pythonPath} {arguments}");

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = _pythonDirectory
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(error))
            {
                Debug.WriteLine($"Warning/Error running Python command: {error}");
            }

            if (!string.IsNullOrEmpty(output))
            {
                Debug.WriteLine($"Python command output: {output}");
            }

            if (process.ExitCode != 0)
            {
                throw new Exception($"Python command failed with exit code {process.ExitCode}. Error: {error}");
            }

            return output.Trim();
        }

        private async Task<bool> CheckPackageInstalledAsync(string packageName)
        {
            try
            {
                var output = await RunPythonCommandAsync($"-m pip show {packageName}");
                return !string.IsNullOrWhiteSpace(output);
            }
            catch
            {
                return false;
            }
        }

        private async Task EnsurePythonEnvironment()
        {
            string pthFile = Path.Combine(_pythonDirectory, "python311._pth");

            if (File.Exists(pthFile))
            {
                string[] lines = await File.ReadAllLinesAsync(pthFile);
                bool modified = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim() == "#import site")
                    {
                        lines[i] = "import site";
                        modified = true;
                        break;
                    }
                }
                if (modified)
                {
                    await File.WriteAllLinesAsync(pthFile, lines);
                    Debug.WriteLine("Modified python311._pth file to enable site imports");
                }
            }
            else
            {
                Debug.WriteLine($"{pthFile} not found");
            }

            string pythonDll = Path.Combine(_pythonDirectory, "python311.dll");
            if (!File.Exists(pythonDll))
            {
                throw new Exception($"Python DLL not found at {pythonDll}. Please ensure Python 3.11 is correctly installed.");
            }
        }
    }
}
