using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using APBConfigManager.UI.Views;

namespace APBConfigManager.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var desktopLifetime = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            desktopLifetime!.Startup += (sender, args) =>
            {

                if (args.Args.Length > 0)
                {
                    desktop.MainWindow = new MainWindow(args.Args[0]);
                }
                else
                {
                    desktop.MainWindow = new MainWindow(string.Empty);
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}