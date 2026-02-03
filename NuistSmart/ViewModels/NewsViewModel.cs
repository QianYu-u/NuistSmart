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
        private readonly DbService _dbService; // 数据库服务

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
                    // 1. 更新 UI 列表
                    if (!NewsList.Any(n => n.Url == item.Url))
                    {
                        NewsList.Add(item);
                    }
                }

                // 【核心修改】拿到数据后，立刻存入数据库！
                // 这样不管点不点开，标题和日期都记住了。
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
                    // 1. 更新 UI
                    if (!NewsList.Any(n => n.Url == item.Url))
                    {
                        NewsList.Add(item);
                    }
                }

                // 【核心修改】加载更多时，也立刻存入数据库！
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
        /// 将列表数据批量存入 LiteDB
        /// </summary>
        private void SaveListToDatabase(List<NewsItem> items)
        {
            // 为了不卡顿 UI，我们用 Task.Run 在后台线程做存储
            Task.Run(() =>
            {
                try
                {
                    int count = 0;
                    foreach (var item in items)
                    {
                        // 1. 先查库，如果已经存在，就跳过
                        // (防止覆盖掉之前可能已经下载好的“正文内容”)
                        var existing = _dbService.GetNews(item.Url);

                        if (existing == null)
                        {
                            // 2. 只有库里没有这条新闻时，才存入
                            var cache = new NewsDetailCache
                            {
                                Url = item.Url,
                                Title = item.Title,
                                Date = item.Date,
                                Content = "", // 列表页没有正文，先留空
                                CreateTime = DateTime.Now
                            };
                            _dbService.SaveNews(cache);
                            count++;
                        }
                    }
                    // 调试输出，方便你知道存了多少
                    System.Diagnostics.Debug.WriteLine($"[LiteDB] 自动归档完成，新存入 {count} 条新闻目录。");
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
                // 点击时依然可以去完善正文（可选，为了更丰富的数据）
                // 但基础的标题和日期，在列表加载时就已经存好了。
                await Launcher.LaunchUriAsync(new Uri(item.Url));
            }
        }
    }
}