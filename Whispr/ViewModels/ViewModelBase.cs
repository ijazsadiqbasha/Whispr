using ReactiveUI;
using Whispr.Models;

namespace Whispr.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        protected static AppSettings Settings { get; } = AppSettings.Load();

        protected void SaveSettings()
        {
            Settings.Save();
        }
    }
}
