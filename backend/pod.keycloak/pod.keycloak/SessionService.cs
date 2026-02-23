namespace pod.keycloak
{
    using StackExchange.Redis;
    using System.Text.Json;

    public class SessionService
    {
        private readonly IDatabase _db;

        public SessionService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task CreateSession(SessionModel session)
        {
            await _db.StringSetAsync(
                $"session:{session.SessionId}",
                JsonSerializer.Serialize(session),
                TimeSpan.FromMinutes(10));
        }

        public async Task<SessionModel?> GetSession(string sessionId)
        {
            var data = await _db.StringGetAsync($"session:{sessionId}");
            if (data.IsNullOrEmpty) return null;
            return JsonSerializer.Deserialize<SessionModel>(data!);
        }

        public async Task DeleteSession(string sessionId)
        {
            await _db.KeyDeleteAsync($"session:{sessionId}");
        }
    }
}
