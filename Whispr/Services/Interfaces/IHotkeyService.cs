using System;
using Avalonia.Controls;

namespace Whispr.Services.Interfaces
{
    public interface IHotkeyService : IDisposable
    {
        void Initialize(Window window, Action hotkeyAction);
        void ChangeKey(int key);
    }
}