using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuistSmart.Services;
using NuistSmart.Models;
using Microsoft.UI.Dispatching;

namespace NuistSmart.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly DbService _dbService;
        private readonly AiService _aiService;
        private readonly BusService _busService;
        private readonly TokenCaptureService _tokenCaptureService;
        private readonly DispatcherQueue _dispatcherQueue;

        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private string userQuery = string.Empty;

        [ObservableProperty]
        private string aiAnswer = string.Empty;

        [ObservableProperty]
        private bool isAskingAi;

        // 聊天会话相关属性
        [ObservableProperty]
        private ChatSession? currentSession;

        [ObservableProperty]
        private ObservableCollection<ChatSession> sessionList = new();

        [ObservableProperty]
        private ObservableCollection<ChatMessage> messageHistory = new();

        [ObservableProperty]
        private bool isHistoryPanelOpen = false;

        public ChatViewModel(DbService dbService, AiService aiService, BusService busService, TokenCaptureService tokenCaptureService)
        {
            _dbService = dbService;
            _aiService = aiService;
            _busService = busService;
            _tokenCaptureService = tokenCaptureService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // 初始化会话
            InitializeSession();
        }

        /// <summary>
        /// 初始化会话：加载或创建会话
        /// </summary>
        private void InitializeSession()
        {
            try
            {
                // 加载所有会话列表
                LoadSessionList();

                // 获取当前激活的会话
                CurrentSession = _dbService.GetActiveSession();

                // 如果没有激活会话，创建一个新会话
                if (CurrentSession == null)
                {
                    CurrentSession = _dbService.CreateSession("新对话");
                    LoadSessionList(); // 刷新列表
                }

                // 加载当前会话的历史消息
                LoadCurrentSessionMessages();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 初始化会话失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载会话列表
        /// </summary>
        private void LoadSessionList()
        {
            try
            {
                var sessions = _dbService.GetAllSessions();
                SessionList.Clear();
                foreach (var session in sessions)
                {
                    SessionList.Add(session);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 加载会话列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载当前会话的历史消息
        /// </summary>
        private void LoadCurrentSessionMessages()
        {
            try
            {
                if (CurrentSession == null) return;

                var messages = _dbService.GetSessionMessages(CurrentSession.Id);
                MessageHistory.Clear();
                foreach (var msg in messages)
                {
                    MessageHistory.Add(msg);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 加载历史消息失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task AskAiAsync()
        {
            if (string.IsNullOrWhiteSpace(UserQuery)) return;
            if (IsAskingAi) return;
            if (CurrentSession == null)
            {
                // 如果没有当前会话，创建一个
                CurrentSession = _dbService.CreateSession("新对话");
                LoadSessionList();
            }

            IsAskingAi = true;
            var currentQuery = UserQuery;
            UserQuery = string.Empty; // 清空输入框

            try
            {
                // 1. 保存用户消息到数据库
                var userMessage = new ChatMessage
                {
                    SessionId = CurrentSession.Id,
                    Role = "user",
                    Content = currentQuery,
                    Timestamp = DateTime.Now
                };
                _dbService.SaveMessage(userMessage);
                MessageHistory.Add(userMessage);

                // 如果这是会话的第一条用户消息，更新会话标题
                if (MessageHistory.Count(m => m.Role == "user") == 1)
                {
                    var title = currentQuery.Length > 20 ? currentQuery.Substring(0, 20) + "..." : currentQuery;
                    _dbService.UpdateSessionTitle(CurrentSession.Id, title);
                    CurrentSession.Title = title;
                    LoadSessionList(); // 刷新列表显示
                }

                // 2. 从数据库取最近 30 天公告和联系人作为上下文
                var recentNews = await Task.Run(() => _dbService.GetRecentNews(30));
                var contacts = await Task.Run(() => _dbService.GetAllContacts());

                if (recentNews.Count == 0 && contacts.Count == 0)
                {
                    var errorMsg = "本地数据库中暂无任何数据，请先浏览\"信息公告\"或加载其他模块后再试。";
                    var errorMessage = new ChatMessage
                    {
                        SessionId = CurrentSession.Id,
                        Role = "assistant",
                        Content = errorMsg,
                        Timestamp = DateTime.Now
                    };
                    _dbService.SaveMessage(errorMessage);
                    MessageHistory.Add(errorMessage);
                    return;
                }

                // 3. 构建系统提示词（包含公告和联系人上下文）
                var contextBuilder = new StringBuilder();
                if (recentNews.Count > 0)
                {
                    // 限制公告数量，避免上下文过大导致 API 截断响应
                    var limitedNews = recentNews.Take(50).ToList();
                    contextBuilder.AppendLine($"【近期校园公告列表（最近{limitedNews.Count}条）】");
                    foreach (var news in limitedNews)
                    {
                        contextBuilder.AppendLine($"- [{news.Date}] {news.Title}");
                    }
                }

                if (contacts.Count > 0)
                {
                    contextBuilder.AppendLine("\n【校园各部门通讯录/黄页】");
                    foreach (var contact in contacts)
                    {
                        contextBuilder.AppendLine($"- 名称: {contact.Name} | 电话: {contact.Phone} | 地点: {contact.WorkPlace}");
                    }
                }

                contextBuilder.AppendLine($"\n【系统信息】当前日期: {DateTime.Now:yyyy-MM-dd}");

                var systemPrompt = "你是南京信息工程大学的智能助手，可以帮助学生和教职工处理以下任务：\n\n" +
                    "1. **校园信息查询**：基于校园公告、黄页通讯录回答问题\n" +
                    "2. **班车预约抢票**：帮助用户自动预约校园班车\n\n" +
                    "# 班车抢票功能说明\n" +
                    "当用户想要预约班车时，你需要：\n" +
                    "1. 识别用户意图（包含\"抢票\"、\"预约\"、\"班车\"、\"校车\"等关键词）\n" +
                    "2. 提取以下信息：\n" +
                    "   - 始发站：金牛湖尚学楼 或 本部文德楼\n" +
                    "   - 终点站：金牛湖尚学楼 或 本部文德楼\n" +
                    "   - 日期：格式 YYYY-MM-DD（如果用户说\"明天\"、\"今天\"，需要计算实际日期）\n" +
                    "   - 时间：格式 HH:mm（可选，如果不指定则获取所有班次）\n" +
                    "   - Token：用户的认证令牌（可选，如果没有提供则自动抓包）\n" +
                    "3. 当你判断用户想要抢票时，请在回复的**最后一行**输出特殊格式：\n" +
                    "   [BUS_REQUEST|始发站|终点站|日期|时间|Token]\n" +
                    "   例如：[BUS_REQUEST|金牛湖尚学楼|本部文德楼|2026-04-05|21:00|xxxtoken]\n" +
                    "   或者：[BUS_REQUEST|金牛湖尚学楼|本部文德楼|2026-04-05||]\n\n" +
                    "# 常规信息查询\n" +
                    "请严格基于你接收到的联系方式和公告列表来回答，如果你在黄页里找到了对应的部门电话，请直接回复部门名称、电话和地点。" +
                    "如果无法从列表中找到，请如实告知。\n\n" +
                    "回答得体、亲切，使用中文。\n\n" +
                    contextBuilder.ToString();

                // 4. 获取最近5轮对话历史
                var recentMessages = _dbService.GetRecentMessages(CurrentSession.Id, 5);

                // 5. 构建完整的messages数组
                var messagesList = new List<object>();
                messagesList.Add(new { role = "system", content = systemPrompt });

                // 添加历史对话（已包含当前用户消息，因为已在上方保存到数据库）
                foreach (var msg in recentMessages)
                {
                    messagesList.Add(new { role = msg.Role, content = msg.Content });
                }

                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 发送上下文对话，消息数: {messagesList.Count}");

                // 6. 调用 AI 服务（使用上下文对话方法）
                var answer = await _aiService.GetAnswerWithContextAsync(messagesList.ToArray());

                // 7. 检测是否包含班车预约请求
                var busRequest = ParseBusRequest(answer);
                if (busRequest != null)
                {
                    // 移除特殊标记，只显示用户友好的回复
                    var cleanAnswer = Regex.Replace(answer, @"\[BUS_REQUEST\|[^\]]+\]", "").Trim();
                    
                    // 保存AI回复（不包含班车请求标记）
                    var assistantMessage = new ChatMessage
                    {
                        SessionId = CurrentSession.Id,
                        Role = "assistant",
                        Content = cleanAnswer + "\n\n⏳ 正在为您处理班车预约请求...",
                        Timestamp = DateTime.Now
                    };
                    _dbService.SaveMessage(assistantMessage);
                    MessageHistory.Add(assistantMessage);

                    // 同时也更新AiAnswer以保持兼容性
                    AiAnswer = assistantMessage.Content;

                    // 执行抢票操作
                    await ExecuteBusRequestAsync(busRequest);
                }
                else
                {
                    // 8. 保存AI回复到数据库
                    var assistantMessage = new ChatMessage
                    {
                        SessionId = CurrentSession.Id,
                        Role = "assistant",
                        Content = answer,
                        Timestamp = DateTime.Now
                    };
                    _dbService.SaveMessage(assistantMessage);
                    MessageHistory.Add(assistantMessage);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = new ChatMessage
                {
                    SessionId = CurrentSession.Id,
                    Role = "assistant",
                    Content = $"查询失败: {ex.Message}",
                    Timestamp = DateTime.Now
                };
                _dbService.SaveMessage(errorMessage);
                MessageHistory.Add(errorMessage);
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 异常: {ex.Message}");
            }
            finally
            {
                IsAskingAi = false;
            }
        }

        private BusRequest? ParseBusRequest(string aiResponse)
        {
            // 匹配格式：[BUS_REQUEST|始发站|终点站|日期|时间|Token]
            var match = Regex.Match(aiResponse, @"\[BUS_REQUEST\|([^|]*)\|([^|]*)\|([^|]*)\|([^|]*)\|([^|]*)\]");
            if (!match.Success) return null;

            return new BusRequest
            {
                StartStation = match.Groups[1].Value.Trim(),
                EndStation = match.Groups[2].Value.Trim(),
                Date = match.Groups[3].Value.Trim(),
                Time = match.Groups[4].Value.Trim(),
                Token = match.Groups[5].Value.Trim()
            };
        }

        private async Task ExecuteBusRequestAsync(BusRequest request)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                AppendToAnswer("\n\n📍 始发站: " + request.StartStation);
                AppendToAnswer("\n📍 终点站: " + request.EndStation);
                AppendToAnswer("\n📅 日期: " + request.Date);
                if (!string.IsNullOrEmpty(request.Time))
                    AppendToAnswer("\n🕐 时间: " + request.Time);

                string token = request.Token;

                // 如果没有提供 Token，尝试自动抓包
                if (string.IsNullOrWhiteSpace(token))
                {
                    AppendToAnswer("\n\n🔑 未提供 Token，正在启动代理抓包...");
                    token = await _tokenCaptureService.StartCaptureAsync(
                        msg => AppendToAnswer("\n   " + msg),
                        _cancellationTokenSource.Token
                    );
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    AppendToAnswer("\n\n❌ 未能获取 Token，预约失败！请手动提供 Token 或确保微信小程序已打开。");
                    return;
                }

                AppendToAnswer("\n\n✅ Token 获取成功！");
                AppendToAnswer("\n\n🔍 正在获取可用班次...");

                // 获取班次列表
                var busList = await _busService.GetBusListAsync(
                    token,
                    request.StartStation,
                    request.EndStation,
                    request.Date,
                    msg => AppendToAnswer("\n   " + msg),
                    _cancellationTokenSource.Token
                );

                if (busList.Count == 0)
                {
                    AppendToAnswer("\n\n❌ 未找到符合条件的班次！");
                    return;
                }

                AppendToAnswer($"\n\n📋 找到 {busList.Count} 个班次：");
                foreach (var bus in busList)
                {
                    AppendToAnswer($"\n   - {bus.DepartureTime} (余票:{bus.Remain})");
                }

                // 筛选目标班次
                var targetBuses = busList;
                if (!string.IsNullOrWhiteSpace(request.Time))
                {
                    targetBuses = busList.Where(b => 
                        b.DepartureTime?.Contains(request.Time) == true
                    ).ToList();

                    if (targetBuses.Count == 0)
                    {
                        AppendToAnswer($"\n\n⚠️ 未找到 {request.Time} 的班次，将监控所有班次");
                        targetBuses = busList;
                    }
                    else
                    {
                        AppendToAnswer($"\n\n🎯 已锁定 {targetBuses.Count} 个目标班次");
                    }
                }

                var busIds = targetBuses.Select(b => b.Id.ToString()).ToList();
                AppendToAnswer("\n\n🚀 开始监控抢票...");

                // 开始轮询抢票
                await _busService.StartPollingAsync(
                    token,
                    busIds,
                    msg => AppendToAnswer("\n   " + msg),
                    _cancellationTokenSource.Token
                );

                AppendToAnswer("\n\n✅ 抢票流程结束！");
            }
            catch (OperationCanceledException)
            {
                AppendToAnswer("\n\n⚠️ 操作已取消");
            }
            catch (Exception ex)
            {
                AppendToAnswer($"\n\n❌ 发生错误: {ex.Message}");
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void AppendToAnswer(string text)
        {
            if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
            {
                _dispatcherQueue.TryEnqueue(() => 
                {
                    AiAnswer += text;
                    // 同时更新MessageHistory中的最后一条消息
                    if (MessageHistory.Count > 0 && MessageHistory.Last().Role == "assistant")
                    {
                        MessageHistory[MessageHistory.Count - 1].Content += text;
                    }
                });
            }
            else
            {
                AiAnswer += text;
                if (MessageHistory.Count > 0 && MessageHistory.Last().Role == "assistant")
                {
                    MessageHistory[MessageHistory.Count - 1].Content += text;
                }
            }
        }

        #region 会话管理命令

        /// <summary>
        /// 新建会话命令
        /// </summary>
        [RelayCommand]
        private void NewSession()
        {
            try
            {
                CurrentSession = _dbService.CreateSession("新对话");
                LoadSessionList();
                MessageHistory.Clear();
                AiAnswer = string.Empty;
                UserQuery = string.Empty;
                System.Diagnostics.Debug.WriteLine("[ChatViewModel] 新建会话成功");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 新建会话失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 切换会话命令
        /// </summary>
        [RelayCommand]
        private void SwitchSession(ChatSession session)
        {
            try
            {
                if (session == null) return;
                
                _dbService.SwitchSession(session.Id);
                CurrentSession = session;
                LoadCurrentSessionMessages();
                AiAnswer = string.Empty;
                
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 切换到会话: {session.Title}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 切换会话失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除会话命令
        /// </summary>
        [RelayCommand]
        private void DeleteSession(ChatSession session)
        {
            try
            {
                if (session == null) return;
                
                _dbService.DeleteSession(session.Id);
                LoadSessionList();

                // 如果删除的是当前会话，创建新会话
                if (CurrentSession?.Id == session.Id)
                {
                    NewSession();
                }
                
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 删除会话成功: {session.Title}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 删除会话失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清空所有会话命令
        /// </summary>
        [RelayCommand]
        private void ClearAllSessions()
        {
            try
            {
                _dbService.ClearAllSessions();
                NewSession();
                System.Diagnostics.Debug.WriteLine("[ChatViewModel] 清空所有会话成功");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 清空会话失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 展开/折叠历史面板命令
        /// </summary>
        [RelayCommand]
        private void ToggleHistoryPanel()
        {
            IsHistoryPanelOpen = !IsHistoryPanelOpen;
        }

        #endregion

        private class BusRequest
        {
            public string StartStation { get; set; } = "";
            public string EndStation { get; set; } = "";
            public string Date { get; set; } = "";
            public string Time { get; set; } = "";
            public string Token { get; set; } = "";
        }
    }
}
