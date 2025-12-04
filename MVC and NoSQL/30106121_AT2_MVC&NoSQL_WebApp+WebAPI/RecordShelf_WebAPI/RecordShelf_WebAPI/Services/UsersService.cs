using RecordShelf_WebAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace RecordShelf_WebAPI.Services
{
    public class UsersService
    {
        private readonly IMongoCollection<User> _usersCollection;

        public UsersService(IOptions<AudioLibraryDatabaseSettings> audioLibraryDatabaseSettings)
        {
            var mongoClient = new MongoClient(audioLibraryDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(audioLibraryDatabaseSettings.Value.DatabaseName);

            _usersCollection = mongoDatabase.GetCollection<User>(audioLibraryDatabaseSettings.Value.UsersCollectionName);
        }

        public async Task<List<User>> GetAllAsync() =>
        await _usersCollection.Find(_ => true).ToListAsync();

        public async Task<User?> GetSingleAsync(string id) =>
        await _usersCollection.Find(x => x.UserId == id).FirstOrDefaultAsync();

        public async Task<List<User>> SearchbyUsernameAsync(string nameSearch) =>
        await _usersCollection.Find(x => x.Username.Contains(nameSearch)).ToListAsync();

        public async Task CreateAsync(User newUser) =>
        await _usersCollection.InsertOneAsync(newUser);

        public async Task UpdateAsync(string id, User updatedUser) =>
        await _usersCollection.ReplaceOneAsync(x => x.UserId == id, updatedUser);

        public async Task RemoveAsync(string id) =>
        await _usersCollection.DeleteOneAsync(x => x.UserId == id);

        public async Task<User?> FindByEmailOrUsernameAsync(string input) =>
        await _usersCollection.Find(u => u.Email == input || u.Username == input).FirstOrDefaultAsync();

        public async Task<bool> ExistsByUsernameAsync(string username) =>
        await _usersCollection.Find(u => u.Username == username).AnyAsync();

        public async Task<bool> ExistsByEmailAsync(string email) =>
        await _usersCollection.Find(u => u.Email == email).AnyAsync();

    }
}
