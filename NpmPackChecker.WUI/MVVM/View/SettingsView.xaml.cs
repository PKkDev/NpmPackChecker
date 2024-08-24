using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using NpmPackChecker.WUI.MVVM.ViewModel;


namespace NpmPackChecker.WUI.MVVM.View
{
    public sealed partial class SettingsView : Page
    {
        public SettingsViewModel ViewModel { get; set; }

        public SettingsView()
        {
            InitializeComponent();
            DataContext = ViewModel = App.GetService<SettingsViewModel>();
        }

        //private void ToggleButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        //{
        //    if ((sender as ToggleButton).IsChecked == true)
        //        GitlabTokenBox.PasswordRevealMode = PasswordRevealMode.Visible;
        //    else
        //        GitlabTokenBox.PasswordRevealMode = PasswordRevealMode.Hidden;
        //}
    }
}
