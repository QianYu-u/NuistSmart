using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuistSmart.Models;
using NuistSmart.Services;
using System.Collections.Generic;
using System.Linq; // 【关键】引入 Linq 用于去重查找

namespace NuistSmart.ViewModels
{
    public partial class NewsViewModel : ObservableObject
    {
        private readonly NewsService _newsService;

        private string _nextPageUrl = string.Empty;

        [ObservableProperty]
        private ObservableCollection<NewsItem> newsList = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isLoadingMore;

        [ObservableProperty]
        private bool hasMoreData = false;

        public List<string> Categories { get; } = new()
        {
            "全部", "文件公告", "学术报告", "招标信息", "会议通知",
            "教学考试", "党政事务", "组织人事", "科研信息",
            "招生就业", "创新创业", "校园活动", "学院动态"
        };

        [ObservableProperty]
        private string selectedCategory = "全部";

        public NewsViewModel()
        {
            _newsService = new NewsService();
            _ = LoadNewsAsync();
        }

        async partial void OnSelectedCategoryChanged(string value)
        {
            await LoadNewsAsync();
        }

        [RelayCommand]
        private async Task LoadNewsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            NewsList.Clear();
            HasMoreData = false;

            try
            {
                var result = await _newsService.GetNewsListAsync(SelectedCategory, isNextPage: false);

                foreach (var item in result.Items)
                {
                    // 【去重逻辑】如果列表里没有这条 URL，才添加
                    // 这能完美解决置顶新闻在每一页都出现的问题
                    if (!NewsList.Any(n => n.Url == item.Url))
                    {
                        NewsList.Add(item);
                    }
                }

                _nextPageUrl = result.NextPageUrl;
                HasMoreData = !string.IsNullOrEmpty(_nextPageUrl);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadMoreNewsAsync()
        {
            if (IsLoadingMore || string.IsNullOrEmpty(_nextPageUrl)) return;

            IsLoadingMore = true;

            try
            {
                var result = await _newsService.GetNewsListAsync(_nextPageUrl, isNextPage: true);

                foreach (var item in result.Items)
                {
                    // 【去重逻辑】同样的判断，防止翻页时遇到重复的置顶内容
                    if (!NewsList.Any(n => n.Url == item.Url))
                    {
                        NewsList.Add(item);
                    }
                }

                _nextPageUrl = result.NextPageUrl;
                HasMoreData = !string.IsNullOrEmpty(_nextPageUrl);
            }
            finally
            {
                IsLoadingMore = false;
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