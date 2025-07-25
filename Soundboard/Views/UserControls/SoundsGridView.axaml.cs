using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Linq;

namespace Soundboard.Views.UserControls;

public partial class SoundsGridView : UserControl
{
    public SoundsGridView()
    {
        InitializeComponent();

        SoundsList.SelectionChanged += SoundsList_SelectionChanged;
    }

    private void SoundsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SoundsList.SelectedItem == null)
            return;

        SoundsList.ScrollIntoView(SoundsList.SelectedItem, null);

        var row = SoundsList.GetVisualDescendants()
                    .OfType<DataGridRow>()
                    .FirstOrDefault(r => r.DataContext == SoundsList.SelectedItem);

        if (row != null)
        {
            row.Focus();
        }
    }
}