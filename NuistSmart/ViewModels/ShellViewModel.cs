using CommunityToolkit.Mvvm.ComponentModel;
using NuistSmart.Models;
using NuistSmart.Services;
using System;
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
        /// 用户头像 (BitmapImage 或其他 ImageSource)
        /// </summary>
        [ObservableProperty]
        private Microsoft.UI.Xaml.Media.ImageSource? userAvatar;

        /// <summary>
        /// 设置当前登录的用户
        /// 在登录成功后调用
        /// </summary>
        public async void SetCurrentUser(User user)
        {
            CurrentUser = user;
            WelcomeMessage = $"欢迎，{user.Name}";

            // 处理抓取到的 AvatarUrl (可能是 base64 或 http 链接)
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                if (user.AvatarUrl.StartsWith("data:image"))
                {
                    try
                    {
                        var parts = user.AvatarUrl.Split(',');
                        if (parts.Length == 2)
                        {
                            byte[] imageBytes = System.Convert.FromBase64String(parts[1]);
                            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                            using (var writer = new Windows.Storage.Streams.DataWriter(stream.GetOutputStreamAt(0)))
                            {
                                writer.WriteBytes(imageBytes);
                                await writer.StoreAsync();
                            }
                            var bmp = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                            await bmp.SetSourceAsync(stream);
                            UserAvatar = bmp;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine($"[ShellViewModel] 头像解析失败: {ex.Message}");
                    }
                }
                else
                {
                    try
                    {
                        UserAvatar = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new System.Uri(user.AvatarUrl));
                    }
                    catch { }
                }
            }
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
                
                var calendarData = await _calendarService.FetchCalendarAsync();
                
                if (calendarData != null && calendarData.SchoolYearData != null)
                {
                    Debug.WriteLine($"[ShellViewModel] 获取到 {calendarData.SchoolYearData.XN} 学年数据，准备保存...");
                    _dbService.SaveCalendarData(calendarData);
                    Debug.WriteLine("[ShellViewModel] 校历数据更新完成");
                }
                else
                {
                    Debug.WriteLine("[ShellViewModel] 未获取到校历数据");
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"[ShellViewModel] 更新校历失败: {ex.Message}");
            }
        }
    }
}
