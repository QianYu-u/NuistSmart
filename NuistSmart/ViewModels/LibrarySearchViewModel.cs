using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuistSmart.Models;
using NuistSmart.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NuistSmart.ViewModels
{
    public partial class LibrarySearchViewModel : ObservableObject
    {
        private readonly LibraryService _libraryService;

        public LibrarySearchViewModel(LibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        /// <summary>
        /// 当前显示的图书列表
        /// </summary>
        public ObservableCollection<BookItem> Books { get; } = new();

        [ObservableProperty]
        private string searchKeyword = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool showEmptyState;

        [ObservableProperty]
        private string emptyStateMessage = "暂无图书数据";

        [ObservableProperty]
        private string displayTitle = "新书推荐";

        /// <summary>
        /// 加载最新图书列表（页面初始化时自动调用）
        /// </summary>
        [RelayCommand]
        private async Task LoadNewBooksAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            ShowEmptyState = false;
            DisplayTitle = "新书推荐";
            Books.Clear();

            try
            {
                var books = await _libraryService.GetNewBooksAsync(page: 1, pageSize: 20);
                AddBooksWithCovers(books);
                
                if (Books.Count == 0)
                {
                    EmptyStateMessage = "未发现新入馆图书";
                    ShowEmptyState = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibrarySearchVM] 加载新书异常: {ex.Message}");
                EmptyStateMessage = "加载失败，请检查网络";
                ShowEmptyState = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 搜索图书
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                // 如果清空搜索，变回加载新书
                await LoadNewBooksAsync();
                return;
            }

            IsLoading = true;
            ShowEmptyState = false;
            DisplayTitle = "搜索结果";
            Books.Clear();

            try
            {
                var books = await _libraryService.SearchBooksAsync(SearchKeyword);
                AddBooksWithCovers(books);

                if (Books.Count == 0)
                {
                    EmptyStateMessage = $"未找到关于 \"{SearchKeyword}\" 的图书";
                    ShowEmptyState = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibrarySearchVM] 搜索异常: {ex.Message}");
                EmptyStateMessage = "搜索出错了";
                ShowEmptyState = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 辅助方法：将书籍加入集合并触发异步封面加载
        /// </summary>
        private void AddBooksWithCovers(System.Collections.Generic.IEnumerable<BookItem> books)
        {
            foreach (var book in books)
            {
                string isbn = book.Description;
                book.Description = string.Empty;
                Books.Add(book);

                _ = LoadCoverAsync(book, isbn);
            }
        }

        /// <summary>
        /// 异步加载单本书的封面图片
        /// </summary>
        private async Task LoadCoverAsync(BookItem book, string isbn)
        {
            try
            {
                string coverUrl = await _libraryService.GetCoverUrlAsync(isbn);
                if (!string.IsNullOrEmpty(coverUrl))
                {
                    book.CoverUrl = coverUrl;
                }
                else
                {
                    book.CoverUrl = "ms-appx:///Assets/Square150x150Logo.scale-200.png";
                }
            }
            catch
            {
                book.CoverUrl = "ms-appx:///Assets/Square150x150Logo.scale-200.png";
            }
        }
    }
}
