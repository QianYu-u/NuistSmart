using HtmlAgilityPack;
using NuistSmart.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace NuistSmart.Services
{
    public class NewsService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://bulletin.nuist.edu.cn";

        // 1. 【新增】分类映射表 (参考了你的 rssbulletin.txt)
        private readonly Dictionary<string, string> _categoryMap = new()
        {
            { "全部", "index.htm" },
            { "文件公告", "wjgg.htm" },
            { "学术报告", "xsbg.htm" },
            { "招标信息", "zbxx.htm" },
            { "会议通知", "hytz2.htm" },
            { "教学考试", "jxks.htm" },
            { "党政事务", "dzsw.htm" },
            { "组织人事", "zzrs.htm" },
            { "科研信息", "kyxx.htm" },
            { "招生就业", "zsjy.htm" },
            { "创新创业", "cxcy.htm" },
            { "校园活动", "xyhd.htm" },
            { "学院动态", "xydt.htm" },
            { "专题讲座", "ztjz.htm" }
        };

        public NewsService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                UseCookies = false,
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        }

        // 2. 【修改】方法增加 category 参数
        public async Task<List<NewsItem>> GetNewsListAsync(string category = "全部")
        {
            try
            {
                // 根据分类名获取对应的文件名，默认用 index.htm
                string fileName = _categoryMap.ContainsKey(category) ? _categoryMap[category] : "index.htm";
                string url = $"{BaseUrl}/{fileName}";

                var responseBytes = await _httpClient.GetByteArrayAsync(url);

                string htmlContent = Encoding.UTF8.GetString(responseBytes);
                if (htmlContent.Count(c => c == '\uFFFD') > 50)
                {
                    htmlContent = Encoding.GetEncoding("GBK").GetString(responseBytes);
                }

                int doctypeIndex = htmlContent.IndexOf("DOCTYPE");
                if (doctypeIndex > 0)
                {
                    htmlContent = "<" + htmlContent.Substring(doctypeIndex);
                }

                var newsList = new List<NewsItem>();
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                var nodes = doc.DocumentNode.SelectNodes("//ul[contains(@class,'news_list')]//li")
                         ?? doc.DocumentNode.SelectNodes("//li");

                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        var item = ParseNode(node);
                        if (item != null) newsList.Add(item);
                    }
                }

                if (newsList.Count > 0)
                {
                    return newsList
                        .GroupBy(x => x.Title)
                        .Select(g => g.First())
                        .OrderByDescending(n => n.Date)
                        .Take(20)
                        .ToList();
                }

                return GetFallbackNews("该分类下暂无数据");
            }
            catch (Exception ex)
            {
                return GetFallbackNews($"错误: {ex.Message}");
            }
        }

        private NewsItem? ParseNode(HtmlNode node)
        {
            try
            {
                var titleNode = node.SelectSingleNode(".//span[contains(@class,'btt')]//a");
                if (titleNode == null) return null;

                string title = titleNode.GetAttributeValue("title", "").Trim();
                if (string.IsNullOrEmpty(title)) title = titleNode.InnerText.Trim();
                if (title.StartsWith("[") && title.EndsWith("]")) return null;

                string href = titleNode.GetAttributeValue("href", "");
                if (string.IsNullOrEmpty(href)) return null;
                if (!href.StartsWith("http")) href = BaseUrl + (href.StartsWith("/") ? "" : "/") + href;

                string date = "";
                var dateNode = node.SelectSingleNode(".//span[contains(@class,'news_date')]")
                            ?? node.SelectSingleNode(".//span[contains(@class,'arti_bs')]");

                if (dateNode != null) date = dateNode.InnerText.Trim();
                else
                {
                    var match = Regex.Match(node.InnerText, @"\d{4}-\d{2}-\d{2}");
                    if (match.Success) date = match.Value;
                }

                if (string.IsNullOrEmpty(date)) return null;

                return new NewsItem
                {
                    Title = title,
                    Url = href,
                    Date = date,
                    IsNew = node.InnerHtml.Contains("new.jpg") || node.InnerHtml.Contains("new.gif")
                };
            }
            catch { return null; }
        }

        private List<NewsItem> GetFallbackNews(string msg)
        {
            return new List<NewsItem> { new NewsItem { Title = msg, Date = "提示", IsNew = false } };
        }
    }
}