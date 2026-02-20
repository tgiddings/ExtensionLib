using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.Networking;
using HarmonyLib;
using Unity.Properties;
using Com.DipoleCat.ExtensionLib.Networking;
using System.Collections;

namespace Com.DipoleCat.ExtensionLib
{
    #nullable enable

    public static class Registry{
        public static IRegistry Deserialize(RocketBinaryReader reader){
            var entries = Serialization.ReadStringArray(reader);
            Dictionary<NamespacedId,uint> id_map = new(entries.Length);
            List<NamespacedId> id_list = new(entries.Length);
            for(int index=0; index < entries.Length; index++){
                var id = new NamespacedId(entries[index]);
                id_map.Add(id,(uint)index);
                id_list.Add(id);
            }
            return new FrozenRegistry(id_map,id_list);
        }

        public static IRegistry<T> Deserialize<T>(INetworkCodec<T> codec, RocketBinaryReader reader) where T: class{
            var entries = Serialization.ReadArray(new NetworkEntryCodec<T>(codec),reader);
            Dictionary<NamespacedId,uint> id_map = new(entries.Length);
            List<NamespacedId> id_list = new(entries.Length);
            List<T> data_list = new(entries.Length);
            for(int index=0; index < entries.Length; index++){
                var id = entries[index].Id;
                var data = entries[index].Data;
                id_map.Add(id,(uint)index);
                id_list.Add(id);
                data_list.Add(data);
            }
            return new FrozenRegistry<T>(id_map,id_list,data_list,codec);
        }
    }

    /// <summary>
    /// A 1:1 mapping of <see cref="NamespacedId">human-readable string IDs</see> to indices,
    /// which can be used networking.
    /// </summary>
    /// <remarks>
    /// The derived interface <see cref="IRegistry{T}"/> also stores associated data with each ID.
    /// The derived interfaces <see cref="IMutableRegistry{T}"/> and <see cref="IMutableRegistry"/>
    /// have methods for registering new entries,
    /// and <see cref="IMutableRegistry{T}"/> has methods for replacing the associated data.
    /// </remarks>
    public interface IRegistry{
        public IEnumerable<NamespacedId> OrderedIds {get;}
        uint? GetIndex(NamespacedId id);
        NamespacedId? GetId(uint index);
        bool IsPresent(NamespacedId id){
            return GetIndex(id).HasValue;
        }
        bool IsPresent(uint index);

        public int Count{get;}

        public void Serialize(RocketBinaryWriter writer);
    }

    /// <summary>
    /// A 1:1:1 mapping of <see cref="NamespacedId">human-readable string IDs</see> to indices,
    /// which can be used networking, and some other associated data of type <typeparam name="T"/>.
    /// </summary>
    /// <remarks>
    /// The derived interface <see cref="IMutableRegistry{T}"/> has methods for registering new entries,
    /// as well as methods for replacing the associated data.
    /// </remarks>
    /// <seealso cref="IRegistry"/>
    /// <seealso cref="IMutableRegistry"/>
    public interface IRegistry<T>: IRegistry where T: class{
        public IEnumerable<T> OrderedData {get;}
        public T? GetData(uint index);
        public T? GetData(NamespacedId id){
            uint? index = GetIndex(id);
            if(!index.HasValue) return null;
            return GetData(index.Value);
        }

        /// <exception cref="KeyNotFoundException"><paramref name="index"/> has not been registered</exception>
        public T this[uint index]{
            get {
                var value = GetData(index);
                return value ?? throw new KeyNotFoundException($"index {index} is not registered");
            }
        }

        /// <exception cref="KeyNotFoundException"><paramref name="id"/> has not been registered</exception>
        public T this[NamespacedId id]{
            get {
                var value = GetData(id);
                return value ?? throw new KeyNotFoundException($"ID {id} is not registered");
            }
        }
    }

