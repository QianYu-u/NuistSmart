using LiteDB;
using NuistSmart.Models;
using System;
using System.Diagnostics;
using System.IO;
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
