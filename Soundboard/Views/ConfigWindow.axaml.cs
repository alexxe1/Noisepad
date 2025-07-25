using Avalonia.Controls;

namespace Soundboard.Views;

public partial class ConfigWindow : Window
{
    public static ConfigWindow Instance { get; private set; } = null!;

    public ConfigWindow()
    {
        InitializeComponent();

        Instance = this;
    }
}