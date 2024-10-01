using System;

namespace Whispr.Services
{
    public interface IHotkeyService
    {
        bool ChangeKey(int key);

        event EventHandler HotkeyTriggered;
        event EventHandler HotkeyReleased;
        bool SimulateTextInput(string text);
    }
}
