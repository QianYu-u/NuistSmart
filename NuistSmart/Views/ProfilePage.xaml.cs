using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;

namespace NuistSmart.Views
{
    /// <summary>
    /// 个人信息页面 - 通过 WebView2 嵌入 CAS 个人中心
    /// 复用 CAS 登录后的 Cookie Session，注入 CSS 隐藏原页面冗余元素
    /// </summary>
    public sealed partial class ProfilePage : Page
    {
        /// <summary>
        /// CAS 个人中心 URL
        /// </summary>
        private const string PersonalCenterUrl =
            "https://authserver.nuist.edu.cn/personalInfo/personCenter/index.html#/accountsecurity";

        /// <summary>
        /// 标记是否已完成初始化（防止多次触发）
        /// </summary>
        private bool _isInitialized = false;

        public ProfilePage()
        {
            this.InitializeComponent();
            _ = InitializeWebViewAsync();
        }

        /// <summary>
        /// 初始化 WebView2 并注入自定义样式脚本
        /// </summary>
        private async System.Threading.Tasks.Task InitializeWebViewAsync()
        {
            try
            {
                await ProfileWebView.EnsureCoreWebView2Async();

                // 监听导航完成事件以切换 加载中 → 内容
                ProfileWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

                // 拦截导航，防止用户在 WebView 中跳转到无关页面
                ProfileWebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;

                // 注入 CSS/JS：在每次文档创建时自动执行
                string injectScript = BuildInjectScript();
                await ProfileWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(injectScript);

                // 导航到个人中心
                ProfileWebView.Source = new Uri(PersonalCenterUrl);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProfilePage] WebView2 初始化失败: {ex.Message}");
                ShowError($"WebView2 初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 构建注入脚本：隐藏原页面冗余元素，适配应用内嵌样式
        /// </summary>
        private static string BuildInjectScript()
        {
            return @"
                (function() {
                    const style = document.createElement('style');
                    style.type = 'text/css';
                    style.innerHTML = `
                        /* ===== 隐藏页面顶部导航栏与底部版权栏 ===== */
                        .header, .navbar, .nav-bar, header,
                        .top-bar, .top-nav, .el-header, nav.el-menu, .person-header,
                        footer, .footer, .copyright, [class*='footer'] {
                            display: none !important;
                        }

                        /* ===== 去除页面本身的山脉背景，融入原生 UI ===== */
                        html, body, #app, .app-main, .person-main, .person-content {
                            background-image: none !important;
                            background-color: transparent !important;
                            margin: 0 !important;
                            padding: 0 !important;
                        }

                        /* ===== 核心：让 mainframe 充满整个视口，屏蔽其他冗余元素 ===== */
                        .mainframe {
                            position: fixed !important;
                            top: 0 !important;
                            left: 0 !important;
                            width: 100vw !important;
                            height: 100vh !important;
                            max-width: none !important;
                            min-height: 100vh !important;
                            z-index: 99999 !important;
                            margin: 0 !important;
                            padding: 0 !important;
                            border-radius: 0 !important;
                            box-shadow: none !important;
                            display: flex !important; /* 如果原布局不是flex，强制使用以避免塌陷 */
                        }
                    `;

                    const inject = () => {
                        if (document.head) {
                            document.head.appendChild(style);
                        } else {
                            document.documentElement.appendChild(style);
                        }
                    };

                    if (document.readyState === 'loading') {
                        document.addEventListener('DOMContentLoaded', inject);
                    } else {
                        inject();
                    }

                    // 二次精准隐藏
                    function refineHide() {
                        var topNav = document.querySelector('.person-header') || document.querySelector('.el-header') || document.querySelector('header');
                        if (topNav) topNav.style.display = 'none';

                        var footerEl = document.querySelector('footer') || document.querySelector('.footer');
                        if (footerEl) footerEl.style.display = 'none';

                        document.body.style.paddingTop = '0';
                    }

                    setTimeout(refineHide, 500);
                    setTimeout(refineHide, 1500);
                    setTimeout(refineHide, 3000);

                    var observer = new MutationObserver(function() { refineHide(); });
                    if (document.body) {
                        observer.observe(document.body, { childList: true, subtree: true });
                    } else {
                        document.addEventListener('DOMContentLoaded', function() {
                            observer.observe(document.body, { childList: true, subtree: true });
                        });
                    }
                })();
            ";
        }

        /// <summary>
        /// 导航完成事件：切换 加载指示器 → WebView2 内容
        /// </summary>
        private void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                // 导航成功：隐藏加载中，显示 WebView
                LoadingPanel.Visibility = Visibility.Collapsed;
                ErrorPanel.Visibility = Visibility.Collapsed;
                ProfileWebView.Visibility = Visibility.Visible;

                Debug.WriteLine("[ProfilePage] 个人中心页面加载完成");
            }
            else
            {
                // 导航失败
                Debug.WriteLine($"[ProfilePage] 导航失败: {args.WebErrorStatus}");

                // 如果是需要认证（302 重定向到登录页），给用户友好提示
                if (sender.Source.Contains("authserver/login"))
                {
                    ShowError("登录会话已过期，请重新登录");
                }
                else
                {
                    ShowError($"页面加载失败 ({args.WebErrorStatus})");
                }
            }
        }

        /// <summary>
        /// 导航拦截：如果 WebView 被重定向到登录页，说明 Session 已过期
        /// 同时阻止跳转到非个人中心的外部页面
        /// </summary>
        private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            string url = args.Uri;
            if (string.IsNullOrEmpty(url)) return;

            Debug.WriteLine($"[ProfilePage] 导航拦截: {url}");

            // 被重定向到 CAS 登录页 → Session 过期
            if (url.Contains("authserver/login") && !url.Contains("personalInfo"))
            {
                args.Cancel = true;
                ShowError("登录会话已过期，请返回重新登录");
                return;
            }

            // 允许的域名白名单
            bool isAllowed = url.Contains("authserver.nuist.edu.cn");
            if (!isAllowed && !url.StartsWith("about:"))
            {
                args.Cancel = true;
                Debug.WriteLine($"[ProfilePage] 已拦截外部跳转: {url}");
            }
        }

        /// <summary>
        /// 显示错误状态
        /// </summary>
        private void ShowError(string message)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            ProfileWebView.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            ErrorText.Text = message;
        }

        /// <summary>
        /// 重新加载按钮点击事件
        /// </summary>
        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitialized)
            {
                // 重置到加载状态
                LoadingPanel.Visibility = Visibility.Visible;
                ErrorPanel.Visibility = Visibility.Collapsed;
                ProfileWebView.Visibility = Visibility.Collapsed;

                // 重新导航
                ProfileWebView.Source = new Uri(PersonalCenterUrl);
            }
            else
            {
                // 重新初始化
                _ = InitializeWebViewAsync();
            }
        }
    }
}
