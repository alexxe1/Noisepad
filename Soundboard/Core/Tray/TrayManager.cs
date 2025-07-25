using Avalonia.Controls;
using Soundboard;
using Soundboard.Core.Config;

public class TrayManager
{
    private TrayIcon? _trayIcon;
    private Window _mainWindow;

    public TrayManager(Window mainWindow)
    {
        _mainWindow = mainWindow;

        var menu = new NativeMenu();

        var showItem = new NativeMenuItem("Show");
        showItem.Click += (_, _) => ShowWindow();

        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApp();

        menu.Items.Add(showItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon("Assets/Icons/noisepad-icon.ico"),
            ToolTipText = ConfigManager.AppName,
            Menu = menu,
            IsVisible = true
        };
    }

    public void HideToTray()
    {
        _mainWindow.Hide();
    }

    public void ShowWindow()
    {
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    public void ExitApp()
    {
        Dispose();

        App.ManualExit();
        App.ShutdownApp();
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
    }
}
