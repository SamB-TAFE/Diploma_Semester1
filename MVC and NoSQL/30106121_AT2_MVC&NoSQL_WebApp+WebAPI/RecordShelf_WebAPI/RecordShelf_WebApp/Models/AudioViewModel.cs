using System;

namespace RecordShelf_WebApp.Models
{
    public class AudioViewModel
    {
        public string? AudioId { get; set; }
        public string AudioTitle { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public string FilePath { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
    }
}
