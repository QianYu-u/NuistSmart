using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
// ↓↓↓ 新增的工具箱：专门用来控制窗口大小的
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT.Interop;
using NuistSmart.Views;

namespace NuistSmart
{
    public sealed partial class MainWindow : Window
    {
        private AppWindow _appWindow; // 用来控制窗口的对象

        public MainWindow()
        {
            // [保留] 旧逻辑：初始化界面
            this.InitializeComponent();

            // [新增] 获取当前窗口的控制权
            _appWindow = GetAppWindowForCurrentWindow();

            // [新增] 启动时，强制把窗口变小（宽500，高750），适合登录页
            _appWindow.Resize(new Windows.Graphics.SizeInt32(500, 750));

            // [新增] 监听：如果页面跳转了，我就看看要不要改变窗口大小
            AppFrame.Navigated += AppFrame_Navigated;

            // [保留] 旧逻辑：让 Frame 导航到登录页
            AppFrame.Navigate(typeof(LoginPage));
        }

        // [新增] 这是一个“事件处理函数”，每次页面跳转完成都会触发
        private void AppFrame_Navigated(object sender, NavigationEventArgs e)
        {
            // 如果跳转到了主页 (ShellPage)，就把窗口最大化
            if (e.SourcePageType == typeof(ShellPage))
            {
                if (_appWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.Maximize();
                }
            }
            // 如果跳转回登录页，就把窗口变回小尺寸
            else if (e.SourcePageType == typeof(LoginPage))
            {
                if (_appWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.Restore(true);
                }
                _appWindow.Resize(new Windows.Graphics.SizeInt32(500, 750));
            }
        }

        // [新增] 这是一个辅助工具函数，用来获取 AppWindow（微软写的标准写法，照抄即可）
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }
    }
}