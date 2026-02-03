using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection; // 【关键】必须引用这个，才能用 GetRequiredService
using NuistSmart.Models;
using NuistSmart.ViewModels;

namespace NuistSmart.Views
{
    public sealed partial class NewsPage : Page
    {
        // 这里的 ViewModel 属性会被 XAML 中的 {x:Bind ViewModel...} 自动引用
        public NewsViewModel ViewModel { get; }

        public NewsPage()
        {
            this.InitializeComponent();

            // 【关键修复】
            // 不要直接 new NewsViewModel()，因为它的构造函数现在需要 DbService 参数。
            // 我们找 App 的“管家”（ServiceProvider）要一个实例，它会自动处理参数注入。
            ViewModel = App.ServiceProvider.GetRequiredService<NewsViewModel>();
        }

        // 列表点击事件中转
        private void OnItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is NewsItem item)
            {
                // 调用 ViewModel 的 OpenLinkCommand
                // 此时 ViewModel 会先存数据库，再打开浏览器
                ViewModel.OpenLinkCommand.Execute(item);
            }
        }
    }
}