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
    public class NewsResult
    {
        public List<NewsItem> Items { get; set; } = new();
        public string NextPageUrl { get; set; } = string.Empty;
    }

    public class NewsService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://bulletin.nuist.edu.cn";

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

        public async Task<NewsResult> GetNewsListAsync(string categoryOrUrl = "全部", bool isNextPage = false)
        {
            var result = new NewsResult();
            try
            {
                string url;
                if (isNextPage) url = categoryOrUrl;
                else
                {
                    string fileName = _categoryMap.ContainsKey(categoryOrUrl) ? _categoryMap[categoryOrUrl] : "index.htm";
                    url = $"{BaseUrl}/{fileName}";
                }

                var responseBytes = await _httpClient.GetByteArrayAsync(url);
                string htmlContent = Encoding.UTF8.GetString(responseBytes);
                if (htmlContent.Count(c => c == '\uFFFD') > 50)
                    htmlContent = Encoding.GetEncoding("GBK").GetString(responseBytes);

                int doctypeIndex = htmlContent.IndexOf("DOCTYPE");
                if (doctypeIndex > 0) htmlContent = "<" + htmlContent.Substring(doctypeIndex);

                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                var nodes = doc.DocumentNode.SelectNodes("//ul[contains(@class,'news_list')]//li")
                         ?? doc.DocumentNode.SelectNodes("//li");

                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        var item = ParseNode(node);
                        if (item != null) result.Items.Add(item);
                    }
                }

                var nextPageNode = doc.DocumentNode.SelectSingleNode("//a[contains(text(),'下页')]")
                                ?? doc.DocumentNode.SelectSingleNode("//a[contains(text(),'下一页')]");

                if (nextPageNode != null)
                {
                    string nextHref = nextPageNode.GetAttributeValue("href", "");
                    if (!string.IsNullOrEmpty(nextHref) && !nextHref.Contains("javascript"))
                    {
                        if (nextHref.StartsWith("http")) result.NextPageUrl = nextHref;
                        else
                        {
                            var uri = new Uri(url);
                            var path = uri.AbsolutePath;
                            var directory = path.Substring(0, path.LastIndexOf('/') + 1);
                            result.NextPageUrl = $"{uri.Scheme}://{uri.Host}{directory}{nextHref}";
                        }
                    }
                }

                return result;
            }
            catch (Exception) { return result; }
        }

        private NewsItem? ParseNode(HtmlNode node)
        {
            try
            {
                string innerHtml = node.InnerHtml.ToLower();

                // 【核心修复】精准拦截置顶公告
                // 根据你的源码：置顶公告包含 <span class="zdtb"> 和 top.jpg
                // 只要发现这两个特征中的任何一个，直接杀掉！
                if ( innerHtml.Contains("top.jpg") ||
                    innerHtml.Contains("top.gif")) // 保留 .gif 以防万一
                {
                    return null;
                }

                var titleNode = node.SelectSingleNode(".//span[contains(@class,'btt')]//a");
                if (titleNode == null) return null;

                string title = titleNode.GetAttributeValue("title", "").Trim();
                if (string.IsNullOrEmpty(title)) title = titleNode.InnerText.Trim();

                // 二次拦截
                if (title.Contains("置顶") || (title.StartsWith("[") && title.EndsWith("]"))) return null;

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
                    IsNew = innerHtml.Contains("new.jpg") || innerHtml.Contains("new.gif")
                };
            }
            catch { return null; }
        }
    }
}