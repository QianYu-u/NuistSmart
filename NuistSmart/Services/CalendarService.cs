using HtmlAgilityPack;
using NuistSmart.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NuistSmart.Services
{
    /// <summary>
    /// 校历服务，负责从教务系统抓取校历假期数据
    /// </summary>
    public class CalendarService
    {
        // 校历 URL
        private const string CalendarUrl = "https://i.nuist.edu.cn/qljfwapp/sys/lwSchoolCalendar/schoolYearCalendar.do";

        /// <summary>
        /// 异步获取校历假期数据
        /// </summary>
        /// <returns>假期列表</returns>
        public async Task<List<HolidayItem>> FetchHolidaysAsync()
        {
            var holidays = new List<HolidayItem>();

            try
            {
                Debug.WriteLine("[CalendarService] 开始抓取校历数据...");

                // 1. 获取网页 HTML
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                var htmlBytes = await client.GetByteArrayAsync(CalendarUrl);
                
                // 尝试 UTF-8 编码，如果乱码则改用 GB2312
                var html = Encoding.UTF8.GetString(htmlBytes);
                
                // 如果包含乱码特征（如问号），尝试 GB2312
                if (html.Contains("�"))
                {
                    Debug.WriteLine("[CalendarService] 检测到乱码，尝试 GB2312 编码");
                    html = Encoding.GetEncoding("GB2312").GetString(htmlBytes);
                }

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                Debug.WriteLine("[CalendarService] HTML 加载成功，开始解析...");

                // 2. 解析逻辑
                // 定位主要的日历表格
                var rows = doc.DocumentNode.SelectNodes("//tr");
                if (rows == null || rows.Count == 0)
                {
                    Debug.WriteLine("[CalendarService] 未找到表格行");
                    return holidays;
                }

                Debug.WriteLine($"[CalendarService] 找到 {rows.Count} 个表格行");

                // 初始年份（简化逻辑：9月开始为当前学年第一年，1月开始为第二年）
                int currentYear = DateTime.Now.Year;
                int defaultStartYear = DateTime.Now.Month >= 9 ? DateTime.Now.Year : DateTime.Now.Year - 1;
                
                currentYear = defaultStartYear;

                foreach (var row in rows)
                {
                    var cells = row.SelectNodes("td");
                    if (cells == null || cells.Count < 9) continue; // 忽略非日期行

                    // 第1列：月份 (例如 "9月")
                    var monthText = cells[0].InnerText.Trim();
                    // 第2列：周次 或 假期名 (例如 "1", "寒假")
                    var weekText = cells[1].InnerText.Trim();

                    // 提取月份数字
                    var monthMatch = Regex.Match(monthText, @"\d+");
                    if (!monthMatch.Success) continue;

                    int month = int.Parse(monthMatch.Value);

                    // 年份推断逻辑：如果从12月变到1月，年份+1
                    // 简化处理：9-12月用第一年，1-8月用第二年
                    if (month >= 1 && month <= 8)
                    {
                        currentYear = defaultStartYear + 1;
                    }
                    else if (month >= 9 && month <= 12)
                    {
                        currentYear = defaultStartYear;
                    }

                    // 判断是否为整周假期
                    bool isWholeWeekHoliday = weekText.Contains("寒假") || weekText.Contains("暑假");
                    string holidayName = isWholeWeekHoliday ? weekText : "";

                    // 遍历周一到周日 (第3列 到 第9列)
                    for (int i = 2; i <= 8; i++)
                    {
                        var cellText = cells[i].InnerText.Trim();
                        
                        // 单元格内容通常是 "1", "2" 或 "1 国庆节"
                        // 提取数字作为日期
                        var dayString = new string(cellText.Where(char.IsDigit).ToArray());
                        
                        if (int.TryParse(dayString, out int day))
                        {
                            try 
                            {
                                var date = new DateTime(currentYear, month, day);
                                string specificHoliday = "";

                                // 检查单元格内是否有特殊节日文字
                                // 去掉数字和空白字符后，检查是否包含"节"、"假"等关键字
                                string textWithoutDigits = Regex.Replace(cellText, @"[\d\s]", "");
                                
                                // 过滤掉农历日期（如"初一"、"廿一"等）
                                if (!string.IsNullOrEmpty(textWithoutDigits) &&
                                    (textWithoutDigits.Contains("节") || 
                                     textWithoutDigits.Contains("假") || 
                                     textWithoutDigits.Contains("年")) &&
                                    !textWithoutDigits.Contains("初") &&
                                    !textWithoutDigits.Contains("廿") &&
                                    !textWithoutDigits.Contains("卅"))
                                {
                                    specificHoliday = textWithoutDigits; // 例如 "国庆节"
                                }

                                // 如果是整周假期 或 这一天有特殊节日，就存入
                                if (isWholeWeekHoliday || !string.IsNullOrEmpty(specificHoliday))
                                {
                                    holidays.Add(new HolidayItem
                                    {
                                        Date = date,
                                        Name = !string.IsNullOrEmpty(specificHoliday) ? specificHoliday : holidayName,
                                        IsLongVacation = isWholeWeekHoliday
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[CalendarService] 无效日期: {currentYear}-{month}-{day}, {ex.Message}");
                            }
                        }
                    }
                }

                Debug.WriteLine($"[CalendarService] 解析完成，共找到 {holidays.Count} 个假期");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CalendarService] 抓取校历失败: {ex.Message}");
                Debug.WriteLine($"[CalendarService] 堆栈跟踪: {ex.StackTrace}");
            }

            return holidays;
        }
    }
}
