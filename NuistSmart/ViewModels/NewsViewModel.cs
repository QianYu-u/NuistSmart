using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuistSmart.Models;
using NuistSmart.Services;
using System.Collections.Generic;
using System.Linq;

namespace NuistSmart.ViewModels
{
    public partial class NewsViewModel : ObservableObject
    {
        private readonly NewsService _newsService;
        private readonly DbService _dbService;

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
            "教学考试", "电子事务", "组织人事", "科研信息",
            "招生就业", "创新创业", "校园活动", "学院动态"
        };

        [ObservableProperty]
        private string selectedCategory = "全部";

        // 注意：将 AiService 移除了
        public NewsViewModel(DbService dbService)
        {
            _dbService = dbService;
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
                    if (!NewsList.Any(n => n.Url == item.Url))
                    {
                        NewsList.Add(item);
                    }
                }

                SaveListToDatabase(result.Items);

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
                    if (!NewsList.Any(n => n.Url == item.Url))
                    {
                        NewsList.Add(item);
                    }
                }

                SaveListToDatabase(result.Items);

                _nextPageUrl = result.NextPageUrl;
                HasMoreData = !string.IsNullOrEmpty(_nextPageUrl);
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        /// <summary>
        /// 将列表基础数据存入 LiteDB
        /// </summary>
        private void SaveListToDatabase(List<NewsItem> items)
        {
            Task.Run(() =>
            {
                try
                {
                    int count = 0;
                    foreach (var item in items)
                    {
                        var existing = _dbService.GetNews(item.Url);

                        if (existing == null)
                        {
                            var cache = new NewsDetailCache
                            {
                                Url = item.Url,
                                Title = item.Title,
                                Date = item.Date,
                                Content = "",
                                CreateTime = DateTime.Now
                            };
                            _dbService.SaveNews(cache);
                            count++;
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[LiteDB] 自动归档完成，新存入 {count} 条新目录。");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LiteDB] 存储失败: {ex.Message}");
                }
            });
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