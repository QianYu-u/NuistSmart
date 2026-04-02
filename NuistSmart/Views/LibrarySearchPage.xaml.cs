using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using NuistSmart.ViewModels;

namespace NuistSmart.Views
{
    public sealed partial class LibrarySearchPage : Page
    {
        public LibrarySearchViewModel ViewModel { get; }

        public LibrarySearchPage()
        {
            ViewModel = App.ServiceProvider.GetRequiredService<LibrarySearchViewModel>();
            this.InitializeComponent();
        }
    }
}
