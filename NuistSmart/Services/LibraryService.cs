using HtmlAgilityPack;
using NuistSmart.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace NuistSmart.Services
{
    public class LibraryService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<List<BookItem>> SearchBooksAsync(string keyword)
        {
            var results = new List<BookItem>();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return results;
            }

            try
            {
                // URL Encode the keyword
                string encodedKeyword = HttpUtility.UrlEncode(keyword);
                
                // Target URL for OPAC openlink
                string url = $"https://elib.nuist.edu.cn/opac/openlink.php?strSearchType=title&strText={encodedKeyword}";

                // Ensure using proper headers, some OPAC systems block plain agents
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string htmlContent = await response.Content.ReadAsStringAsync();

                // Setup HtmlAgilityPack Document
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(htmlContent);

                // TODO: 填入正确的书目列表 XPath 占位
                // 例如: "//ol[@id='search_book_list']/li" 或 "//table[@class='result_list']/tr"
                var bookNodes = doc.DocumentNode.SelectNodes("//li[@class='book_list_info']"); 

                if (bookNodes != null)
                {
                    foreach (var node in bookNodes)
                    {
                        var book = new BookItem();
                        
                        // TODO: 填入各项具体属性的 XPath 解析逻辑
                        /* 示例:
                        book.Title = node.SelectSingleNode(".//h3/a")?.InnerText.Trim();
                        book.Author = node.SelectSingleNode(".//p[@class='author']")?.InnerText.Trim();
                        book.Publisher = node.SelectSingleNode(".//p[@class='publisher']")?.InnerText.Trim();
                        book.CallNumber = node.SelectSingleNode(".//p[@class='callNo']")?.InnerText.Trim();
                        book.AvailableStatus = node.SelectSingleNode(".//span[@class='status']")?.InnerText.Trim();
                        book.CoverUrl = node.SelectSingleNode(".//img")?.GetAttributeValue("src", "");
                        */

                        // TODO 删除以下占位用假数据，目前用于在解析完善前测试跑通流程
                        book.Title = "测试书名: " + keyword;
                        book.Author = "测试作者";
                        book.Publisher = "测试出版社";
                        book.CallNumber = "A123/456";
                        book.AvailableStatus = "可借";
                        book.CoverUrl = "ms-appx:///Assets/Square150x150Logo.scale-200.png";

                        results.Add(book);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibraryService] 搜索异常: {ex.Message}");
                // In production, you might want to rethrow or return custom error states.
            }

            return results;
        }
    }
}
