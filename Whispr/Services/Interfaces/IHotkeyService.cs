using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whispr.Services
{
    public interface IHotkeyService
    {
        bool ChangeKey(int key);

        event EventHandler HotkeyTriggered;
        bool SimulateTextInput(string text);
    }
}
