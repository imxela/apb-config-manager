using APBConfigManager.UI.ViewModels;
using Avalonia.Controls;

namespace APBConfigManager.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(this);
    }
}