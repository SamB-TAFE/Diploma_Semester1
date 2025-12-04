namespace RecordShelf_WebAPI.Models
{
    public class AudioLibraryDatabaseSettings
    {
        public string ConnectionString { get; set; } = null!;

        public string DatabaseName { get; set; } = null!;

        public string UsersCollectionName { get; set; } = null!;

        public string AudiosCollectionName { get; set; } = null!;

        public string AnalyticsCollectionName { get; set; } = null!;

    }
}
