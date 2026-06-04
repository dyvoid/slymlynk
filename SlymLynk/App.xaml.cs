using System.Threading;
using System.Windows;
using SlymLynk.Views;

namespace SlymLynk;

public partial class App : Application
{
    private Mutex? _mutex;

    private void App_Startup(object sender, StartupEventArgs e)
    {
        _mutex = new Mutex(initiallyOwned: true, "SlymLynk_SingleInstance", out bool createdNew);

        if (!createdNew)
        {
            // Another instance is running — bring it to foreground and exit.
            _mutex.Dispose();
            _mutex = null;
            Shutdown();
            return;
        }

        var window = new MainWindow();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
