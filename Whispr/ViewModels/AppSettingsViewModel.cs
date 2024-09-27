using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

namespace Whispr.ViewModels
{
    public class AppSettingsViewModel : ViewModelBase
    {
        private string _modelStatusText = string.Empty;
        private bool _isModelProgressVisible = false;
        private string _selectedShortcutKey = "Space";
        private string _selectedAIModel = "Model1";

        public string ModelStatusText
        {
            get => _modelStatusText;
            set => this.RaiseAndSetIfChanged(ref _modelStatusText, value);
        }

        public bool IsModelProgressVisible
        {
            get => _isModelProgressVisible;
            set => this.RaiseAndSetIfChanged(ref _isModelProgressVisible, value);
        }

        public string SelectedShortcutKey
        {
            get => _selectedShortcutKey;
            set => this.RaiseAndSetIfChanged(ref _selectedShortcutKey, value);
        }

        public string SelectedAIModel
        {
            get => _selectedAIModel;
            set => this.RaiseAndSetIfChanged(ref _selectedAIModel, value);
        }

        public ObservableCollection<string> ShortcutKeys { get; set; }
        public ObservableCollection<string> AIModels { get; set; }

        public ReactiveCommand<Unit, Unit> DownloadModelCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadModelCommand { get; }

        public AppSettingsViewModel()
        {
            ShortcutKeys = ["Space", "Enter", "Tab"];
            AIModels = ["Model1", "Model2", "Model3"];

            DownloadModelCommand = ReactiveCommand.CreateFromTask(DownloadModel);
            LoadModelCommand = ReactiveCommand.CreateFromTask(LoadModel);
        }

        private async Task DownloadModel()
        {
            IsModelProgressVisible = true;
            ModelStatusText = "Downloading model...";
            // Simulate download
            await Task.Delay(2000);
            ModelStatusText = "Model downloaded.";
            IsModelProgressVisible = false;
        }

        private async Task LoadModel()
        {
            IsModelProgressVisible = true;
            ModelStatusText = "Loading model...";
            // Simulate loading
            await Task.Delay(2000);
            ModelStatusText = "Model loaded.";
            IsModelProgressVisible = false;
        }
    }
}