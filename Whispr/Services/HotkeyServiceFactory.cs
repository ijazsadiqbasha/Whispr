using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Whispr.Services.Interfaces;

namespace Whispr.Services
{
    public static class HotkeyServiceFactory
    {
        public static IHotkeyService Create(IConfiguration configuration)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsHotkeyService(configuration);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxHotkeyService(configuration);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new MacOSHotkeyService(configuration);
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported operating system");
            }
        }
    }
}