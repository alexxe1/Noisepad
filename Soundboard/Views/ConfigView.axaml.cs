using Avalonia.Controls;
using Soundboard.ViewModels;

namespace Soundboard.Views;

public partial class ConfigView : UserControl
{
    public ConfigView()
    {
        InitializeComponent();

        DataContext = new ConfigViewModel();
    }
}