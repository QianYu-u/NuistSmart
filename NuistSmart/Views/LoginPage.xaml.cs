using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using NuistSmart.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NuistSmart.Services;
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
        private string _capturedStudentId = "";
        
        /// <summary>
        /// 是否正在抓取个人信息，阻止重复重定向
        /// </summary>
        private bool _isFetchingProfile = false;

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

                        // ===== 捕获学号：多策略确保在表单提交前拿到用户名 =====
                        function captureUsername() {
                            var u = document.querySelector('#username');
                            if (!u) return;

                            function sendId() {
                                if (u.value) {
                                    window.chrome.webview.postMessage('STUDENT_ID:' + u.value);
                                }
                            }

                            // 策略 1：用户输入时实时捕获
                            u.addEventListener('input', sendId);
                            u.addEventListener('change', sendId);

                            // 策略 2：浏览器自动填充检测
                            setTimeout(sendId, 500);
                            setTimeout(sendId, 1500);

                            // 策略 3：拦截编程式 form.submit()（CAS 常用此方式加密提交）
                            var form = document.querySelector('form');
                            if (form) {
                                form.addEventListener('submit', sendId);
                                var origSubmit = form.submit;
                                form.submit = function() {
                                    sendId();
                                    return origSubmit.apply(this, arguments);
                                };
                            }

                            // 策略 4：监听所有提交按钮点击
                            document.querySelectorAll('button, input[type=submit], .login_btn').forEach(function(btn) {
                                btn.addEventListener('click', sendId, true);
                            });
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
        /// 双重事件监听之 NavigationStarting（改为 async 以支持 ExecuteScriptAsync）
        /// </summary>
        private async void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            string url = args.Uri;
            if (string.IsNullOrEmpty(url)) return;

            Debug.WriteLine($"[LoginPage] URL 拦截: {url}");

            // ===== 阶段 1：CAS 表单 POST 提交时，抢先读取学号 =====
            // 当用户点击登录，CAS 的 JS 会 POST 到 /authserver/login
            // 此时表单 DOM 还在，可以通过 ExecuteScriptAsync 读取 #username 的值
            if (url.Contains("authserver/login") && string.IsNullOrEmpty(_capturedStudentId))
            {
                try
                {
                    // 注意：不能 Cancel 这个 POST，只是趁 DOM 还在时读取学号
                    string result = await sender.ExecuteScriptAsync(
                        "document.querySelector('#username')?.value || ''");
                    if (result != null)
                    {
                        // ExecuteScriptAsync 返回 JSON 编码的字符串，如 "\"20231234\""
                        string sid = result.Trim('"');
                        if (!string.IsNullOrEmpty(sid))
                        {
                            _capturedStudentId = sid;
                            Debug.WriteLine($"[LoginPage] 已从 DOM 捕获学号: {_capturedStudentId}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LoginPage] ExecuteScript 读取学号异常: {ex.Message}");
                }
            }

            // ===== 阶段 2：拦截 CAS 登录成功后的重定向，进行后台个人信息抓取 =====
            if (url.Contains("ticket=ST-") ||
                url.Contains("i.nuist.edu.cn/login") ||
                url.Contains("i.nuist.edu.cn/default"))
            {
                if (_isFetchingProfile) return;

                // 强制阻断重定向
                args.Cancel = true;
                _isFetchingProfile = true;

                // 如果之前未通过 DOM 捕获到学号，最后再试一次
                if (string.IsNullOrEmpty(_capturedStudentId))
                {
                    try
                    {
                        string result = await sender.ExecuteScriptAsync(
                            "document.querySelector('#username')?.value || ''");
                        if (result != null)
                        {
                            string sid = result.Trim('"');
                            if (!string.IsNullOrEmpty(sid)) _capturedStudentId = sid;
                        }
                    }
                    catch (Exception) {}
                }

                // 改变 UI 状态为正在获取数据，屏蔽 WebView
                ViewModel.IsLoading = true;
                ViewModel.AnnouncementText = "正在获取用户信息...";
                LoginWebView.Visibility = Visibility.Collapsed;
                
                // 注入抓取姓名的脱壳脚本
                string profileScript = @"
                    var observer = new MutationObserver(function() {
                        var nameLabel = document.querySelector('.userinfo-cn');
                        if (nameLabel) {
                            var img = document.querySelector('.userinfo img');
                            var avatar = img && img.src ? img.src : '';
                            window.chrome.webview.postMessage('PROFILE:' + nameLabel.innerText + '|' + avatar);
                            observer.disconnect();
                        }
                    });
                    if (document.body) observer.observe(document.body, { childList: true, subtree: true });
                    else document.addEventListener('DOMContentLoaded', () => observer.observe(document.body, { childList: true, subtree: true }));
                    
                    // 超时回退机制
                    setTimeout(function(){ window.chrome.webview.postMessage('PROFILE_FAIL:'); }, 5000);
                ";

                await sender.AddScriptToExecuteOnDocumentCreatedAsync(profileScript);

                // 隐式导航到个人中心页抓取 DOM 数据
                LoginWebView.Source = new Uri("https://authserver.nuist.edu.cn/personalInfo/personCenter/index.html");
                return;
            }
        }

        /// <summary>
        /// 双重事件监听之 SourceChanged（备用拦截）
        /// </summary>
        private void CoreWebView2_SourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
        {
            string url = sender.Source;
            if (string.IsNullOrEmpty(url)) return;

            if (url.Contains("ticket=ST-") ||
                url.Contains("i.nuist.edu.cn/login") ||
                url.Contains("i.nuist.edu.cn/default"))
            {
                if (_isFetchingProfile) return;
                
                sender.Stop();
                // 依赖 NavigationStarting 进行抓取，所以这里直接阻止并等待
                // 但如果万一漏掉了
            }
        }

        /// <summary>
        /// 接收来自 CAS 页面 JS 的 postMessage，用于捕获学号（备用通道）
        /// </summary>
        private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                string message = args.TryGetWebMessageAsString();
                if (message?.StartsWith("STUDENT_ID:") == true)
                {
                    _capturedStudentId = message.Substring("STUDENT_ID:".Length);
                    Debug.WriteLine($"[LoginPage] postMessage 捕获学号: {_capturedStudentId}");
                }
                else if (message?.StartsWith("PROFILE:") == true)
                {
                    if (_hasNavigatedToShell) return;
                    string profileData = message.Substring("PROFILE:".Length);
                    string[] parts = profileData.Split('|');
                    string name = parts.Length > 0 ? parts[0] : "";
                    string avatarUrl = parts.Length > 1 ? parts[1] : null;

                    var user = new Models.User 
                    { 
                        StudentId = string.IsNullOrEmpty(_capturedStudentId) ? "未知" : _capturedStudentId,
                        Name = string.IsNullOrEmpty(name) ? "未知" : name,
                        AvatarUrl = avatarUrl
                    };
                    
                    sender.NavigationStarting -= CoreWebView2_NavigationStarting;
                    sender.SourceChanged -= CoreWebView2_SourceChanged;
                    sender.WebMessageReceived -= CoreWebView2_WebMessageReceived;

                    Debug.WriteLine($"[LoginPage] 抓取到用户信息: {user.Name}");
                    
                    // 将登录用户信息保存到本地数据库
                    App.ServiceProvider.GetService<DbService>()?.SaveUserInfo(user);

                    NavigateToShellPage(user);
                }
                else if (message?.StartsWith("PROFILE_FAIL:") == true)
                {
                    if (_hasNavigatedToShell) return;
                    var user = new Models.User 
                    { 
                        StudentId = string.IsNullOrEmpty(_capturedStudentId) ? "未知" : _capturedStudentId, 
                        Name = "未知" 
                    };
                    sender.NavigationStarting -= CoreWebView2_NavigationStarting;
                    sender.SourceChanged -= CoreWebView2_SourceChanged;
                    sender.WebMessageReceived -= CoreWebView2_WebMessageReceived;

                    // 虽然抓取失败，但只要有学号也保存一下（或者作为游客保存）
                    App.ServiceProvider.GetService<DbService>()?.SaveUserInfo(user);
                    
                    NavigateToShellPage(user);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginPage] WebMessage 处理异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 标记是否已经跳转到主页，防抖
        /// </summary>
        private bool _hasNavigatedToShell = false;

        /// <summary>
        /// 跳转回 ShellPage，包含 Frame 空值兼容处理
        /// </summary>
        private void NavigateToShellPage(Models.User user)
        {
            if (_hasNavigatedToShell) return;
            _hasNavigatedToShell = true;

            Debug.WriteLine($"[LoginPage] 准备跳转 ShellPage，用户: {user.Name} ({user.StudentId})");

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
                Debug.WriteLine("[LoginPage] 已调用 Frame.Navigate -> ShellPage");
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
