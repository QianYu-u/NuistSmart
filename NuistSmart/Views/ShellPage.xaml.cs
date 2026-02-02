using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NuistSmart.Models;
using NuistSmart.ViewModels;
using global::Microsoft.UI.Xaml.Markup;
using System;

namespace NuistSmart.Views
{
    public sealed partial class ShellPage : Page
    {
        public ShellViewModel ViewModel { get; }

        public ShellPage()
        {
            this.InitializeComponent();
            ViewModel = new ShellViewModel();

            // 默认选中第一个菜单项（智能对话）
            MainNavigationView.SelectedItem = MainNavigationView.MenuItems[0];
        }

        /// <summary>
        /// 页面导航到这里时触发
        /// OnNavigatedTo 是 Page 的生命周期方法
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取从登录页传递过来的用户信息
            // e.Parameter 包含导航时传递的参数
            if (e.Parameter is User user)
            {
                // 设置当前用户到 ViewModel
                ViewModel.SetCurrentUser(user);

                // 更新 UI 显示学号
                StudentIdText.Text = $"学号：{user.StudentId}";
            }
        }

        /// <summary>
        /// NavigationView 菜单项选择变化时触发
        /// 这里可以根据选择的菜单项导航到不同的页面
        /// </summary>
        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            // 获取选中的菜单项
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                // 获取菜单项的 Tag 属性，用于标识要导航到哪个页面
                string? tag = selectedItem.Tag?.ToString();

                switch (tag)
                {
                    case "ChatPage":
                        // TODO: 导航到智能对话页面
                        // ContentFrame.Navigate(typeof(ChatPage));
                        ShowPlaceholder("智能对话功能正在开发中...");
                        break;

                    case "QueryPage":
                        // TODO: 导航到教务查询页面
                        // ContentFrame.Navigate(typeof(QueryPage));
                        ShowPlaceholder("教务查询功能正在开发中...");
                        break;

                    case "AboutPage":
                        // TODO: 导航到关于页面
                        // ContentFrame.Navigate(typeof(AboutPage));
                        ShowPlaceholder("关于页面正在开发中...");
                        break;
                }
            }
            // 如果选中的是设置项
            else if (args.IsSettingsSelected)
            {
                ShowPlaceholder("设置页面正在开发中...");
            }
        }

        /// <summary>
        /// 显示占位内容（功能未实现时的提示）
        /// </summary>
        private void ShowPlaceholder(string message)
        {
            // 清空 Frame 内容
            ContentFrame.Content = null;

            // 创建一个简单的占位界面
            var placeholder = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var stackPanel = new StackPanel
            {
                Spacing = 16,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var icon = new FontIcon
            {
                Glyph = "\uE946", // Info 图标
                FontSize = 64,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
            };

            var textBlock = new TextBlock
            {
                Text = message,
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stackPanel.Children.Add(icon);
            stackPanel.Children.Add(textBlock);
            placeholder.Children.Add(stackPanel);

            // 将占位界面设置为 Frame 的内容
            ContentFrame.Content = placeholder;
        }
    }
}
