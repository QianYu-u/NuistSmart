using LiteDB;
using NuistSmart.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;

namespace NuistSmart.Services;

/// <summary>
/// LiteDB 数据库服务，提供新闻缓存的增删改查功能
/// </summary>
public class DbService
{
    private LiteDatabase? _db;
    private const string CollectionName = "news_cache";
    private const string CollectionName_Users = "users";
    private const string CollectionName_Contacts = "contacts";
    private const string CollectionName_ChatSessions = "chat_sessions";
    private const string CollectionName_ChatMessages = "chat_messages";
    private const string DbFileName = "nuistsmart.db";

    /// <summary>
    /// 初始化数据库连接
    /// </summary>
    public void Init()
    {
        try
        {
            // 获取应用本地存储文件夹路径
            var localFolder = ApplicationData.Current.LocalFolder.Path;
            var dbPath = Path.Combine(localFolder, DbFileName);

            Debug.WriteLine($"[DbService] 初始化数据库，路径: {dbPath}");

            // 创建或打开数据库
            _db = new LiteDatabase(dbPath);

            // 为聊天相关集合创建索引以优化查询性能
            var sessionCol = _db.GetCollection<ChatSession>(CollectionName_ChatSessions);
            sessionCol.EnsureIndex(x => x.CreateTime);
            sessionCol.EnsureIndex(x => x.UpdateTime);
            sessionCol.EnsureIndex(x => x.IsActive);

            var messageCol = _db.GetCollection<ChatMessage>(CollectionName_ChatMessages);
            messageCol.EnsureIndex(x => x.SessionId);
            messageCol.EnsureIndex(x => x.Timestamp);

            Debug.WriteLine("[DbService] 数据库初始化成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 数据库初始化失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 获取当前保存的用户信息（获取最新登录的一个用户）
    /// </summary>
    public User? GetCurrentUserInfo()
    {
        try
        {
            if (_db == null) return null;
            var collection = _db.GetCollection<User>(CollectionName_Users);
            // 这里简单返回集合里的第一个用户数据或者最后一个插入的数据
            return collection.FindAll().LastOrDefault();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 获取用户信息失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 保存或更新用户信息
    /// </summary>
    public bool SaveUserInfo(User user)
    {
        try
        {
            if (_db == null) return false;
            var collection = _db.GetCollection<User>(CollectionName_Users);
            collection.Upsert(user);
            Debug.WriteLine($"[DbService] 保存用户信息成功: {user.Name} ({user.StudentId})");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 保存用户信息失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 批量保存或更新通讯录黄页信息
    /// </summary>
    public void SaveContacts(List<ContactCache> contacts)
    {
        try
        {
            if (_db == null) return;
            var collection = _db.GetCollection<ContactCache>(CollectionName_Contacts);
            // 每次拉取都全量覆盖或更新，因为联系人不多，可以先清空再插入，或者全量 Upsert
            collection.DeleteAll(); // 简单起见，全部清空重新写入
            collection.InsertBulk(contacts);
            Debug.WriteLine($"[DbService] 成功保存 {contacts.Count} 条黄页信息");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 保存黄页信息失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取所有通讯录黄页信息
    /// </summary>
    public List<ContactCache> GetAllContacts()
    {
        try
        {
            if (_db == null) return new List<ContactCache>();
            var collection = _db.GetCollection<ContactCache>(CollectionName_Contacts);
            return collection.FindAll().ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 获取黄页信息失败: {ex.Message}");
            return new List<ContactCache>();
        }
    }

    /// <summary>
    /// 根据 URL 查询缓存的新闻
    /// </summary>
    /// <param name="url">新闻 URL</param>
    /// <returns>新闻详情，如果不存在则返回 null</returns>
    public NewsDetailCache? GetNews(string url)
    {
        try
        {
            if (_db == null)
            {
                Debug.WriteLine("[DbService] 数据库未初始化，请先调用 Init()");
                return null;
            }

            var collection = _db.GetCollection<NewsDetailCache>(CollectionName);
            var news = collection.FindById(url);

            if (news != null)
            {
                Debug.WriteLine($"[DbService] 成功查询到缓存: {news.Title}");
            }
            else
            {
                Debug.WriteLine($"[DbService] 未找到缓存: {url}");
            }

            return news;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 查询新闻失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 保存或更新新闻缓存
    /// </summary>
    /// <param name="news">新闻详情对象</param>
    /// <returns>是否保存成功</returns>
    public bool SaveNews(NewsDetailCache news)
    {
        try
        {
            if (_db == null)
            {
                Debug.WriteLine("[DbService] 数据库未初始化，请先调用 Init()");
                return false;
            }

            var collection = _db.GetCollection<NewsDetailCache>(CollectionName);
            
            // 使用 Upsert 方法：如果存在则更新，不存在则插入
            var result = collection.Upsert(news);

            Debug.WriteLine($"[DbService] 保存新闻成功: {news.Title} (URL: {news.Url})");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 保存新闻失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 单独更新某条新闻的 AI 摘要
    /// </summary>
    /// <param name="url">新闻 URL</param>
    /// <param name="summary">AI 摘要内容</param>
    /// <returns>是否更新成功</returns>
    public bool UpdateSummary(string url, string summary)
    {
        try
        {
            if (_db == null)
            {
                Debug.WriteLine("[DbService] 数据库未初始化，请先调用 Init()");
                return false;
            }

            var collection = _db.GetCollection<NewsDetailCache>(CollectionName);
            var news = collection.FindById(url);

            if (news == null)
            {
                Debug.WriteLine($"[DbService] 更新摘要失败：未找到新闻 {url}");
                return false;
            }

            // 更新摘要字段
            news.AiSummary = summary;
            collection.Update(news);

            Debug.WriteLine($"[DbService] 更新摘要成功: {news.Title}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 更新摘要失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 批量保存或更新假期数据
    /// </summary>
    /// <param name="holidays">假期列表</param>
    public void SaveHolidays(List<HolidayItem> holidays)
    {
        try
        {
            if (_db == null)
            {
                Debug.WriteLine("[DbService] 数据库未初始化，请先调用 Init()");
                return;
            }

            var col = _db.GetCollection<HolidayItem>("holidays");
            
            // 批量插入或更新 (Upsert)
            col.Upsert(holidays);
            
            Debug.WriteLine($"[DbService] 成功存入 {holidays.Count} 条假期数据");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 保存假期失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 查询指定年月的所有假期
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份</param>
    /// <returns>假期列表</returns>
    public List<HolidayItem> GetHolidaysByMonth(int year, int month)
    {
        try
        {
            if (_db == null)
            {
                Debug.WriteLine("[DbService] 数据库未初始化，请先调用 Init()");
                return new List<HolidayItem>();
            }

            var col = _db.GetCollection<HolidayItem>("holidays");

            // 查询指定月份的假期
            var results = col.Find(h => h.Date.Year == year && h.Date.Month == month);
            return new List<HolidayItem>(results);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 查询假期失败: {ex.Message}");
            return new List<HolidayItem>();
        }
    }

    /// <summary>
    /// 查询指定日期的假期信息
    /// </summary>
    /// <param name="date">日期</param>
    /// <returns>假期信息，如果不是假期则返回 null</returns>
    public HolidayItem? GetHolidayByDate(DateTime date)
    {
        try
        {
            if (_db == null)
            {
                Debug.WriteLine("[DbService] 数据库未初始化，请先调用 Init()");
                return null;
            }

            var col = _db.GetCollection<HolidayItem>("holidays");
            var dateKey = date.ToString("yyyy-MM-dd");
            
            return col.FindById(dateKey);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 查询假期失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取最近 N 天内的公告缓存列表
    /// </summary>
    /// <param name="days">天数</param>
    /// <returns>公告列表</returns>
    public List<NewsDetailCache> GetRecentNews(int days = 30)
    {
        try
        {
            if (_db == null)
            {
                Debug.WriteLine("[DbService] 数据库未初始化，请先调用 Init()");
                return new List<NewsDetailCache>();
            }

            var collection = _db.GetCollection<NewsDetailCache>(CollectionName);
            var all = collection.FindAll().ToList();

            // 尝试按日期字符串筛选最近 N 天
            var cutoff = DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd");
            var recent = all
                .Where(n => string.Compare(n.Date, cutoff, StringComparison.Ordinal) >= 0)
                .OrderByDescending(n => n.Date)
                .ToList();

            // 如果按日期筛选结果为空（可能日期格式不一致），返回全部数据
            if (recent.Count == 0)
            {
                Debug.WriteLine($"[DbService] 按日期筛选无结果，返回全部 {all.Count} 条");
                return all.OrderByDescending(n => n.Date).ToList();
            }

            Debug.WriteLine($"[DbService] 查询到最近 {days} 天公告 {recent.Count} 条");
            return recent;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 查询近期公告失败: {ex.Message}");
            return new List<NewsDetailCache>();
        }
    }

    /// <summary>
    /// 保存整学年的校历数据
    /// </summary>
    public void SaveCalendarData(CalendarApiResponse data)
    {
        try
        {
            if (_db == null) return;
            var col = _db.GetCollection<CalendarApiResponse>("calendars");
            data.Id = "XN_" + data.SchoolYearData?.XN;
            col.Upsert(data);
            Debug.WriteLine($"[DbService] 成功存入 {data.SchoolYearData?.XN} 学年校历数据");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 保存校历失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取某学年的校历数据
    /// </summary>
    public CalendarApiResponse? GetCalendarData(string xn)
    {
        try
        {
            if (_db == null) return null;
            var col = _db.GetCollection<CalendarApiResponse>("calendars");
            return col.FindById("XN_" + xn);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 查询校历失败: {ex.Message}");
            return null;
        }
    }

    #region 聊天会话管理

    /// <summary>
    /// 创建新的聊天会话
    /// </summary>
    /// <param name="title">会话标题（可选）</param>
    /// <returns>新创建的会话</returns>
    public ChatSession CreateSession(string? title = null)
    {
        try
        {
            if (_db == null)
            {
                Debug.WriteLine("[DbService] 数据库未初始化");
                throw new InvalidOperationException("数据库未初始化");
            }

            var collection = _db.GetCollection<ChatSession>(CollectionName_ChatSessions);

            // 将之前的激活会话设为非激活
            var activeSession = collection.FindOne(s => s.IsActive);
            if (activeSession != null)
            {
                activeSession.IsActive = false;
                collection.Update(activeSession);
            }

            // 创建新会话
            var newSession = new ChatSession
            {
                Id = Guid.NewGuid().ToString(),
                Title = title ?? "新对话",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                IsActive = true
            };

            collection.Insert(newSession);
            Debug.WriteLine($"[DbService] 创建新会话成功: {newSession.Id}");
            return newSession;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 创建会话失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 获取当前激活的会话
    /// </summary>
    /// <returns>激活的会话，如果没有则返回null</returns>
    public ChatSession? GetActiveSession()
    {
        try
        {
            if (_db == null) return null;
            var collection = _db.GetCollection<ChatSession>(CollectionName_ChatSessions);
            return collection.FindOne(s => s.IsActive);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 获取激活会话失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取所有会话列表，按更新时间倒序排列
    /// </summary>
    /// <returns>会话列表</returns>
    public List<ChatSession> GetAllSessions()
    {
        try
        {
            if (_db == null) return new List<ChatSession>();
            var collection = _db.GetCollection<ChatSession>(CollectionName_ChatSessions);
            return collection.FindAll()
                .OrderByDescending(s => s.UpdateTime)
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 获取会话列表失败: {ex.Message}");
            return new List<ChatSession>();
        }
    }

    /// <summary>
    /// 切换到指定会话
    /// </summary>
    /// <param name="sessionId">要切换到的会话ID</param>
    /// <returns>是否切换成功</returns>
    public bool SwitchSession(string sessionId)
    {
        try
        {
            if (_db == null) return false;
            var collection = _db.GetCollection<ChatSession>(CollectionName_ChatSessions);

            // 将所有会话设为非激活
            var allSessions = collection.FindAll().ToList();
            foreach (var session in allSessions)
            {
                session.IsActive = (session.Id == sessionId);
                collection.Update(session);
            }

            Debug.WriteLine($"[DbService] 切换到会话: {sessionId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 切换会话失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 删除指定会话及其所有消息
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>是否删除成功</returns>
    public bool DeleteSession(string sessionId)
    {
        try
        {
            if (_db == null) return false;

            // 删除会话
            var sessionCol = _db.GetCollection<ChatSession>(CollectionName_ChatSessions);
            sessionCol.Delete(sessionId);

            // 删除该会话的所有消息
            var messageCol = _db.GetCollection<ChatMessage>(CollectionName_ChatMessages);
            messageCol.DeleteMany(m => m.SessionId == sessionId);

            Debug.WriteLine($"[DbService] 删除会话成功: {sessionId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 删除会话失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 清空所有会话和消息
    /// </summary>
    /// <returns>是否清空成功</returns>
    public bool ClearAllSessions()
    {
        try
        {
            if (_db == null) return false;

            var sessionCol = _db.GetCollection<ChatSession>(CollectionName_ChatSessions);
            var messageCol = _db.GetCollection<ChatMessage>(CollectionName_ChatMessages);

            sessionCol.DeleteAll();
            messageCol.DeleteAll();

            Debug.WriteLine("[DbService] 清空所有会话成功");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 清空会话失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 更新会话标题
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="title">新标题</param>
    /// <returns>是否更新成功</returns>
    public bool UpdateSessionTitle(string sessionId, string title)
    {
        try
        {
            if (_db == null) return false;
            var collection = _db.GetCollection<ChatSession>(CollectionName_ChatSessions);
            var session = collection.FindById(sessionId);
            if (session == null) return false;

            session.Title = title;
            session.UpdateTime = DateTime.Now;
            collection.Update(session);

            Debug.WriteLine($"[DbService] 更新会话标题成功: {title}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 更新会话标题失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 更新会话的最后更新时间
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    public void TouchSession(string sessionId)
    {
        try
        {
            if (_db == null) return;
            var collection = _db.GetCollection<ChatSession>(CollectionName_ChatSessions);
            var session = collection.FindById(sessionId);
            if (session != null)
            {
                session.UpdateTime = DateTime.Now;
                collection.Update(session);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 更新会话时间失败: {ex.Message}");
        }
    }

    #endregion

    #region 聊天消息管理

    /// <summary>
    /// 保存消息到数据库
    /// </summary>
    /// <param name="message">消息对象</param>
    /// <returns>是否保存成功</returns>
    public bool SaveMessage(ChatMessage message)
    {
        try
        {
            if (_db == null) return false;
            var collection = _db.GetCollection<ChatMessage>(CollectionName_ChatMessages);
            collection.Insert(message);
            
            // 更新会话的最后更新时间
            TouchSession(message.SessionId);
            
            Debug.WriteLine($"[DbService] 保存消息成功: {message.Role} - {message.Content.Substring(0, Math.Min(20, message.Content.Length))}...");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 保存消息失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取指定会话的所有消息，按时间顺序排列
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>消息列表</returns>
    public List<ChatMessage> GetSessionMessages(string sessionId)
    {
        try
        {
            if (_db == null) return new List<ChatMessage>();
            var collection = _db.GetCollection<ChatMessage>(CollectionName_ChatMessages);
            return collection.Find(m => m.SessionId == sessionId)
                .OrderBy(m => m.Timestamp)
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 获取会话消息失败: {ex.Message}");
            return new List<ChatMessage>();
        }
    }

    /// <summary>
    /// 获取指定会话最近N轮对话（N轮 = N个user消息 + N个assistant消息）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="rounds">对话轮数，默认5轮</param>
    /// <returns>最近的消息列表（按时间正序）</returns>
    public List<ChatMessage> GetRecentMessages(string sessionId, int rounds = 5)
    {
        try
        {
            if (_db == null) return new List<ChatMessage>();
            var collection = _db.GetCollection<ChatMessage>(CollectionName_ChatMessages);
            
            // 获取所有消息并按时间倒序排列
            var allMessages = collection.Find(m => m.SessionId == sessionId)
                .OrderByDescending(m => m.Timestamp)
                .ToList();

            // 提取最近N轮对话（排除system消息）
            var recentMessages = new List<ChatMessage>();
            int userCount = 0, assistantCount = 0;

            foreach (var msg in allMessages)
            {
                if (msg.Role == "user" && userCount < rounds)
                {
                    recentMessages.Add(msg);
                    userCount++;
                }
                else if (msg.Role == "assistant" && assistantCount < rounds)
                {
                    recentMessages.Add(msg);
                    assistantCount++;
                }

                // 如果已经收集到N轮对话，退出
                if (userCount >= rounds && assistantCount >= rounds)
                    break;
            }

            // 按时间正序返回（oldest first）
            recentMessages.Reverse();
            Debug.WriteLine($"[DbService] 获取最近 {rounds} 轮消息: {recentMessages.Count} 条");
            return recentMessages;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 获取最近消息失败: {ex.Message}");
            return new List<ChatMessage>();
        }
    }

    #endregion

    /// <summary>
    /// 释放数据库资源
    /// </summary>
    public void Dispose()
    {
        try
        {
            _db?.Dispose();
            Debug.WriteLine("[DbService] 数据库连接已释放");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 释放数据库连接失败: {ex.Message}");
        }
    }
}
