namespace MultiAudioOutput;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Check for silent startup (from autostart)
        bool startMinimized = args.Contains("--minimized") || args.Contains("-m");

        ApplicationConfiguration.Initialize();

        // Prevent multiple instances - a second copy would create a duplicate
        // tray icon and compete for the same audio devices
        using var instanceMutex = new Mutex(true, @"Local\MultiAudioOutput_SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show("Multi Audio Output is already running. Check the system tray.",
                "Multi Audio Output", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.Run(new MainForm(startMinimized));
    }
}
