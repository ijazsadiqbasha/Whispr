using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Whispr.Services.Interfaces;

namespace Whispr.Services
{
    public unsafe partial class WindowsHotkeyService(IConfiguration configuration) : IHotkeyService, IDisposable
    {
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int HOTKEY_ID = 9000;
        private const int WM_HOTKEY = 0x0312;

        private readonly IConfiguration _configuration = configuration;
        private IntPtr _windowHandle;
        private Action? _hotkeyAction;
        private WndProcDelegate? _wndProcDelegate;
        private IntPtr _oldWndProc;
        private bool _isRegistered = false;
        private int _key;

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrA", StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongA", StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
        }

        [LibraryImport("user32.dll", EntryPoint = "CallWindowProcW")]
        private static partial IntPtr CallWindowProc64(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [LibraryImport("user32.dll", EntryPoint = "CallWindowProcA")]
        private static partial IntPtr CallWindowProc32(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private static IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam)
        {
            if (IntPtr.Size == 8)
                return CallWindowProc64(lpPrevWndFunc, hWnd, Msg, wParam, lParam);
            else
                return CallWindowProc32(lpPrevWndFunc, hWnd, Msg, wParam, lParam);
        }

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public void Initialize(Window window, Action hotkeyAction)
        {
            _windowHandle = GetWindowHandle(window);
            _hotkeyAction = hotkeyAction;
            _wndProcDelegate = WndProc;
            _oldWndProc = SetWindowLongPtr(_windowHandle, -4, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));

            int defaultKey = _configuration.GetValue<int>("Hotkey:DefaultKey", 32);
            ChangeKey(defaultKey);
            
            Debug.WriteLine("Windows hotkey service initialized");
        }

        public void ChangeKey(int key)
        {
            Debug.WriteLine($"Changing hotkey to: 0x{key:X}");
            if (_key != key)
            {
                _key = key;
                try
                {
                    RegisterHotKey();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to register hotkey: {ex.Message}");
                    // Optionally, you can raise an event or use a callback to inform the ViewModel about the failure
                }
            }
            else
            {
                Debug.WriteLine("Key unchanged, skipping registration");
            }
        }

        private static IntPtr GetWindowHandle(Window window)
        {
            var handle = window.TryGetPlatformHandle();
            return handle?.Handle ?? IntPtr.Zero;
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                int modifiers = (int)((ulong)lParam & 0xFFFF);
                int vk = (int)(((ulong)lParam >> 16) & 0xFFFF);
                
                Debug.WriteLine($"Hotkey event received: ID={id}, Modifiers={modifiers}, VK={vk}");
                
                if (id == HOTKEY_ID && IsTextCursorActive())
                {
                    Debug.WriteLine($"Registered hotkey pressed with active text cursor: CTRL+SHIFT+0x{_key:X}");
                    Dispatcher.UIThread.Post(_hotkeyAction!);
                    return IntPtr.Zero;
                }
            }
            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private void RegisterHotKey()
        {
            if (_isRegistered)
            {
                Debug.WriteLine($"Unregistering previous hotkey: CTRL+SHIFT+0x{_key:X}");
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
                _isRegistered = false;
            }

            Debug.WriteLine($"Attempting to register new hotkey: CTRL+SHIFT+0x{_key:X}");
            if (RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, _key))
            {
                _isRegistered = true;
                Debug.WriteLine($"Hotkey registered successfully: CTRL+SHIFT+0x{_key:X}");
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                Debug.WriteLine($"Failed to register hotkey (key: 0x{_key:X}). Error code: {error}");
                throw new Exception($"Failed to register hotkey. Error code: {error}. This might be because the hotkey is already in use by another application.");
            }
        }

        [LibraryImport("user32.dll")]
        private static partial IntPtr GetForegroundWindow();

        [LibraryImport("user32.dll")]
        private static partial uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetCaretPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private static bool IsTextCursorActive()
        {
            var foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
                return false;

            uint threadId = GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);

            var guiInfo = new GUITHREADINFO();
            guiInfo.cbSize = Marshal.SizeOf(guiInfo);

            if (!GetGUIThreadInfo(threadId, ref guiInfo))
                return false;

            // Check if there's a caret (text cursor)
            if (guiInfo.hwndCaret != IntPtr.Zero)
                return true;

            // If no caret, check if there's a selection range (which also indicates text input is possible)
            if (guiInfo.rcCaret.Left != guiInfo.rcCaret.Right || guiInfo.rcCaret.Top != guiInfo.rcCaret.Bottom)
                return true;

            // As a final check, see if we can get the caret position
            return GetCaretPos(out _);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_isRegistered)
                {
                    UnregisterHotKey(_windowHandle, HOTKEY_ID);
                    _isRegistered = false;
                }
                if (_oldWndProc != IntPtr.Zero)
                {
                    SetWindowLongPtr(_windowHandle, -4, _oldWndProc);
                    _oldWndProc = IntPtr.Zero;
                }
                Debug.WriteLine("WindowsHotkeyService disposed");
            }
        }
    }
}