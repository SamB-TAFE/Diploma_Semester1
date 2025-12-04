namespace RecordShelf_WebApp.Models
{
    public class AnalyticsViewModel
    {
        public string AnalyticsId { get; set; } = null!;

        public string AudioId { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public int ListenCount { get; set; }

        public int LikeCount { get; set; }

    }
}
