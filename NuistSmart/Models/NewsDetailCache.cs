using LiteDB;
using System;

namespace NuistSmart.Models;

/// <summary>
/// 新闻详情缓存实体，用于本地存储新闻内容和AI摘要
/// </summary>
public class NewsDetailCache
{
    /// <summary>
    /// 新闻URL，作为主键
    /// </summary>
    [BsonId]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 新闻标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 发布日期
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// 新闻正文内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// AI生成的摘要（可为空）
    /// </summary>
    public string? AiSummary { get; set; }

    /// <summary>
    /// 缓存入库时间
    /// </summary>
    public DateTime CreateTime { get; set; }
}
