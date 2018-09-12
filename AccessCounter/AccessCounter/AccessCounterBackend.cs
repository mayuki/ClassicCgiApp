using StackExchange.Redis;
using System.Threading;
using System.Threading.Tasks;

namespace AccessCounter
{
    public interface IAccessCounterBackend
    {
        Task<long> IncrementAsync();
        Task<long> GetAsync();
    }

    public class InMemoryAccessCounterBackend : IAccessCounterBackend
    {
        private long _counter = 0;

        public Task<long> IncrementAsync()
        {
            return Task.FromResult(Interlocked.Increment(ref _counter));
        }

        public Task<long> GetAsync()
        {
            return Task.FromResult(Interlocked.Read(ref _counter));
        }
    }

    public class RedisAccessCounterBackend : IAccessCounterBackend
    {
        private readonly RedisBackendOptions _options;
        private readonly ConnectionMultiplexer _connection;

        public RedisAccessCounterBackend(RedisBackendOptions options)
        {
            _options = options;
            _connection = ConnectionMultiplexer.Connect(options.ConnectionString);
        }

        public async Task<long> GetAsync()
        {
            var db = _connection.GetDatabase(_options.Database);
            var result = await db.StringGetAsync("AccessCounter");

            return result.HasValue ? (long)result : 0;
        }

        public async Task<long> IncrementAsync()
        {
            var db = _connection.GetDatabase(_options.Database);
            var result = await db.StringIncrementAsync("AccessCounter");

            return result;
        }

        public class RedisBackendOptions
        {
            /// <summary>
            /// <see cref="ConnectionMultiplexer"/>に渡されるRedisの接続文字列
            /// </summary>
            public string ConnectionString { get; set; } = "localhost";

            /// <summary>
            /// Redisのデータベース
            /// </summary>
            public int Database { get; set; } = -1;
        }
    }
}
