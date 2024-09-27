using System;

namespace Whispr.Services
{
    public interface IHotkeyService
    {
        void RegisterHotkey(string hotkeyName, Action action);
        void UnregisterHotkey(string hotkeyName);
        bool IsHotkeyRegistered(string hotkeyName);
    }
}