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
            this.InitializeComponent();

            // 扩展内容到标题栏，实现现代化布局
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null);

            // 获取当前窗口的控制权
            _appWindow = GetAppWindowForCurrentWindow();

            // 启动时，设置登录页窗口：固定宽屏尺寸 960x600，适配左右分栏布局
            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMaximizable = false;
                presenter.IsResizable = false;
                presenter.IsMinimizable = true;
            }
            _appWindow.Resize(new Windows.Graphics.SizeInt32(960, 600));
            CenterWindow();

            // 监听页面跳转事件，动态调整窗口状态
            AppFrame.Navigated += AppFrame_Navigated;

            // 导航到登录页
            AppFrame.Navigate(typeof(LoginPage));
        }

        // [新增] 这是一个“事件处理函数”，每次页面跳转完成都会触发
        private void AppFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (_appWindow.Presenter is not OverlappedPresenter presenter)
                return;

            // 进入主页 (ShellPage)：恢复所有窗口功能，设置舒适的桌面尺寸
            if (e.SourcePageType == typeof(ShellPage))
            {
                presenter.IsMaximizable = true;
                presenter.IsResizable = true;
                presenter.IsMinimizable = true;
                presenter.Restore(true);
                
                _appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));
                CenterWindow();
            }
            // 返回登录页：禁止最大化和拉伸，固定宽屏尺寸适配左右分栏
            else if (e.SourcePageType == typeof(LoginPage))
            {
                presenter.IsMaximizable = false;
                presenter.IsResizable = false;
                presenter.IsMinimizable = true;
                presenter.Restore(true);
                
                _appWindow.Resize(new Windows.Graphics.SizeInt32(960, 600));
                CenterWindow();
            }
        }

        // 获取 AppWindow 的辅助函数
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        // 窗口居中显示的辅助函数
        private void CenterWindow()
        {
            if (_appWindow == null) return;

            var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;
            var windowSize = _appWindow.Size;

            var x = (workArea.Width - windowSize.Width) / 2 + workArea.X;
            var y = (workArea.Height - windowSize.Height) / 2 + workArea.Y;

            _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }
    }
}