    /// <summary>
    /// A modifiable, 1:1:1 mapping of <see cref="NamespacedId">human-readable string IDs</see> to indices,
    /// which can be used networking, and some other associated data of type <typeparam name="T"/>.
    /// </summary>
    /// <seealso cref="IRegistry{T}"/>
    /// <seealso cref="IMutableRegistry"/>
    public interface IMutableRegistry<T>: IRegistry<T> where T: class {
        /// <exception cref="ArgumentException"><paramref name="id"/> has already been registered </exception>
        public uint Register(NamespacedId id, T data);
        /// <remarks>
        /// may invalidate the indices of other entries
        /// </remarks>
        public void Deregister(NamespacedId id);

        /// <exception cref="KeyNotFoundException"><paramref name="index"/> has not been registered</exception>
        public void SetData(uint index, T data);

        /// <exception cref="KeyNotFoundException"><paramref name="id"/> has not been registered</exception>
        public void SetData(NamespacedId id, T data){
            uint? index = GetIndex(id);
            if(!index.HasValue) throw new KeyNotFoundException($"id {id} has not been registered");
            else SetData(index.Value,data);
        }

        /// <summary>
        /// If <paramref name="id"/>, sets its registered data to <paramref name="data"/>.
        /// Otherwise, registers <paramref name="id"/> with <paramref name="data"/>
        /// </summary>
        public void SetOrRegister(NamespacedId id, T data){
            uint? index = GetIndex(id);
            if(!index.HasValue) Register(id,data);
            else SetData(index.Value,data);
        }

        public IRegistry FrozenCopy();

        /// <exception cref="KeyNotFoundException"><paramref name="index"/> has not been registered</exception>
        new public T this[uint index]{
            get {
                var value = GetData(index);
                return value ?? throw new KeyNotFoundException($"index {index} is not registered");
            }
            set
            {
                SetData(index, value);
            }
        }

        /// <exception cref="KeyNotFoundException">
        /// <paramref name="id"/> has not been registered.
        /// Note that this is only thrown by the getter, since the setter will register the ID if not already registered
        /// </exception>
        new public T this[NamespacedId id]{
            get {
                var value = GetData(id);
                return value??throw new KeyNotFoundException($"ID {id} is not registered");
            }
            set{
                SetOrRegister(id, value);
            }
        }
    }

    /// <summary>
    /// A modifiable, 1:1:1 mapping of <see cref="NamespacedId">human-readable string IDs</see> to indices,
    /// which can be used networking, and some other associated data of type <typeparam name="T"/>.
    /// </summary>
    /// <seealso cref="IRegistry"/>
    /// <seealso cref="IMutableRegistry{T}"/>
    public interface IMutableRegistry: IRegistry {
        /// <exception cref="ArgumentException"><paramref name="id"/> has already been registered </exception>
        public uint Register(NamespacedId id);
        /// <remarks>
        /// may invalidate the indices of other entries
        /// </remarks>
        public void Deregister(NamespacedId id);

        public IRegistry FrozenCopy();
    }

    //note: not thread-safe
    public sealed class SimpleRegistry<T>: IMutableRegistry<T> where T: class
    {
        internal readonly Dictionary<NamespacedId,uint> id_map = new();

        //effectively a reverse id_map, but since indices are guaranteed to be consecutive and start at 0,
        //we index into a List instead of a Dictionary
        internal readonly List<NamespacedId> id_list = new();

        internal readonly List<T> data_list = new();

        internal readonly INetworkCodec<T> codec;

        public int Count => id_list.Count;

        public IEnumerable<T> OrderedData => data_list.AsReadOnly();

        public IEnumerable<NamespacedId> OrderedIds => id_list.AsReadOnly();

        public SimpleRegistry(INetworkCodec<T> codec){
            this.codec = codec;
        }

