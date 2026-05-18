using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NuistSmart.Services;

/// <summary>
/// AI 服务，调用 Gemini (OpenAI 兼容格式) 接口
/// </summary>
public class AiService
{
    // ============================================================
    // 👇 在这里填入你的真实配置
    // ============================================================
    private const string ApiKey = " 233 ";
    private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
    private const string ModelName = "gemini-flash-latest";
    // ============================================================

    private readonly HttpClient _httpClient;

    private const string SystemPrompt =
        "你是南京信息工程大学的智能助手，可以帮助学生和教职工处理以下任务：\n\n" +
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
        "用户会给你一份带有公告标题、日期以及各个部门电话和办公地点的黄页通讯录列表作为上下文。" +
        "请严格基于你接收到的联系方式和公告列表来回答，如果你在黄页里找到了对应的部门电话，请直接回复部门名称、电话和地点。" +
        "如果无法从列表中找到，请如实告知。\n\n" +
        "回答得体、亲切，使用中文。";

    public AiService()
    {
        // 允许 HttpClient 自动使用系统代理（如 Clash/V2Ray 等）
        var handler = new HttpClientHandler
        {
            UseProxy = true,
            Proxy = System.Net.WebRequest.DefaultWebProxy
        };
        
        // 如果代理需要凭据，使用默认凭据
        if (handler.Proxy != null)
        {
            handler.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
        }

        _httpClient = new HttpClient(handler)
        {
            // 延长超时时间到 60 秒，避免某些代理节点响应慢导致 TaskCanceledException
            Timeout = TimeSpan.FromSeconds(60)
        };
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
    }

    /// <summary>
    /// 基于公告上下文回答用户问题
    /// </summary>
    /// <param name="userQuery">用户自然语言问题</param>
    /// <param name="context">公告标题+日期拼接的上下文</param>
    /// <returns>AI 回答文本</returns>
    public async Task<string> GetAnswerAsync(string userQuery, string context)
    {
        try
        {
            var userMessage = $"以下是近期校园公告列表：\n\n{context}\n\n用户问题：{userQuery}";

            var requestBody = new
            {
                model = ModelName,
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = userMessage }
                },
                max_tokens = 8192,
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Debug.WriteLine($"[AiService] 发送请求，问题: {userQuery}");

            var response = await _httpClient.PostAsync(Endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[AiService] API 请求失败: {response.StatusCode} - {responseBody}");
                return $"AI 服务请求失败 ({response.StatusCode})，请检查 API Key 和网络连接。";
            }

            // 解析 OpenAI 兼容格式的响应
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                // 记录 finish_reason 用于调试截断问题
                if (firstChoice.TryGetProperty("finish_reason", out var finishReason))
                {
                    Debug.WriteLine($"[AiService] finish_reason: {finishReason.GetString()}");
                }
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var answerContent))
                {
                    var answer = answerContent.GetString() ?? "未获取到回答内容";
                    Debug.WriteLine($"[AiService] 获取回答成功，长度: {answer.Length}");
                    return answer;
                }
            }

            Debug.WriteLine($"[AiService] 响应格式异常: {responseBody}");
            return "AI 返回了意外的响应格式，请重试。";
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine("[AiService] 请求超时");
            return "请求超时，请检查网络连接后重试。";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AiService] 异常: {ex.Message}");
            return $"发生错误: {ex.Message}";
        }
    }

    /// <summary>
    /// 支持上下文对话的AI请求（新方法）
    /// </summary>
    /// <param name="messages">完整的消息历史数组，包含system、user和assistant消息</param>
    /// <returns>AI 回答文本</returns>
    public async Task<string> GetAnswerWithContextAsync(object[] messages)
    {
        try
        {
            var requestBody = new
            {
                model = ModelName,
                messages = messages,
                max_tokens = 8192,
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Debug.WriteLine($"[AiService] 发送上下文对话请求，消息数: {messages.Length}");

            var response = await _httpClient.PostAsync(Endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[AiService] API 请求失败: {response.StatusCode} - {responseBody}");
                return $"AI 服务请求失败 ({response.StatusCode})，请检查 API Key 和网络连接。";
            }

            // 解析 OpenAI 兼容格式的响应
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                // 记录 finish_reason 用于调试截断问题
                if (firstChoice.TryGetProperty("finish_reason", out var finishReason))
                {
                    Debug.WriteLine($"[AiService] finish_reason: {finishReason.GetString()}");
                }
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var answerContent))
                {
                    var answer = answerContent.GetString() ?? "未获取到回答内容";
                    Debug.WriteLine($"[AiService] 获取回答成功，长度: {answer.Length}");
                    return answer;
                }
            }

            Debug.WriteLine($"[AiService] 响应格式异常: {responseBody}");
            return "AI 返回了意外的响应格式，请重试。";
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine("[AiService] 请求超时");
            return "请求超时，请检查网络连接后重试。";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AiService] 异常: {ex.Message}");
            return $"发生错误: {ex.Message}";
        }
    }
}
