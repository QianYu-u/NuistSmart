using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;

namespace NuistSmart.Views
{
    public sealed partial class BookDetailPage : Page
    {
        private string _currentUrl = string.Empty;

        public BookDetailPage()
        {
            this.InitializeComponent();
            this.Loaded += BookDetailPage_Loaded;
        }

        /// <summary>
        /// 接收导航参数（DetailUrl 字符串）
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string url && !string.IsNullOrEmpty(url))
            {
                _currentUrl = url;
            }
        }

        private async void BookDetailPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentUrl)) return;

            try
            {
                // 确保 WebView2 已初始化
                await DetailWebView.EnsureCoreWebView2Async();

                // 注入 CSS 隐藏图书馆网站的顶部导航栏等冗余 UI
                DetailWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

                // 导航到详情页
                DetailWebView.CoreWebView2.Navigate(_currentUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BookDetailPage] WebView2 初始化异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 页面加载完成后，注入 CSS 隐藏多余的网站 UI 元素
        /// </summary>
        private async void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (!args.IsSuccess) return;

            LoadingBar.Visibility = Visibility.Collapsed;

            try
            {
                // 隐藏图书馆网站的顶部导航栏、侧边栏等，只保留书籍详情内容
                string css = @"
                    .header, .nav, .navbar, .top-bar, .site-header,
                    .footer, .site-footer, .bottom-bar,
                    .sidebar, .side-bar,
                    .breadcrumb, .bread-crumb,
                    #header, #nav, #footer, #sidebar {
                        display: none !important;
                    }
                ";
                string script = $@"
                    var style = document.createElement('style');
                    style.textContent = `{css}`;
                    document.head.appendChild(style);
                ";
                await DetailWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BookDetailPage] CSS 注入异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 返回按钮：回退到图书列表
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame != null && this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        /// <summary>
        /// 在系统浏览器中打开当前页面
        /// </summary>
        private async void OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentUrl))
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(_currentUrl));
            }
        }
    }
}
