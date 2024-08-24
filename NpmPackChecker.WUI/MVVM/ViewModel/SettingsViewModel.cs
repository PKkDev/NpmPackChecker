using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using NpmPackChecker.WUI.MVVM.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace NpmPackChecker.WUI.MVVM.ViewModel;

public class SettingsViewModel : ObservableRecipient
{
    //private readonly DataStorageService _dataStorage;

    public ObservableCollection<UITheme> Themes { get; set; }
    private UITheme _selectedTheme;
    public UITheme SelectedTheme
    {
        get { return _selectedTheme; }
        set
        {
            SetTheme(value);
            SetProperty(ref _selectedTheme, value);
        }
    }

    //private BaseAppSettings _baseAppSettings;
    //public BaseAppSettings BaseAppSettings
    //{
    //    get => _baseAppSettings;
    //    set => SetProperty(ref _baseAppSettings, value);
    //}

    public RelayCommand OpenLocalStorage { get; set; }
    public RelayCommand OpenTokenSettingsWeb { get; set; }
    public RelayCommand SaveBaseAppSettings { get; set; }

    public SettingsViewModel()
    {
        //_dataStorage = App.GetService<DataStorageService>();

        //BaseAppSettings = _dataStorage.GetByKey<BaseAppSettings>("BaseAppSettings");

        Themes = new()
        {
            new UITheme("Default", ElementTheme.Default),
            new UITheme("Dark", ElementTheme.Dark),
            new UITheme("Light", ElementTheme.Light),
        };

        if (App.MainWindow.Content is FrameworkElement rootElement2)
        {
            var theme = rootElement2.RequestedTheme;
            var search = Themes.FirstOrDefault(x => x.Theme == theme);
            if (search != null)
                SelectedTheme = search;
        }

        OpenLocalStorage = new RelayCommand(async () =>
        {
            //await _dataStorage.OpenFolder();
        });

        OpenTokenSettingsWeb = new RelayCommand(async () =>
        {
            var url = "https://gitlabsvr.nsd.ru/gitlab/-/profile/personal_access_tokens";
            await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
        });

        SaveBaseAppSettings = new RelayCommand(async () =>
        {
            //await _dataStorage.SetByKey(BaseAppSettings, "BaseAppSettings");
        });
    }

    public void SetTheme(UITheme theme)
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            if (rootElement.RequestedTheme == theme.Theme)
                return;

            rootElement.RequestedTheme = theme.Theme;
        }
    }
}
