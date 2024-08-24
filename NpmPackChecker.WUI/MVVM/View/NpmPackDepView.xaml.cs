using Microsoft.UI.Xaml.Controls;
using NpmPackChecker.WUI.MVVM.ViewModel;
using System.Linq;

namespace NpmPackChecker.WUI.MVVM.View
{

    public sealed partial class NpmPackDepView : Page
    {
        public NpmPackDepViewModel ViewModel { get; set; }

        public NpmPackDepView()
        {
            InitializeComponent();
            DataContext = ViewModel = App.GetService<NpmPackDepViewModel>();
        }

        private void DepsSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var text = sender.Text.ToLower();

                if (string.IsNullOrEmpty(text))
                {
                    ViewModel.FilterTree("");
                }

                var res = ViewModel.DepNodeView.TotalDeps.Where(x => x.ToLower().Contains(text));
                sender.ItemsSource = res;
            }
        }

        private void DepsSearchSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var user = args.SelectedItem.ToString();
            ViewModel.FilterTree(user);
        }
    }
}
