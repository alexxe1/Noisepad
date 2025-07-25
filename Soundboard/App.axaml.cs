using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using Soundboard.ViewModels;
using Soundboard.Views;
using System;

namespace Soundboard;

public partial class App : Application
{
    public static event Action OnAppClosing = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            desktop.Exit += OnExit;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        OnAppClosing?.Invoke();
    }

    public static void ManualExit()
    {
        OnAppClosing?.Invoke();
    }

    public static void ShutdownApp()
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}

public static class AppFocusTracker
{
    public static bool IsAppFocused { get; private set; } = true;

    public static void AttachToWindow(Window window)
    {
        window.GotFocus += (_, _) => IsAppFocused = true;
        window.LostFocus += (_, _) => IsAppFocused = false;
        window.Deactivated += (_, _) => IsAppFocused = false;
        window.Activated += (_, _) => IsAppFocused = true;
    }
}