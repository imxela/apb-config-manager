using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using APBConfigManager.UI.ViewModels;
using APBConfigManager.UI.Views;
using System;

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
            desktop.Startup += (sender, args) =>
            {
                if (args.Args.Length > 0)
                {
                    try
                    {
                        Guid profileId = Guid.Parse(args.Args[0]);
                        ProfileManager.Instance.RunGameWithProfile(profileId);
                        Environment.Exit(0);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Invalid profile ID in command-line argument position 1!");
                    }
                }
            };

            desktop.MainWindow = new MainWindow();
            desktop.MainWindow.DataContext = new MainViewModel(desktop.MainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }
}