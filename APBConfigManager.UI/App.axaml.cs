using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using APBConfigManager.UI.Views;
using System.Linq;
using System.IO;
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
            var desktopLifetime = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            desktopLifetime!.Startup += (sender, args) =>
            {
                // Will run a specific profile without initializing the GUI
                if (args.Args.Contains("--run"))
                {
                    desktop.MainWindow = new MainWindow(args.Args[1]);
                }
                // Replaces the symlinked config directory with a normal one
                // while preserving its content. Invoked by the uninstaller.
                else if (args.Args.Contains("--unlink"))
                {
                    if (FileUtils.IsSymlink(AppConfig.AbsGameConfigDir))
                    {
                        string tempPath = AppConfig.AbsGameConfigDir + "_Temp";
                        Directory.Move(AppConfig.AbsGameConfigDir, tempPath);

                        Directory.CreateDirectory(AppConfig.AbsGameConfigDir);
                        CopyDirectory(tempPath, AppConfig.AbsGameConfigDir, true);

                        Directory.Delete(tempPath);
                    }

                    Environment.Exit(0);
                }
                else
                {
                    // Normal startup
                    desktop.MainWindow = new MainWindow(string.Empty);
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        DirectoryInfo[] dirs = dir.GetDirectories();

        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}