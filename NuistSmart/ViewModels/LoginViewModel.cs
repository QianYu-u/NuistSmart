using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using NuistSmart.Models;
using NuistSmart.Services;

namespace NuistSmart.ViewModels
{
    /// <summary>
    /// 登录页面的 ViewModel
    /// 继承 ObservableObject 是为了获得属性变化通知功能 (INotifyPropertyChanged)
    /// partial 关键字是必需的，因为源生成器会生成另一部分代码
    /// </summary>
    public partial class LoginViewModel : ObservableObject
    {
        private readonly ILoginService _loginService;
        private readonly IGithubService _githubService;

        // ============ 使用 [ObservableProperty] 源生成器特性 ============
        // 这个特性会自动生成：
        // 1. 私有字段 _studentId
        // 2. 公共属性 StudentId 
        // 3. 属性变化时自动触发 PropertyChanged 事件
        // 不需要手动写 get/set 和 OnPropertyChanged 调用，大大简化代码！

        /// <summary>
        /// 学号输入框绑定的属性
        /// </summary>
        [ObservableProperty]
        private string studentId = string.Empty;

        /// <summary>
        /// 密码输入框绑定的属性
        /// </summary>
        [ObservableProperty]
        private string password = string.Empty;

        /// <summary>
        /// 是否正在加载中（控制加载圈的显示）
        /// 当为 true 时，界面会显示 ProgressRing，并禁用登录按钮
        /// </summary>
        [ObservableProperty]
        private bool isLoading = false;

        /// <summary>
        /// 错误消息（如果有）
        /// </summary>
        [ObservableProperty]
        private string errorMessage = string.Empty;

        /// <summary>
        /// 公告文本，从 GitHub 动态获取
        /// </summary>
        [ObservableProperty]
        private string announcementText = "正在加载公告...";

        // ============ 登录成功事件 ============
        // 因为 ViewModel 不应该直接引用 View 或进行导航
        // 所以使用事件通知 View 层登录成功，由 View 负责导航
        public event EventHandler<User>? LoginSucceeded;

        // ============ 构造函数 ============
        public LoginViewModel(ILoginService loginService, IGithubService githubService)
        {
            _loginService = loginService;
            _githubService = githubService;
            
            // 异步加载公告
            _ = LoadAnnouncementAsync();
        }

        // 无参构造函数用于设计时数据绑定
        public LoginViewModel() : this(new LoginService(), new GithubService())
        {
        }

        /// <summary>
        /// 异步加载公告内容
        /// </summary>
        private async Task LoadAnnouncementAsync()
        {
            try
            {
                AnnouncementText = await _githubService.GetAdvisoryAsync();
            }
            catch (Exception)
            {
                // 如果加载失败，显示默认消息
                AnnouncementText = "欢迎使用 NuistSmart！";
            }
        }

        // ============ 使用 [RelayCommand] 源生成器特性 ============
        // 这个特性会自动生成：
        // 1. LoginCommand 属性（类型为 IAsyncRelayCommand）
        // 2. 自动处理异步方法
        // 3. 自动处理 CanExecute 逻辑
        // 不需要手动创建 RelayCommand 对象！

        /// <summary>
        /// 登录命令的实现方法
        /// [RelayCommand] 会自动生成 LoginCommand 属性供 XAML 绑定
        /// </summary>
        [RelayCommand]
        private async Task LoginAsync()
        {
            // 清空之前的错误消息
            ErrorMessage = string.Empty;

            // 简单的前端验证
            if (string.IsNullOrWhiteSpace(StudentId))
            {
                ErrorMessage = "请输入学号";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "请输入密码";
                return;
            }

            // 开始加载 - 这会自动更新 UI（显示加载圈，禁用按钮）
            IsLoading = true;

            try
            {
                // 调用登录服务
                var user = await _loginService.LoginAsync(StudentId, Password);

                if (user != null)
                {
                    // 登录成功 - 触发事件通知 View
                    LoginSucceeded?.Invoke(this, user);
                }
                else
                {
                    // 登录失败 - 显示错误消息
                    ErrorMessage = "学号或密码错误，请重试";
                }
            }
            catch (Exception ex)
            {
                // 网络错误或其他异常
                ErrorMessage = $"登录失败：{ex.Message}";
            }
            finally
            {
                // 无论成功失败，都要停止加载状态
                IsLoading = false;
            }
        }
    }
}
