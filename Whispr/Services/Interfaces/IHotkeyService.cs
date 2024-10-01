using System;
using System.Threading.Tasks;

namespace Whispr.Services
{
    public interface IHotkeyService
    {
        bool ChangeKey(int key);

        event EventHandler HotkeyTriggered;
        event EventHandler HotkeyReleased;
        Task<bool> SimulateTextInput(string text);
    }
}
