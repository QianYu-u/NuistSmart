using NuistSmart.Models;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NuistSmart.Services
{
    /// <summary>
    /// 校历服务，负责从教务系统抓取完整的 JSON 校历数据
    /// </summary>
    public class CalendarService
    {
        private const string CalendarApiUrl = "https://i.nuist.edu.cn/qljfwapp/sys/lwSchoolCalendar/schoolYearCalendar/getSchoolYearCalendarDetail.do";

        /// <summary>
        /// 获取指定学年的校历数据
        /// </summary>
        /// <param name="schoolYear">学年年份，例如 2025 代表 2025-2026 学年。如不传则使用服务器默认的当前学年</param>
        public async Task<CalendarApiResponse?> FetchCalendarAsync(int? schoolYear = null)
        {
            try
            {
                Debug.WriteLine($"[CalendarService] 开始抓取学年 {schoolYear?.ToString() ?? "当前默认"} 校历数据...");

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                string url = CalendarApiUrl;
                if (schoolYear.HasValue)
                {
                    url += $"?XN={schoolYear.Value}";
                }

                var json = await client.GetStringAsync(url);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var response = JsonSerializer.Deserialize<CalendarApiResponse>(json, options);

                if (response != null && response.Code == "0")
                {
                    response.LastUpdated = DateTime.Now;
                    Debug.WriteLine("[CalendarService] 校历数据解析成功");
                    return response;
                }
                else
                {
                    Debug.WriteLine($"[CalendarService] 校历接口返回空数据或失败 code={response?.Code}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CalendarService] 抓取校历失败: {ex.Message}");
                Debug.WriteLine($"[CalendarService] 堆栈跟踪: {ex.StackTrace}");
                return null;
            }
        }
    }
}
