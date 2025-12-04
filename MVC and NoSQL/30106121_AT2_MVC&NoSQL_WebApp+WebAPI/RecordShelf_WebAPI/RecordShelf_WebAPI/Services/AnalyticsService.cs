using RecordShelf_WebAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace RecordShelf_WebAPI.Services
{
    public class AnalyticsService
    {
        private readonly IMongoCollection<Analytics> _analyticsCollection;

        public AnalyticsService(IOptions<AudioLibraryDatabaseSettings> audioLibraryDatabaseSettings)
        {
            var mongoClient = new MongoClient(audioLibraryDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(audioLibraryDatabaseSettings.Value.DatabaseName);

            _analyticsCollection = mongoDatabase.GetCollection<Analytics>(audioLibraryDatabaseSettings.Value.AnalyticsCollectionName);
        }

        public async Task<List<Analytics>> GetAllAsync() =>
        await _analyticsCollection.Find(_ => true).ToListAsync();

        public async Task<Analytics?> GetSingleAsync(string analId) =>
        await _analyticsCollection.Find(x => x.AnalyticsId == analId).FirstOrDefaultAsync();

        public async Task<Analytics?> GetByAudioIdAsync(string audioId) =>
        await _analyticsCollection.Find(x => x.AudioId == audioId).FirstOrDefaultAsync();

        public async Task<List<Analytics>> GetUsersAnalyticsAsync(string userId) =>
        await _analyticsCollection.Find(x => x.UserId == userId).ToListAsync();

        public async Task CreateAsync(Analytics newAnalytics) =>
        await _analyticsCollection.InsertOneAsync(newAnalytics);

        public async Task UpdateAsync(string analyticId, Analytics updatedAnalytics) =>
        await _analyticsCollection.ReplaceOneAsync(x => x.AnalyticsId == analyticId, updatedAnalytics);

        public async Task RemoveAsync(string analyticId) =>
        await _analyticsCollection.DeleteOneAsync(x => x.AnalyticsId == analyticId);

        public async Task RemovebyAudioIdAsync(string audioId) =>
        await _analyticsCollection.DeleteOneAsync(x => x.AudioId == audioId);
    }
}
