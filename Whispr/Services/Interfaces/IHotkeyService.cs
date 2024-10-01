using System;

namespace Whispr.Services
{
    public interface IHotkeyService
    {
        bool ChangeKey(int key);

        event EventHandler HotkeyTriggered;
        bool SimulateTextInput(string text);
    }
}
