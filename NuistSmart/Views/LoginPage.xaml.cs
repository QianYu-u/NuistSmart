using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NuistSmart.ViewModels;
using System;
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
        }

        /// <summary>
        /// 处理登录成功事件
        /// 这里是少数可以在代码后台处理的逻辑：页面导航
        /// 因为导航涉及到 Frame，属于 View 层的职责
        /// </summary>
        private void OnLoginSucceeded(object? sender, Models.User user)
        {
            // 获取当前页面所在的 Frame
            // Frame 是 WinUI/UWP 中用于页面导航的容器
            var frame = this.Frame;

            if (frame != null)
            {
                // 导航到 ShellPage（主界面）
                // 并传递用户信息作为参数
                frame.Navigate(typeof(ShellPage), user);
            }
        }

        /// <summary>
        /// 处理 GitHub 按钮点击事件
        /// 使用系统默认浏览器打开项目主页
        /// </summary>
        private async void OnGitHubButtonClick(object sender, RoutedEventArgs e)
        {
            _ = await Launcher.LaunchUriAsync(new Uri("https://github.com/QianYu-u/NuistSmart"));
        }
    }
}
