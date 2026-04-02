using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NuistSmart.Models;
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
            this.Loaded += LibrarySearchPage_Loaded;
        }

        /// <summary>
        /// 页面加载后自动获取新书列表
        /// </summary>
        private async void LibrarySearchPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 只在新书列表为空时加载（避免返回时重复请求）
            if (ViewModel.Books.Count == 0)
            {
                await ViewModel.LoadNewBooksCommand.ExecuteAsync(null);
            }
        }

        /// <summary>
        /// 新书卡片点击 → 导航到 WebView2 详情页
        /// </summary>
        private void NewBooksGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is BookItem book && !string.IsNullOrEmpty(book.DetailUrl))
            {
                this.Frame.Navigate(typeof(BookDetailPage), book.DetailUrl);
            }
        }
    }
}