        public uint Register(NamespacedId id, T data){
            //enforce uniqueness
            uint? existing_index = GetIndex(id);
            if(existing_index.HasValue) throw new ArgumentException($"ID {id} is already registered");

            uint index = (uint)Count;
            id_list.Add(id);
            data_list.Add(data);
            id_map.Add(id,index);
            return index;
        }
        public T? GetData(uint index){
            if (index >= Count) return null;
            return data_list[(int)index];
        }
        public T? GetData(NamespacedId id){
            uint? integer_id = GetIndex(id);
            if(!integer_id.HasValue) return null;
            return GetData(integer_id.Value);
        }

        public void SetData(uint index, T data){
            if (index >= Count) throw new KeyNotFoundException($"index {index} is not registered");
            data_list[(int)index] = data;
        }

        public uint? GetIndex(NamespacedId id)
        {
            if(!id_map.ContainsKey(id)) return null;
            return id_map[id];
        }

        public NamespacedId? GetId(uint index)
        {
            if (index >= Count) throw new KeyNotFoundException($"index {index} is not registered");
            return id_list[(int)index];
        }

        public bool IsPresent(uint index)
        {
            return index < Count;
        }

        public bool IsPresent(NamespacedId id){
            return id_map.ContainsKey(id);
        }

        public void Deregister(NamespacedId id)
        {
            uint? index = GetIndex(id);
            //not registered; nothing to do
            if(!index.HasValue) return;
            id_list.RemoveAt((int)index);
            id_map.Remove(id);
        }

        public IRegistry FrozenCopy()
        {
            return new FrozenRegistry<T>(
                id_map,
                id_list,
                data_list,
                codec);
        }

        public void Serialize(RocketBinaryWriter writer)
        {
            Serialization.WriteArray(
                new List<NetworkEntry<T>>(
                    from index in Enumerable.Range(0,(int)Count)
                    let id = GetId((uint)index)
                    let data = GetData((uint)index)
                    orderby index
                    select new NetworkEntry<T>(id.Value,data)
                ),
                new NetworkEntryCodec<T>(codec),
                writer
            );
        }
    }

    internal sealed class FrozenRegistry<T> : IRegistry<T> where T : class
    {
        private readonly Dictionary<NamespacedId,uint> id_map;

        //effectively a reverse id_map, but since indices are guaranteed to be consecutive and start at 0,
        //we index into a List instead of a Dictionary
        private readonly List<NamespacedId> id_list;

        private readonly List<T> data_list;

        private readonly INetworkCodec<T> codec;

        public int Count => id_list.Count;

        public IEnumerable<T> OrderedData => data_list.AsReadOnly();

        public IEnumerable<NamespacedId> OrderedIds => id_list.AsReadOnly();

        internal FrozenRegistry(
            Dictionary<NamespacedId,uint> id_map,
            List<NamespacedId> id_list,
            List<T> data_list,
            INetworkCodec<T> codec){
                //no reference types in these, so shallow copy is sufficient
            this.id_map = new(id_map);
            this.id_list = new(id_list);
            this.data_list = new(data_list);
            this.codec = codec;
        }

        public T? GetData(uint index)
        {
            if (index >= Count) return null;
            return data_list[(int)index];
        }

        public uint? GetIndex(NamespacedId id)
        {
            if(!id_map.ContainsKey(id)) return null;
            return id_map[id];
        }

        public NamespacedId? GetId(uint index)
        {
            if (index >= Count) throw new KeyNotFoundException($"index {index} is not registered");
            return id_list[(int)index];
        }

        public bool IsPresent(uint index)
        {
            return index < Count;
        }

        public void Serialize(RocketBinaryWriter writer)
        {
            Serialization.WriteArray(
                new List<NetworkEntry<T>>(
                    from index in Enumerable.Range(0,(int)Count)
                    let id = GetId((uint)index)
                    let data = GetData((uint)index)
                    orderby index
                    select new NetworkEntry<T>(id.Value,data)
                ),
                new NetworkEntryCodec<T>(codec),
                writer
            );
        }
    }
    public sealed class SimpleRegistry : IMutableRegistry
    {
        internal readonly Dictionary<NamespacedId,uint> id_map = new();

