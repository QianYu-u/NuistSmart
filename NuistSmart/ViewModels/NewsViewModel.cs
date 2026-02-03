using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuistSmart.Models;
using NuistSmart.Services;
using System.Collections.Generic;

namespace NuistSmart.ViewModels
{
    public partial class NewsViewModel : ObservableObject
    {
        private readonly NewsService _newsService;

        [ObservableProperty]
        private ObservableCollection<NewsItem> newsList = new();

        [ObservableProperty]
        private bool isLoading;

        // 1. 【新增】分类列表数据源
        public List<string> Categories { get; } = new()
        {
            "全部", "文件公告", "学术报告", "招标信息", "会议通知",
            "教学考试", "党政事务", "组织人事", "科研信息",
            "招生就业", "创新创业", "校园活动", "学院动态"
        };

        // 2. 【新增】当前选中的分类
        [ObservableProperty]
        private string selectedCategory = "全部";

        public NewsViewModel()
        {
            _newsService = new NewsService();
            // 启动时加载默认分类
            _ = LoadNewsAsync();
        }

        // 3. 【新增】监听分类变化，自动刷新
        // 当 SelectedCategory 属性改变时，这个方法会自动被调用
        async partial void OnSelectedCategoryChanged(string value)
        {
            await LoadNewsAsync();
        }

        // 4. 【修改】加载新闻，支持刷新
        [RelayCommand]
        private async Task LoadNewsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            NewsList.Clear(); // 刷新前先清空，给用户一种正在刷新的感觉

            try
            {
                // 传入当前选中的分类
                var news = await _newsService.GetNewsListAsync(SelectedCategory);

                foreach (var item in news)
                {
                    NewsList.Add(item);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task OpenLinkAsync(NewsItem item)
        {
            if (item != null && !string.IsNullOrEmpty(item.Url))
            {
                await Launcher.LaunchUriAsync(new Uri(item.Url));
            }
        }
    }
}