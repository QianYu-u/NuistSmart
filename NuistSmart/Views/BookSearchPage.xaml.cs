using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using NuistSmart.ViewModels;

namespace NuistSmart.Views
{
    public sealed partial class BookSearchPage : Page
    {
        public LibraryViewModel ViewModel { get; }

        public BookSearchPage()
        {
            ViewModel = App.ServiceProvider.GetRequiredService<LibraryViewModel>();
            this.InitializeComponent();
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (ViewModel.SearchBooksCommand.CanExecute(null))
            {
                ViewModel.SearchBooksCommand.Execute(null);
            }
        }
    }
}
