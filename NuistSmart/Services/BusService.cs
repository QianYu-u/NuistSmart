using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NuistSmart.Models;

namespace NuistSmart.Services
{
    public class BusService
    {
        private readonly HttpClient _httpClient;
        private const string API_LIST_URL = "https://api.nuist.899988.xyz/seatReserve/getRunList";
        private const string API_BOOK_URL = "https://api.nuist.899988.xyz/seatReserve/reserve";

        public BusService()
        {
            _httpClient = new HttpClient();
        }

        private HttpRequestMessage CreateRequest(string url, string token, object? payload = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            // 强制使用 HTTP/1.1，避免现代 .NET 默认的 HTTP/2 协议被原服务器网关切断导致 "Response ended prematurely"
            request.Version = new Version(1, 1);

            request.Headers.TryAddWithoutValidation("auth", token);
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 MicroMessenger/7.0.20.1781(0x6700143B) NetType/WIFI MiniProgramEnv/Windows WindowsWechat/WMPF WindowsWechat(0x63090a13) XWEB/11253");
            request.Headers.TryAddWithoutValidation("Referer", "https://service.nuist.edu.cn/");
            request.Headers.TryAddWithoutValidation("Accept", "*/*");

            // 使用 StringContent 替代 JsonContent，保证严格产出 Content-Length=2，避免触发 Chunked(分块)传输导致老旧服务器断开连接
            string jsonString = payload != null ? JsonSerializer.Serialize(payload) : "{}";
            request.Content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");

            return request;
        }

        public async Task<List<BusRunItem>> GetBusListAsync(
            string token,
            string targetStart,
            string targetEnd,
            string targetDate,
            Action<string> logCallback,
            CancellationToken cancellationToken)
        {
            try
            {
                logCallback($"正在获取班次列表: {targetStart} -> {targetEnd}, 日期: {targetDate}");
                
                var req = CreateRequest(API_LIST_URL, token);
                var response = await _httpClient.SendAsync(req, cancellationToken);
                response.EnsureSuccessStatusCode();

                var strResult = await response.Content.ReadAsStringAsync();
                logCallback($"API 响应: {strResult.Substring(0, Math.Min(200, strResult.Length))}...");
                
                var resJson = JsonSerializer.Deserialize<BusResponse<List<BusRunItem>>>(strResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (resJson != null && resJson.Code == 0 && resJson.Data != null)
                {
                    var filteredBuses = resJson.Data
                        .Where(bus => bus.Start == targetStart && bus.End == targetEnd)
                        .Where(bus =>
                        {
                            if (string.IsNullOrWhiteSpace(targetDate)) return true;
                            if (string.IsNullOrWhiteSpace(bus.DepartureTime)) return false;
                            string[] parts = bus.DepartureTime.Split(' ');
                            return parts.Length > 0 && parts[0] == targetDate;
                        })
                        .ToList();

                    logCallback($"找到 {filteredBuses.Count} 个符合条件的班次");
                    return filteredBuses;
                }
                else
                {
                    logCallback($"获取班次失败: {resJson?.Msg ?? "未知错误"}");
                    return new List<BusRunItem>();
                }
            }
            catch (Exception ex)
            {
                string detailedError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                logCallback($"获取班次异常: {detailedError}");
                return new List<BusRunItem>();
            }
        }

        public async Task StartPollingAsync(
            string token,
            List<string> targetBusIds,
            Action<string> logCallback,
            CancellationToken cancellationToken)
        {
            logCallback($"=== 抢票轮询启动 | 监控 {targetBusIds.Count} 个班次 ===");

            var random = new Random();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    bool success = await CheckTicketAsync(token, targetBusIds, logCallback, cancellationToken);
                    if (success)
                    {
                        break;
                    }

                    // 避免刚轮询完就开始下一次，加入随机 5-20 秒间隔发呆
                    int sleepTimerSeconds = random.Next(5, 21);
                    logCallback($"  ... 抢票机制随机休息 {sleepTimerSeconds} 秒，防止风控异常封号 ...");
                    await Task.Delay(sleepTimerSeconds * 1000, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    logCallback("轮询任务已取消中止。");
                    break;
                }
                catch (Exception ex)
                {
                    string detailedError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    logCallback($"监控轮询通信异常: {detailedError}");
                    await Task.Delay(random.Next(5, 21) * 1000, cancellationToken);
                }
            }
        }

        private async Task<bool> CheckTicketAsync(
            string token,
            List<string> targetBusIds,
            Action<string> logCallback,
            CancellationToken cancellationToken)
        {
            var req = CreateRequest(API_LIST_URL, token);
            var response = await _httpClient.SendAsync(req, cancellationToken);
            response.EnsureSuccessStatusCode();

            var strResult = await response.Content.ReadAsStringAsync();
            var resJson = JsonSerializer.Deserialize<BusResponse<List<BusRunItem>>>(strResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (resJson != null && resJson.Code == 0 && resJson.Data != null)
            {
                logCallback($"[{DateTime.Now:HH:mm:ss}] 扫描中... 共找到 {resJson.Data.Count} 个班次");

                foreach (var bus in resJson.Data)
                {
                    // 只检查目标班次列表中的班车
                    if (!targetBusIds.Contains(bus.Id.ToString())) continue;

                    string fullTime = bus.DepartureTime ?? "";
                    logCallback($"  > 监控班次: {fullTime} | 余票: {bus.Remain}");

                    if (bus.Remain > 0)
                    {
                        logCallback("  ★ 发现目标余票！准备触发自动化秒杀！ ★");
                        return await BookTicketAsync(token, bus.Id.ToString(), fullTime, logCallback, cancellationToken);
                    }
                }
            }
            else
            {
                logCallback($"服务器查票接口有误: {resJson?.Msg}");
            }

            return false;
        }

        private async Task<bool> BookTicketAsync(string token, string runId, string busInfo, Action<string> logCallback, CancellationToken cancellationToken)
        {
            try
            {
                logCallback($"  >>> [自动下单] 现已锁定并发起请求: {busInfo} ...");
                var req = CreateRequest(API_BOOK_URL, token, new { run = runId });
                var response = await _httpClient.SendAsync(req, cancellationToken);
                var strResult = await response.Content.ReadAsStringAsync();

                logCallback($"  >>> [服务器反馈回显] {strResult}");

                using var doc = JsonDocument.Parse(strResult);
                int code = doc.RootElement.TryGetProperty("code", out var codeEle) ? codeEle.GetInt32() : -1;
                string msg = doc.RootElement.TryGetProperty("msg", out var msgEle) ? msgEle.GetString() ?? "" : "";

                if (code == 0)
                {
                    logCallback("  ★ 恭喜！全自动抢票成功！！您已占座成功！！ ★");
                    return true;
                }
                else if (msg.Contains("已预约") || msg.Contains("重复"))
                {
                    logCallback("  ★ 检测到您已有该班次的票，或者存在未支付记录，无需重复抢票！跑路脚本圆满完成！ ★");
                    return true;
                }
                else
                {
                    logCallback($"  X 悲报...抢票失败错失机会: {msg}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                string detailedError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                logCallback($"  X 严重错误, 跨域下单引发底层异常捕获: {detailedError}");
                return false;
            }
        }
    }
}
