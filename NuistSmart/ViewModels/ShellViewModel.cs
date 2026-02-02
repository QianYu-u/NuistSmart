using CommunityToolkit.Mvvm.ComponentModel;
using NuistSmart.Models;

namespace NuistSmart.ViewModels
{
    /// <summary>
    /// 主界面 (Shell) 的 ViewModel
    /// 负责管理当前登录用户信息和导航栏状态
    /// </summary>
    public partial class ShellViewModel : ObservableObject
    {
        /// <summary>
        /// 当前登录的用户信息
        /// 使用 [ObservableProperty] 自动生成属性变化通知
        /// </summary>
        [ObservableProperty]
        private User? currentUser;

        /// <summary>
        /// 欢迎消息（显示在界面顶部）
        /// </summary>
        [ObservableProperty]
        private string welcomeMessage = "欢迎使用南信大智能体";

        public ShellViewModel()
        {
        }

        /// <summary>
        /// 设置当前登录的用户
        /// 在登录成功后调用
        /// </summary>
        public void SetCurrentUser(User user)
        {
            CurrentUser = user;
            WelcomeMessage = $"欢迎，{user.Name} ({user.StudentId})";
        }
    }
}
