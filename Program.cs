namespace MultiAudioOutput;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Check for silent startup (from autostart)
        bool startMinimized = args.Contains("--minimized") || args.Contains("-m");
        
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(startMinimized));
    }
}
