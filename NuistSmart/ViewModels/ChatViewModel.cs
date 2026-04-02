using System;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuistSmart.Services;

namespace NuistSmart.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly DbService _dbService;
        private readonly AiService _aiService;

        [ObservableProperty]
        private string userQuery = string.Empty;

        [ObservableProperty]
        private string aiAnswer = string.Empty;

        [ObservableProperty]
        private bool isAskingAi;

        public ChatViewModel(DbService dbService, AiService aiService)
        {
            _dbService = dbService;
            _aiService = aiService;
        }

        [RelayCommand]
        private async Task AskAiAsync()
        {
            if (string.IsNullOrWhiteSpace(UserQuery)) return;
            if (IsAskingAi) return;

            IsAskingAi = true;
            AiAnswer = string.Empty;
            try
            {
                // 1. 从数据库取最近 30 天公告作为上下文
                var recentNews = await Task.Run(() => _dbService.GetRecentNews(30));
                var contacts = await Task.Run(() => _dbService.GetAllContacts());

                if (recentNews.Count == 0 && contacts.Count == 0)
                {
                    AiAnswer = "本地数据库中暂无任何数据，请先浏览“信息公告”或加载其他模块后再试。";
                    return;
                }

                // 2. 拼接上下文（标题 + 日期 + 黄页通讯录）
                var sb = new StringBuilder();
                if (recentNews.Count > 0)
                {
                    sb.AppendLine("【近期校园公告列表】");
                    foreach (var news in recentNews)
                    {
                        sb.AppendLine($"- [{news.Date}] {news.Title}");
                    }
                }

                if (contacts.Count > 0)
                {
                    sb.AppendLine("\n【校园各部门通讯录/黄页】");
                    foreach (var contact in contacts)
                    {
                        // Some MC names include department info, some don't. We just dump everything cleanly.
                        sb.AppendLine($"- 名称: {contact.Name} | 电话: {contact.Phone} | 地点: {contact.WorkPlace}");
                    }
                }

                var context = sb.ToString();

                System.Diagnostics.Debug.WriteLine($"[SmartChat] 上下文包含 {recentNews.Count} 条公告和 {contacts.Count} 个联系人");

                // 3. 调用 AI 服务
                var answer = await _aiService.GetAnswerAsync(UserQuery, context);

                // 4. 更新回答
                AiAnswer = answer;
            }
            catch (Exception ex)
            {
                AiAnswer = $"查询失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[SmartChat] 异常: {ex.Message}");
            }
            finally
            {
                IsAskingAi = false;
            }
        }
    }
}
