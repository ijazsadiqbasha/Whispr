using ReactiveUI;
using Whispr.Models;

namespace Whispr.ViewModels
{
    public class ViewModelBase(AppSettings settings) : ReactiveObject
    {
        protected readonly AppSettings Settings = settings;

        protected void SaveSettings()
        {
            Settings.Save();
        }
    }
}
