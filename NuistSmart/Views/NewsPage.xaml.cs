using Microsoft.UI.Xaml.Controls;
using NuistSmart.Models;
using NuistSmart.ViewModels;

namespace NuistSmart.Views
{
    public sealed partial class NewsPage : Page
    {
        public NewsViewModel ViewModel { get; }

        public NewsPage()
        {
            this.InitializeComponent();
            ViewModel = new NewsViewModel();
        }

        // 【关键】这就是中转站方法，名字叫 OnItemClick
        private void OnItemClick(object sender, ItemClickEventArgs e)
        {
            // 1. 拿到被点击的新闻数据
            if (e.ClickedItem is NewsItem item)
            {
                // 2. 告诉 ViewModel 去打开浏览器
                ViewModel.OpenLinkCommand.Execute(item);
            }
        }
    }
}