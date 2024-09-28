using Whispr.Models;
using SharpHook;
using SharpHook.Native;
using System;

namespace Whispr.Services
{
    public class HotkeyService : IHotkeyService, IDisposable
    {
        private readonly AppSettings _appSettings;
        private readonly TaskPoolGlobalHook _hook;
        private bool _ctrlPressed = false;
        private bool _shiftPressed = false;

        public event EventHandler? HotkeyTriggered;

        public HotkeyService(AppSettings appSettings)
        {
            _appSettings = appSettings;
            _hook = new TaskPoolGlobalHook();
            _hook.KeyPressed += OnKeyPressed;
            _hook.KeyReleased += OnKeyReleased;
            _hook.RunAsync();
        }

        private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            switch (e.Data.KeyCode)
            {
                case KeyCode.VcLeftControl:
                case KeyCode.VcRightControl:
                    _ctrlPressed = true;
                    break;
                case KeyCode.VcLeftShift:
                case KeyCode.VcRightShift:
                    _shiftPressed = true;
                    break;
                default:
                    if ((int)e.Data.KeyCode == _appSettings.Hotkey && _ctrlPressed && _shiftPressed)
                    {
                        HotkeyTriggered?.Invoke(this, EventArgs.Empty);
                    }
                    break;
            }
        }

        private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
        {
            switch (e.Data.KeyCode)
            {
                case KeyCode.VcLeftControl:
                case KeyCode.VcRightControl:
                    _ctrlPressed = false;
                    break;
                case KeyCode.VcLeftShift:
                case KeyCode.VcRightShift:
                    _shiftPressed = false;
                    break;
            }
        }

        public bool ChangeKey(int key)
        {
            if (Enum.IsDefined(typeof(KeyCode), (ushort)key))
            {
                _appSettings.Hotkey = key;
                _appSettings.Save();
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            _hook.Dispose();
        }
    }
}