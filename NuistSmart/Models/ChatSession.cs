using LiteDB;
using System;

namespace NuistSmart.Models;

/// <summary>
/// 聊天会话实体，用于管理对话会话
/// </summary>
public class ChatSession
{
    /// <summary>
    /// 会话唯一标识（GUID）
    /// </summary>
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 会话标题（默认取第一条用户消息的前20字符）
    /// </summary>
    public string Title { get; set; } = "新对话";

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否是当前激活的会话（同一时间只能有一个激活会话）
    /// </summary>
    public bool IsActive { get; set; } = false;
}
