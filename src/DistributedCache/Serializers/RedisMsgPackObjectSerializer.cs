using MessagePack;
using StackExchange.Redis.Extensions.Core;

namespace DistributedCache.Serializers;

internal class RedisMsgPackObjectSerializer : ISerializer
{
   public T? Deserialize<T>(byte[]? serializedObject)
   {
      return MessagePackSerializer.Deserialize<T?>(serializedObject);
   }

   public byte[] Serialize<T>(T? item)
   {
      return MessagePackSerializer.Serialize(item);
   }
}