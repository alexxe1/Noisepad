using Avalonia.Controls;
using Soundboard.Core.Config;
using System;

namespace Soundboard.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; } = null!;
    private TrayManager? _tray;

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;

        AppFocusTracker.AttachToWindow(this);
    }

    protected override void OnOpened(EventArgs e)
    {
        if (_tray != null)
        {
            _tray.Dispose();
            _tray = null;
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (ConfigManager.ConfigData.ExitToTray)
        {
            e.Cancel = true;

            if (_tray == null)
                _tray = new TrayManager(this);

            _tray.HideToTray();
            return;
        }

        App.ManualExit();

        base.OnClosing(e);
    }
}
