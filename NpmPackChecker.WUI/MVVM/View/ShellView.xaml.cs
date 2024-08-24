using Microsoft.UI.Xaml.Controls;
using NpmPackChecker.WUI.MVVM.ViewModel;
using NpmPackChecker.WUI.Services;

namespace NpmPackChecker.WUI.MVVM.View;

public sealed partial class ShellView : Page
{
    public ShellViewModel ViewModel { get; set; }

    public ShellView()
    {
        InitializeComponent();
        DataContext = ViewModel = App.GetService<ShellViewModel>();

        ViewModel.NavigationHelperService.Initialize(NavView, ContentFrame); 
        NavView.SelectedItem = NavView.MenuItems[0];
        ViewModel.NavigationHelperService.Navigate("NpmPackDep");

        var service = App.GetService<InfoBarService>();
        service.Initialization(PageInfoBar);
    }

    private void NavView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args) { }
}
