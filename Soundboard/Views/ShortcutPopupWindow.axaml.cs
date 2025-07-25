using Avalonia.Controls;

namespace Soundboard.Views;

public partial class ShortcutPopupWindow : Window
{
    public ShortcutPopupViewModel? ViewModel => DataContext as ShortcutPopupViewModel;

    public ShortcutPopupWindow()
    {
        InitializeComponent();

        DataContext = new ShortcutPopupViewModel(this);
    }
}