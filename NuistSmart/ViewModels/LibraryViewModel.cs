using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuistSmart.Models;
using NuistSmart.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NuistSmart.ViewModels
{
    public partial class LibraryViewModel : ObservableObject
    {
        private readonly LibraryService _libraryService;

        public LibraryViewModel(LibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        [ObservableProperty]
        private ObservableCollection<BookItem> searchResults = new();

        [ObservableProperty]
        private string searchKeyword = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool showNoResults;

        [RelayCommand]
        private async Task SearchBooksAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
                return;

            IsLoading = true;
            ShowNoResults = false;
            SearchResults.Clear();

            try
            {
                var books = await _libraryService.SearchBooksAsync(SearchKeyword);
                
                foreach (var book in books)
                {
                    SearchResults.Add(book);
                }

                ShowNoResults = SearchResults.Count == 0;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
