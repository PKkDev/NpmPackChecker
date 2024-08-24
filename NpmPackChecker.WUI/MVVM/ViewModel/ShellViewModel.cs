using NpmPackChecker.WUI.Services;

namespace NpmPackChecker.WUI.MVVM.ViewModel
{
    public class ShellViewModel
    {
        public NavigationHelperService NavigationHelperService { get; init; }

        public ShellViewModel(NavigationHelperService navigationHelperService)
        {
            NavigationHelperService = navigationHelperService;
        }
    }
}
