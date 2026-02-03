namespace NuistSmart.Models
{
    public class NewsItem
    {
        public string Title { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsNew { get; set; }
    }
}
