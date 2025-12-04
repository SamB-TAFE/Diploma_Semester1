using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RecordShelf_WebAPI.Models;

namespace RecordShelf_WebAPI.Services
{
    public class AudiosService
    {
        private readonly IMongoCollection<Audio> _audiosCollection;

        public AudiosService(IOptions<AudioLibraryDatabaseSettings> audioLibraryDatabaseSettings)
        {
            var mongoClient = new MongoClient(audioLibraryDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(audioLibraryDatabaseSettings.Value.DatabaseName);

            _audiosCollection = mongoDatabase.GetCollection<Audio>(audioLibraryDatabaseSettings.Value.AudiosCollectionName);
        }

        public async Task<List<Audio>> GetAllAsync() =>
        await _audiosCollection.Find(_ => true).SortByDescending(x => x.UploadDate).ToListAsync();

        public async Task<Audio?> GetSingleAsync(string audioId) =>
        await _audiosCollection.Find(x => x.AudioId == audioId).FirstOrDefaultAsync();

        public async Task<List<Audio>> GetUsersAudiosAsync(string userId) =>
        await _audiosCollection.Find(x => x.UserId == userId).SortByDescending(x => x.UploadDate).ToListAsync();

        public async Task CreateAsync(Audio newAudio) =>
        await _audiosCollection.InsertOneAsync(newAudio);

        public async Task UpdateAsync(string audioId, Audio updatedAudio) =>
        await _audiosCollection.ReplaceOneAsync(x => x.AudioId == audioId, updatedAudio);

        public async Task RemoveAsync(string audioId) =>
        await _audiosCollection.DeleteOneAsync(x => x.AudioId == audioId);

        // Search Functions //

        public async Task<List<Audio>> SearchbyNameAsync(string? nameSearch = null)
        {
            if (string.IsNullOrWhiteSpace(nameSearch))
            {
                return await GetAllAsync();
            }
            else
            {
                return await _audiosCollection.Find(x => x.AudioTitle.ToLower().Contains(nameSearch.ToLower())).SortByDescending(x => x.UploadDate).ToListAsync();
            }  
        }
        

        public async Task<List<Audio>> SearchbyArtistAsync(string? artistSearch = null)
        {
            if (string.IsNullOrWhiteSpace(artistSearch))
            {
                return await GetAllAsync();
            }
            else
            {
                return await _audiosCollection.Find(x => x.Artist.ToLower().Contains(artistSearch.ToLower())).SortByDescending(x => x.UploadDate).ToListAsync();
            }
        }
        

        public async Task<List<Audio>> SearchbyTagsAsync(string? tagSearch = null)
        {
            if (string.IsNullOrWhiteSpace(tagSearch))
            {
                return await GetAllAsync();
            }
            else
            {
                return await _audiosCollection.Find(x => x.Tags.Any(y => y.ToLower().Contains(tagSearch.ToLower()))).SortByDescending(x => x.UploadDate).ToListAsync();
            }
        }
        

        public async Task<List<Audio>> SearchUsersAudiosbyNameAsync(string userId, string? nameSearch = null)
        {
            if (string.IsNullOrWhiteSpace(nameSearch))
            {
                return await GetUsersAudiosAsync(userId);
            }
            else
            {
                return await _audiosCollection.Find(x => x.UserId == userId && x.AudioTitle.ToLower().Contains(nameSearch.ToLower())).SortByDescending(x => x.UploadDate).ToListAsync();
            }
        }
        

        public async Task<List<Audio>> SearchUsersAudiosbyTagAsync(string userId, string? tagSearch = null)
        {
            if (string.IsNullOrWhiteSpace(tagSearch))
            {
                return await GetUsersAudiosAsync(userId);
            }
            else
            {
                return await _audiosCollection.Find(x => x.UserId == userId && x.Tags.Any(y => y.ToLower().Contains(tagSearch.ToLower()))).SortByDescending(x => x.UploadDate).ToListAsync();
            }
        }
        
    }
}
