public partial class MainWindow : Window
{
    private readonly WindowsHotkeyService _hotkeyService;
    private readonly AppSettings _appSettings;

    public MainWindow()
    {
        InitializeComponent();
        _appSettings = AppSettings.LoadOrCreate();
        _hotkeyService = new WindowsHotkeyService(_appSettings);

        // Defer hotkey initialization until the window is loaded
        this.Loaded += MainWindow_Loaded;
        this.Closed += MainWindow_Closed;
    }

    private void MainWindow_Loaded(object sender, EventArgs e)
    {
        try
        {
            _hotkeyService.Initialize(this, OnHotkeyPressed);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize hotkey service: {ex.Message}");
            // Show an error message to the user
            ShowError($"Failed to initialize hotkey service: {ex.Message}");
        }
    }

    private void OnHotkeyPressed()
    {
        // Your hotkey action here
        Debug.WriteLine("Hotkey pressed!");
    }

    private void MainWindow_Closed(object sender, EventArgs e)
    {
        _hotkeyService.Dispose();
    }

    private void ShowError(string message)
    {
        // Implement this method to show an error message to the user
        // For example, you could use a MessageBox or a custom dialog
    }
}