using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Whispr.Services;
using Whispr.ViewModels;
using Whispr.Views;

namespace Whispr
{
    internal class Program
    {
        public static ServiceProvider? Services { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            Services = ConfigureServices();
            
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Add configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // Register PythonInstallationService as a singleton
            services.AddSingleton<IPythonInstallationService, PythonInstallationService>();

            return services.BuildServiceProvider();
        }
    }
}