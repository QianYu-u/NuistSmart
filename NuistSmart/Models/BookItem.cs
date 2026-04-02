using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

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

        [NotifyPropertyChangedFor(nameof(CoverImage))]
        [ObservableProperty]
        private string coverUrl = string.Empty;

        public ImageSource? CoverImage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CoverUrl))
                {
                    return null;
                }

                try
                {
                    return new BitmapImage(new Uri(CoverUrl));
                }
                catch
                {
                    return null;
                }
            }
        }

        [ObservableProperty]
        private string detailUrl = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;
    }
}
