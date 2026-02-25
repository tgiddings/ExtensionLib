using System;
using System.Collections.Generic;
using Assets.Scripts.Networking;
using Newtonsoft.Json;

namespace Com.DipoleCat.ExtensionLib.Networking
{
    public static class Codecs{
        public static INetworkCodec<string> StringCodec{get;} = new StringCodec();
        public static INetworkCodec<NamespacedId> IdCodec{get;} = new IdCodec();
        
    }

    public interface INetworkCodec<T>{
        public void Write(T value, RocketBinaryWriter writer);
        public T Read(RocketBinaryReader reader);
    }

    internal struct StringCodec : INetworkCodec<string>
    {
        public readonly string Read(RocketBinaryReader reader)
        {
            return reader.ReadString();
        }

        public readonly void Write(string value, RocketBinaryWriter writer)
        {
            writer.WriteString(value);
        }
    }

    internal struct IdCodec: INetworkCodec<NamespacedId>
    {
        public readonly NamespacedId Read(RocketBinaryReader reader)
        {
            return new NamespacedId(reader.ReadString());
        }

        public readonly void Write(NamespacedId value, RocketBinaryWriter writer)
        {
            writer.WriteString(value.Id);
        }
    }

    /// <summary>
    /// Serializes and Deserializes <typeparam cref="T"/> using the newtonsoft json converter in the vanilla game.
    /// Note that the serialization format is text-based, and is achieved with reflection,
    /// so a custom codec is recommended for types which are frequently serialized
    /// </summary>
    public class JsonSerializationCodec<T> : INetworkCodec<T>
    {
        private readonly List<JsonConverter>? user_converters;
        private static readonly List<JsonConverter> builtin_converters = new (new[]{new IdAdapter()});

        private List<JsonConverter> Converters {
            get {
                var converters = new List<JsonConverter>(builtin_converters);
                if(user_converters!=null) converters.AddRange(user_converters);
                return converters;
            }
        }

        private JsonSerializerSettings SerializationParameters => new()
        {
            Converters=Converters
        };

        public JsonSerializationCodec(){
            user_converters=new();
        }

        public JsonSerializationCodec(IEnumerable<JsonConverter> converters){
            user_converters=new(converters);
        }

        public T Read(RocketBinaryReader reader)
        {
            return JsonConvert.DeserializeObject<T>(
                reader.ReadString(),
                SerializationParameters
            )!;
        }

        public void Write(T value, RocketBinaryWriter writer)
        {
            writer.WriteString(
                JsonConvert.SerializeObject(
                    value,
                    SerializationParameters
                )
            );
        }
    }

    internal class IdAdapter : JsonConverter<NamespacedId>
    {
        public override NamespacedId ReadJson(JsonReader reader, Type objectType, NamespacedId existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if(reader.TokenType != JsonToken.String) {
                throw new JsonSerializationException("Expect JSON string to deserialize to NamespacedId");
            }
            return new NamespacedId(reader.ReadAsString()!);
        }

        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, NamespacedId value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Id);
        }
    }
}