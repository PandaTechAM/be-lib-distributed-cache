using MessagePack;
using StackExchange.Redis.Extensions.Core;

namespace CacheService.Serializers;

internal class RedisMsgPackObjectSerializer : ISerializer
{
    public T? Deserialize<T>(byte[] serializedObject) => MessagePackSerializer.Deserialize<T?>(serializedObject);

    public byte[] Serialize<T>(T? item) => MessagePackSerializer.Serialize(item);
}