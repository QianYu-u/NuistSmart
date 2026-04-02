using HtmlAgilityPack;
using NuistSmart.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace NuistSmart.Services
{
    public class LibraryService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string BaseUrl = "https://elib.nuist.edu.cn";

        static LibraryService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        /// <summary>
        /// 获取最新入馆图书列表（通过 JSON API）
        /// </summary>
        public async Task<List<BookItem>> GetNewBooksAsync(int page = 1, int pageSize = 20)
        {
            var results = new List<BookItem>();

            try
            {
                string url = $"{BaseUrl}/meta-local/opac/new/30/bysubject?page={page}&pageSize={pageSize}&dcpCode=nolimit";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // API 返回结构: { "data": { "dataList": [...], "totalCount": N } }
                if (root.TryGetProperty("data", out var dataObj) &&
                    dataObj.TryGetProperty("dataList", out var dataList))
                {
                    foreach (var item in dataList.EnumerateArray())
                    {
                        var book = new BookItem();

                        book.Title = GetJsonString(item, "title");
                        book.Author = GetJsonString(item, "author");
                        book.Publisher = GetJsonString(item, "publisher");

                        // callno 是数组，取第一个
                        if (item.TryGetProperty("callno", out var callnoArr) &&
                            callnoArr.ValueKind == JsonValueKind.Array &&
                            callnoArr.GetArrayLength() > 0)
                        {
                            book.CallNumber = callnoArr[0].GetString() ?? string.Empty;
                        }

                        // 拼接详情页 URL
                        string bibId = GetJsonString(item, "bibId");
                        if (!string.IsNullOrEmpty(bibId))
                        {
                            book.DetailUrl = $"{BaseUrl}/space/searchDetailLocal/{bibId}";
                        }

                        // ISBN 用于后续封面获取
                        string isbn = GetJsonString(item, "isbn");
                        book.Description = isbn; // 临时存 ISBN，用于异步加载封面

                        results.Add(book);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibraryService] 获取新书列表异常: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// 通过 ISBN 获取图书封面 URL（多源策略：豆瓣代理 → Open Library）
        /// </summary>
        public async Task<string> GetCoverUrlAsync(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                return string.Empty;

            // 清洗 ISBN：去除连字符和空格
            string cleanIsbn = isbn.Replace("-", "").Replace(" ", "").Trim();
            if (string.IsNullOrEmpty(cleanIsbn))
                return string.Empty;

            // ====== 方法 1：图书馆豆瓣代理 API ======
            try
            {
                string url = $"{BaseUrl}/meta-local/opac/third_api/douban/{cleanIsbn}/info?title=null";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();

                    // API 有时返回非 JSON 文本（纯文本简介），需要安全解析
                    if (!string.IsNullOrEmpty(body) && body.TrimStart().StartsWith("{"))
                    {
                        using var doc = JsonDocument.Parse(body);

                        // 优先从 data 对象中取 imageUrl
                        JsonElement target = doc.RootElement;
                        if (target.TryGetProperty("data", out var dataObj))
                        {
                            target = dataObj;
                        }

                        if (target.TryGetProperty("imageUrl", out var imageUrlProp))
                        {
                            string imageUrl = imageUrlProp.GetString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                if (imageUrl.StartsWith("//"))
                                    imageUrl = "https:" + imageUrl;
                                return imageUrl;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibraryService] 豆瓣封面异常 (ISBN={cleanIsbn}): {ex.Message}");
            }

            // ====== 方法 2：Open Library Covers API ======
            try
            {
                // 使用 ?default=false 使无封面时返回 404 而非 1x1 像素
                string olCheckUrl = $"https://covers.openlibrary.org/b/isbn/{cleanIsbn}-M.jpg?default=false";
                using var olResponse = await _httpClient.GetAsync(olCheckUrl, HttpCompletionOption.ResponseHeadersRead);
                if (olResponse.IsSuccessStatusCode)
                {
                    return $"https://covers.openlibrary.org/b/isbn/{cleanIsbn}-M.jpg";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibraryService] OpenLibrary 封面异常 (ISBN={cleanIsbn}): {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 搜索图书（使用真实 POST API）
        /// </summary>
        public async Task<List<BookItem>> SearchBooksAsync(string keyword)
        {
            var results = new List<BookItem>();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return results;
            }

            try
            {
                string url = $"{BaseUrl}/meta-local/opac/search/";

                // 构造搜索请求体
                var requestBody = new
                {
                    page = 1,
                    pageSize = 20,
                    indexName = "idx.opac",
                    sortField = "relevance",
                    sortType = "desc",
                    collapseField = "groupId",
                    queryFieldList = new[]
                    {
                        new
                        {
                            logic = 0,
                            field = "all",
                            values = new[] { keyword },
                            @operator = "*"
                        }
                    },
                    filterFieldList = Array.Empty<object>()
                };

                string jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var dataObj) &&
                    dataObj.TryGetProperty("dataList", out var dataList))
                {
                    foreach (var item in dataList.EnumerateArray())
                    {
                        var book = new BookItem();

                        book.Title = GetJsonString(item, "title");
                        book.Author = GetJsonString(item, "author");
                        book.Publisher = GetJsonString(item, "publisher");

                        // callno 取第一个
                        if (item.TryGetProperty("callno", out var callnoArr) &&
                            callnoArr.ValueKind == JsonValueKind.Array &&
                            callnoArr.GetArrayLength() > 0)
                        {
                            book.CallNumber = callnoArr[0].GetString() ?? string.Empty;
                        }

                        // bibId 详情页
                        string bibId = GetJsonString(item, "bibId");
                        if (!string.IsNullOrEmpty(bibId))
                        {
                            book.DetailUrl = $"{BaseUrl}/space/searchDetailLocal/{bibId}";
                        }

                        // ISBN 封面获取（存储在临时字段供 ViewModel 读取）
                        string isbn = GetJsonString(item, "isbn");
                        book.Description = isbn; 

                        results.Add(book);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibraryService] 搜索异常: {ex.Message}");
            }

            return results;
        }

        private static string GetJsonString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString() ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