        //effectively a reverse id_map, but since indices are guaranteed to be consecutive and start at 0,
        //we index into a List instead of a Dictionary
        internal readonly List<NamespacedId> id_list = new();

        public int Count => id_list.Count;

        public IEnumerable<NamespacedId> OrderedIds => id_list.AsReadOnly();

        public void Deregister(NamespacedId id)
        {
            uint? index = GetIndex(id);
            //not registered; nothing to do
            if(!index.HasValue) return;
            id_list.RemoveAt((int)index);
            id_map.Remove(id);
        }

        public IRegistry FrozenCopy()
        {
            return new FrozenRegistry(this);
        }

        public uint? GetIndex(NamespacedId id)
        {
            if(!id_map.ContainsKey(id)) return null;
            return id_map[id];
        }

        public NamespacedId? GetId(uint index)
        {
            if (index >= Count) throw new KeyNotFoundException($"index {index} is not registered");
            return id_list[(int)index];
        }

        public bool IsPresent(uint index)
        {
            return index < Count;
        }

        public uint Register(NamespacedId id)
        {
            //enforce uniqueness
            uint? existing_index = GetIndex(id);
            if(existing_index.HasValue) throw new ArgumentException($"ID {id} is already registered");

            uint index = (uint)Count;
            id_list.Add(id);
            id_map.Add(id,index);
            return index;
        }

        public void Serialize(RocketBinaryWriter writer)
        {
            Serialization.WriteArray(
                new List<string>(
                    from index in Enumerable.Range(0, Count)
                    let id = GetId((uint)index)
                    orderby index
                    select id.Value.Id
                ),
                writer
            );
        }
    }

    internal sealed class FrozenRegistry : IRegistry
    {
        internal readonly Dictionary<NamespacedId,uint> id_map = new();

        //effectively a reverse id_map, but since indices are guaranteed to be consecutive and start at 0,
        //we index into a List instead of a Dictionary
        internal readonly List<NamespacedId> id_list = new();

        public int Count => id_list.Count;

        public IEnumerable<NamespacedId> OrderedIds => id_list.AsReadOnly();

        public FrozenRegistry(SimpleRegistry registry):
            this(registry.id_map,registry.id_list){}

        public FrozenRegistry(Dictionary<NamespacedId,uint> id_map, List<NamespacedId> id_list){
            //no reference types in these, so shallow copy is sufficient
            this.id_map = new(id_map);
            this.id_list = new(id_list);
        }

        public uint? GetIndex(NamespacedId id)
        {
            if(!id_map.ContainsKey(id)) return null;
            return id_map[id];
        }

        public NamespacedId? GetId(uint index)
        {
            if (index >= Count) throw new KeyNotFoundException($"index {index} is not registered");
            return id_list[(int)index];
        }

        public bool IsPresent(uint index)
        {
            return index < Count;
        }

        public void Serialize(RocketBinaryWriter writer)
        {
            Serialization.WriteArray(
                new List<string>(
                    from index in Enumerable.Range(0,(int)Count)
                    let id = GetId((uint)index)
                    orderby index
                    select id.Value.Id
                ),
                writer
            );
        }
    }

    internal readonly struct NetworkEntry<T>{
        internal NamespacedId Id{get;}
        internal T Data{get;}

        public NetworkEntry(NamespacedId id, T data){
            this.Id = id;
            this.Data = data;
        }
    }

    internal class NetworkEntryCodec<T> : INetworkCodec<NetworkEntry<T>>{
        private readonly INetworkCodec<T> inner;
        internal NetworkEntryCodec(INetworkCodec<T> inner){
            this.inner = inner;
        }
        public NetworkEntry<T> Read(RocketBinaryReader reader)
        {
            return new NetworkEntry<T>(
                new NamespacedId(reader.ReadString()),
                inner.Read(reader)
            );
        }

        public void Write(NetworkEntry<T> value, RocketBinaryWriter writer)
        {
            writer.WriteString(value.Id.Id);
            inner.Write(value.Data,writer);
        }
    }
}