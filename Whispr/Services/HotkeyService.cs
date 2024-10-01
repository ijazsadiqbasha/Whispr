using Whispr.Models;
using SharpHook;
using SharpHook.Native;
using System;
using System.Diagnostics;
using TextCopy;
using System.Threading.Tasks;

namespace Whispr.Services
{
    public class HotkeyService : IHotkeyService, IDisposable
    {
        private readonly AppSettings _appSettings;
        private readonly EventSimulator _eventSimulator;
        private readonly TaskPoolGlobalHook _hook;
        private bool _ctrlPressed = false;
        private bool _shiftPressed = false;
        private bool _isHotkeyPressed = false;

        public event EventHandler? HotkeyTriggered;
        public event EventHandler? HotkeyReleased;

        public HotkeyService(AppSettings appSettings)
        {
            _appSettings = appSettings;
            _eventSimulator = new EventSimulator();
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
                    if ((int)e.Data.KeyCode == _appSettings.Hotkey && _ctrlPressed && _shiftPressed && !_isHotkeyPressed)
                    {
                        _isHotkeyPressed = true;
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
                default:
                    if ((int)e.Data.KeyCode == _appSettings.Hotkey)
                    {
                        _isHotkeyPressed = false;
                        HotkeyReleased?.Invoke(this, EventArgs.Empty);
                    }
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

        public async Task<bool> SimulateTextInput(string text)
        {
            try
            {
                await ClipboardService.SetTextAsync(text);
                _eventSimulator.SimulateKeyPress(KeyCode.VcLeftControl);
                _eventSimulator.SimulateKeyPress(KeyCode.VcV);
                _eventSimulator.SimulateKeyRelease(KeyCode.VcV);
                _eventSimulator.SimulateKeyRelease(KeyCode.VcLeftControl);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with pasting text: '{ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _hook.Dispose();
        }
    }
}