using CommunityToolkit.Mvvm.ComponentModel;

namespace NuistSmart.Models
{
    public partial class BookItem : ObservableObject
    {
        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string author = string.Empty;

        [ObservableProperty]
        private string publisher = string.Empty;

        [ObservableProperty]
        private string callNumber = string.Empty;

        [ObservableProperty]
        private string availableStatus = string.Empty;

        [ObservableProperty]
        private string coverUrl = string.Empty;
    }
}
