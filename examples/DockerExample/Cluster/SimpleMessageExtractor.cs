using Akka.Cluster.Sharding;
using Akka.Util;

namespace DockerExample.Cluster
{
    public sealed class SimpleMessageExtractor : IMessageExtractor
    {
        private readonly IDictionary<int, string> _shardCache;
        private readonly int _maxNumberOfShards;

        public SimpleMessageExtractor(int maxNumberOfShards = 10)
        {
            _maxNumberOfShards = maxNumberOfShards;
            _shardCache = new Dictionary<int, string>(_maxNumberOfShards);
            foreach (var shardId in Enumerable.Range(0, _maxNumberOfShards))
            {
                _shardCache[shardId] = shardId.ToString();
            }
        }

        public string ShardId(object message)
        {
            var entityId = EntityId(message);
            var shardId = _shardCache[Math.Abs(MurmurHash.StringHash(entityId)) % _maxNumberOfShards];
            return shardId;
        }

        public string EntityId(object message)
        {
            return message switch
            {
                GetEntityLocation getEntityLocation => getEntityLocation.EntityId,
                SimpleShardEnvelope simpleShardEnvelope => simpleShardEnvelope.EntityId,
                ShardRegion.StartEntity startEntity => startEntity.EntityId,
                _ => "None",
            };
        }

        public object EntityMessage(object message)
        {
            if (message is SimpleShardEnvelope simpleShardEnvelope)
            {
                return simpleShardEnvelope.Message;
            }

            return message;
        }
    }
}
