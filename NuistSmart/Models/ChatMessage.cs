using LiteDB;
using System;

namespace NuistSmart.Models;

/// <summary>
/// 聊天消息实体，存储用户和AI的对话消息
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// 消息唯一标识（GUID）
    /// </summary>
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 所属会话ID（外键关联ChatSession）
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 消息角色：user(用户)、assistant(AI助手)、system(系统提示)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
