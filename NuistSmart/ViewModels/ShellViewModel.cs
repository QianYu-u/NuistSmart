using CommunityToolkit.Mvvm.ComponentModel;
using NuistSmart.Models;
using NuistSmart.Services;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NuistSmart.ViewModels
{
    /// <summary>
    /// 主界面 (Shell) 的 ViewModel
    /// 负责管理当前登录用户信息和导航栏状态
    /// </summary>
    public partial class ShellViewModel : ObservableObject
    {
        private readonly CalendarService _calendarService;
        private readonly DbService _dbService;

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

        public ShellViewModel(CalendarService calendarService, DbService dbService)
        {
            _calendarService = calendarService;
            _dbService = dbService;
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

        /// <summary>
        /// 更新校历数据
        /// 从教务系统抓取假期信息并存入数据库
        /// </summary>
        public async Task UpdateCalendarAsync()
        {
            try
            {
                Debug.WriteLine("[ShellViewModel] 开始更新校历数据...");
                
                var holidays = await _calendarService.FetchHolidaysAsync();
                
                if (holidays.Count > 0)
                {
                    Debug.WriteLine($"[ShellViewModel] 获取到 {holidays.Count} 条假期数据，准备保存...");
                    _dbService.SaveHolidays(holidays);
                    Debug.WriteLine("[ShellViewModel] 校历数据更新完成");
                }
                else
                {
                    Debug.WriteLine("[ShellViewModel] 未获取到假期数据");
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"[ShellViewModel] 更新校历失败: {ex.Message}");
            }
        }
    }
}
