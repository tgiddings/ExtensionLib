using System;
using System.Collections.Generic;
using Assets.Scripts.Networking;

namespace Com.DipoleCat.ExtensionLib.Networking
{
    #nullable enable
    public static class Serialization{
        
        public static void WriteVarInt(uint value, RocketBinaryWriter writer){
            while (value > 127) {
                writer.WriteByte((byte)(0x80 + (value&0x7f)));
                value >>= 7;
            } 
            writer.WriteByte((byte)value);
        }

        public static uint ReadVarInt(RocketBinaryReader reader){
            uint result = 0;
            byte lsb_index = 0;
            byte next = reader.ReadByte();
            while ((next & 0x80) > 0){
                result |= (next & 0x7fu)<<lsb_index;
                next = reader.ReadByte();
                lsb_index+=7;
            }
            result |= (next & 0x7fu)<<lsb_index;
            return result;
        }

        public static void WriteVarLong(ulong value, RocketBinaryWriter writer){
            while (value > 127) {
                writer.WriteByte((byte)(0x80 + (value&0x7f)));
                value >>= 7;
            } 
            writer.WriteByte((byte)value);
        }

        public static ulong ReadVarLong(RocketBinaryReader reader){
            ulong result = 0;
            byte lsb_index = 0;
            byte next = reader.ReadByte();
            while ((next & 0x80) > 0){
                result |= ((ulong)(next & 0x7fu))<<lsb_index;
                next = reader.ReadByte();
                lsb_index+=7;
            }
            result |= ((ulong)(next & 0x7fu))<<lsb_index;
            return result;
        }
    
        public static void WriteOptional<T>(T? value, INetworkCodec<T> codec, RocketBinaryWriter writer){
            if(value == null){
                writer.WriteBoolean(false);
            }
            else {
                writer.WriteBoolean(true);
                codec.Write(value,writer);
            }
        }

        public static T? ReadOptionalStruct<T>(INetworkCodec<T> codec, RocketBinaryReader reader) where T:struct{
            var isPresent = reader.ReadBoolean();
            if(!isPresent) return null;
            else return codec.Read(reader);
        }

        public static T? ReadOptionalClass<T>(INetworkCodec<T> codec, RocketBinaryReader reader) where T:class{
            var isPresent = reader.ReadBoolean();
            if(!isPresent) return null;
            else return codec.Read(reader);
        }

        public static void WriteArray<T>(ICollection<T> values, INetworkCodec<T> codec, RocketBinaryWriter writer){
            WriteVarInt((uint)values.Count,writer);
            foreach(T value in values){
                codec.Write(value,writer);
            }
        }

        public static void WriteArray(ICollection<string> values, RocketBinaryWriter writer){
            WriteArray(values,Codecs.StringCodec,writer);
        }

        public static void WriteArray(ICollection<NamespacedId> values, RocketBinaryWriter writer){
            WriteArray(values,Codecs.IdCodec,writer);
        }

        public static T[] ReadArray<T>(INetworkCodec<T> codec, RocketBinaryReader reader){
            var length = ReadVarInt(reader);
            var result = new List<T>((int)length);
            for(int i=0; i < length; i++){
                result.Add(codec.Read(reader));
            }
            return result.ToArray();
        }

        public static string[] ReadStringArray(RocketBinaryReader reader){
            return ReadArray(Codecs.StringCodec, reader);
        }

        public static NamespacedId[] ReadIdArray(RocketBinaryReader reader){
            return ReadArray(Codecs.IdCodec, reader);
        }
    }
}