using APBConfigManager.UI.ViewModels;
using Avalonia;
using Avalonia.Controls;
using System;

namespace APBConfigManager.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(string launchWithProfileId)
    {
        InitializeComponent();
        InitializeModel(launchWithProfileId);
    }

    public MainWindow()
    {
        InitializeComponent();
        InitializeModel(string.Empty);
    }

    private void InitializeModel(string launchWithProfileId)
    {
        if (!ProfileManager.IsValidGamePath(AppConfig.GamePath))
        {
            string gamePath;
            if (!LocateGamePath(out gamePath))
            {
                new MessageBoxFactory()
                    .Icon(MessageBoxIconType.Error)
                    .Button("OK", true, false)
                    .Title("ERROR")
                    .Message("The selected directory is not a valid APB game install!")
                    .Show(this);

                Close();
                Environment.Exit(0);
            }

            AppConfig.GamePath = gamePath;
        }

        ProfileManager profileManager = new ProfileManager();

        if (launchWithProfileId != string.Empty)
        {
            try
            {
                Guid profileId = Guid.Parse(launchWithProfileId);
                profileManager.RunGameWithProfile(profileId);
                Environment.Exit(0);
            }
            catch (Exception)
            {
                throw new ArgumentException("Invalid profile GUID in command-line argument!");
            }
        }

        DataContext = new MainViewModel(this, profileManager);
    }

    private bool LocateGamePath(out string gamePath)
    {
        OpenFolderDialog dialog = new OpenFolderDialog();
        dialog.Title = "Locate your APB install...";

        string? result = dialog.ShowAsync(this).Result;

        if (result == null || !ProfileManager.IsValidGamePath(result))
        {
            gamePath = string.Empty;
            return false;
        }

        gamePath = result;
        return true;
    }
}