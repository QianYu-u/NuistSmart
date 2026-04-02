using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuistSmart.Models;
using NuistSmart.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NuistSmart.ViewModels
{
    public class LibrarySearchViewModel : ObservableObject
    {
        private readonly LibraryService _libraryService;
        private string _searchKeyword = string.Empty;
        private bool _isLoading;
        private bool _showNoResults;

        public LibrarySearchViewModel(LibraryService libraryService)
        {
            _libraryService = libraryService;
            SearchCommand = new AsyncRelayCommand(SearchAsync);
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public ObservableCollection<BookItem> SearchResults { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool ShowNoResults
        {
            get => _showNoResults;
            set => SetProperty(ref _showNoResults, value);
        }

        public IAsyncRelayCommand SearchCommand { get; }

        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                SearchResults.Clear();
                ShowNoResults = false;
                return;
            }

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
