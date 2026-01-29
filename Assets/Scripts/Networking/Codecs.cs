using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using Com.DipoleCat.ExtensionLib;
using Unity.Serialization;
using Unity.Serialization.Json;

namespace Com.DipoleCat.ExtensionLib.Networking
{
    #nullable enable
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
    /// Serializes and Deserializes <typeparam cref="T"/> using the com.unity.serialization package.
    /// Note that the serialization format is JSON, and is achieved with reflection,
    /// so a custom codec is recommended for types which are frequently serialized
    /// </summary>
    public readonly struct UnitySerializationCodec<T> : INetworkCodec<T>
    {
        private readonly List<IJsonAdapter>? user_adapters;
        private static readonly List<IJsonAdapter> builtin_adapters = new (new[]{new IdAdapter()});

        private readonly List<IJsonAdapter> Adapters {
            get {
                var adapters = new List<IJsonAdapter>(builtin_adapters);
                if(user_adapters!=null) adapters.AddRange(user_adapters);
                return adapters;
            }
        }

        private readonly JsonSerializationParameters SerializationParameters => new()
        {
                    UserDefinedAdapters=Adapters
        };

        public UnitySerializationCodec(IEnumerable<IJsonAdapter> adapters){
            user_adapters=new(adapters);
        }

        public T Read(RocketBinaryReader reader)
        {
            
            return JsonSerialization.FromJson<T>(
                reader.ReadString(),
                SerializationParameters
            );
        }

        public void Write(T value, RocketBinaryWriter writer)
        {
            writer.WriteString(
                JsonSerialization.ToJson(
                    value,
                    SerializationParameters
                )
            );
        }
    }

    internal class IdAdapter : IJsonAdapter<NamespacedId>
    {
        public NamespacedId Deserialize(in JsonDeserializationContext<NamespacedId> context)
        {
            if(context.SerializedValue.Type != TokenType.String) {
                throw new SerializationException("Expect JSON string to deserialize to NamespacedId");
            }
            return new NamespacedId(context.SerializedValue.AsStringView().ToString());
        }

        public void Serialize(in JsonSerializationContext<NamespacedId> context, NamespacedId value)
        {
            context.Writer.WriteValue(value.Id);
        }
    }
}