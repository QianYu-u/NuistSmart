using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using NuistSmart.ViewModels;
using System;
using System.Diagnostics;
using Windows.System;

namespace NuistSmart.Views
{
    /// <summary>
    /// 登录页面的代码后台
    /// 在 MVVM 模式中，代码后台应该尽量简洁
    /// 主要负责页面初始化和必要的 UI 逻辑
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        /// <summary>
        /// 从 CAS 表单捕获的学号（通过 JS postMessage 传回）
        /// </summary>
        private string _capturedStudentId = "";
        // 使用属性暴露 ViewModel，方便在 XAML 中使用 x:Bind
        // x:Bind 比 Binding 性能更好，因为它是编译时绑定
        public LoginViewModel ViewModel { get; }

        public LoginPage()
        {
            this.InitializeComponent();

            // 初始化 ViewModel
            ViewModel = new LoginViewModel();

            // 订阅登录成功事件
            // 当 ViewModel 触发登录成功事件时，进行页面导航
            ViewModel.LoginSucceeded += OnLoginSucceeded;

            // 异步初始化 WebView2 并注入样式/脚本
            _ = InitializeWebViewAsync();
        }

        /// <summary>
        /// 初始化 WebView2 控件并注入 CAS 登录页的适配脚本
        /// </summary>
        private async System.Threading.Tasks.Task InitializeWebViewAsync()
        {
            try
            {
                // 等待 CoreWebView2 初始化完成
                await LoginWebView.EnsureCoreWebView2Async();

                // 挂载跳转拦截逻辑，双重监听防止漏掉 location.replace
                LoginWebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
                LoginWebView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;

                // 监听来自 JS 的 postMessage（用于捕获学号）
                LoginWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                // 编写注入的 JavaScript 与 CSS 规则
                // 1. 去除背景 (body, html, .mainbg)
                // 2. 隐藏冗余头尾 (.dz_logo, .dz_language, .dz_left, .footer 等)
                // 3. 居中与缩放 Login Box
                string injectScript = @"
                    (function() {
                        const style = document.createElement('style');
                        style.type = 'text/css';
                        style.innerHTML = `
                            /* 只隐藏外部无关元素，保留完整的 <section class='main'> 即整个登录卡片 */
                            .dz_logo, .dz_language, .dz_left, .lunbo_nav, img.mainbg {
                                display: none !important;
                            }
                            
                            /* 去除背景，设置为透明或白色以融入原生 UI */
                            html, body, .container {
                                background-image: none !important;
                                background-color: transparent !important;
                            }
                            
                            /* 尺寸自适应，将核心容器居中 */
                            body {
                                margin: 0 !important;
                                padding: 0 !important;
                                display: flex !important;
                                justify-content: center !important;
                                align-items: center !important;
                                height: 100vh !important;
                                overflow: hidden !important; 
                            }
                            
                            .wrap {
                                width: 100% !important;
                                background: none !important;
                                box-shadow: none !important;
                                display: flex !important;
                                justify-content: center !important;
                                align-items: center !important;
                                padding: 0 !important;
                            }

                            /* 缩放核心登录框（.main 容器）以完整显示在可视区域内 */
                            .main {
                                margin: 0 !important;
                                padding: 0 !important;
                                background: #FFFFFF !important;
                                box-shadow: none !important;
                                border-radius: 8px !important;
                                transform: scale(0.9);
                                transform-origin: center center;
                            }
                        `;
                        
                        // 由于 AddScriptToExecuteOnDocumentCreatedAsync 注入极早，此时可能还没解析到 document.head
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

                        // ===== 捕获学号：在表单提交时，将用户名通过 postMessage 回传给 C# =====
                        function captureUsername() {
                            var form = document.querySelector('form');
                            if (form) {
                                form.addEventListener('submit', function() {
                                    var u = document.querySelector('#username');
                                    if (u && u.value) {
                                        window.chrome.webview.postMessage('STUDENT_ID:' + u.value);
                                    }
                                });
                            }
                        }
                        if (document.readyState === 'loading') {
                            document.addEventListener('DOMContentLoaded', captureUsername);
                        } else {
                            captureUsername();
                        }
                    })();
                ";

                // 在文档初始创建时立刻注入，以最大程度避免页面闪烁
                await LoginWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(injectScript);

                // 关键：先注册脚本，再导航。确保脚本在页面加载前就位
                LoginWebView.Source = new Uri("https://authserver.nuist.edu.cn/authserver/login?service=https%3A%2F%2Fi.nuist.edu.cn%2Flogin%23%2F");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebView2 初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 双重事件监听之 NavigationStarting
        /// </summary>
        private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            CheckAndInterceptUrl(args.Uri, sender, args);
        }

        /// <summary>
        /// 双重事件监听之 SourceChanged
        /// </summary>
        private void CoreWebView2_SourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
        {
            CheckAndInterceptUrl(sender.Source, sender, null);
        }

        /// <summary>
        /// 接收来自 CAS 页面 JS 的 postMessage，用于捕获学号
        /// </summary>
        private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                string message = args.TryGetWebMessageAsString();
                if (message?.StartsWith("STUDENT_ID:") == true)
                {
                    _capturedStudentId = message.Substring("STUDENT_ID:".Length);
                    Debug.WriteLine($"[LoginPage] 已捕获学号: {_capturedStudentId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginPage] WebMessage 处理异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 核心 URL 检查与阻断跳转逻辑
        /// </summary>
        private void CheckAndInterceptUrl(string url, CoreWebView2 webView, CoreWebView2NavigationStartingEventArgs? startArgs)
        {
            if (string.IsNullOrEmpty(url)) return;

            // 埋点日志：打印经过的所有 URL
            Debug.WriteLine($"[LoginPage] URL 拦截: {url}");

            // 检查是否命中 CAS 登录成功的特征参数与跳转路由
            if (url.Contains("ticket=ST-") || 
                url.Contains("i.nuist.edu.cn/login") || 
                url.Contains("i.nuist.edu.cn/default"))
            {
                // 强制阻断，阻止后续网页加载与跳转
                if (startArgs != null)
                {
                    startArgs.Cancel = true;
                }
                webView.Stop();

                // 立刻注销事件监听，防止重定向循环导致重复触发
                webView.NavigationStarting -= CoreWebView2_NavigationStarting;
                webView.SourceChanged -= CoreWebView2_SourceChanged;
                webView.WebMessageReceived -= CoreWebView2_WebMessageReceived;

                // 使用从 CAS 表单捕获的学号（由 JS postMessage 回传）
                var user = new Models.User { StudentId = _capturedStudentId };
                Debug.WriteLine($"[LoginPage] 导航到主界面，学号: {_capturedStudentId}");
                
                // 执行页面跳转回原生 UI 界面
                NavigateToShellPage(user);
            }
        }

        /// <summary>
        /// 跳转回 ShellPage，包含 Frame 空值兼容处理
        /// </summary>
        private void NavigateToShellPage(Models.User user)
        {
            Frame? rootFrame = this.Frame;

            // 检查 this.Frame 是否为 null。若为 null，则通过可视化层级树获取根 Frame 进行跳转，防止指令失效
            if (rootFrame == null)
            {
                DependencyObject? parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(this);
                while (parent != null && !(parent is Frame))
                {
                    parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
                }
                rootFrame = parent as Frame;
            }

            if (rootFrame != null)
            {
                rootFrame.Navigate(typeof(ShellPage), user);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("导航失败：未能在视图树中查找到关联的 Frame 容器。");
            }
        }

        /// <summary>
        /// 处理来自 ViewModel 的登录成功事件（如果有）
        /// </summary>
        private void OnLoginSucceeded(object? sender, Models.User user)
        {
            NavigateToShellPage(user);
        }

        /// <summary>
        /// 处理 GitHub 按钮点击事件
        /// </summary>
        private async void OnGitHubButtonClick(object sender, RoutedEventArgs e)
        {
            _ = await Launcher.LaunchUriAsync(new Uri("https://github.com/QianYu-u/NuistSmart"));
        }
    }
}
