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

            Debug.WriteLine("[DbService] 数据库初始化成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DbService] 数据库初始化失败: {ex.Message}");
            throw;
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
