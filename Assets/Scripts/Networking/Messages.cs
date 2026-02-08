using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using LaunchPadBooster;
using LaunchPadBooster.Networking;
using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;

namespace Com.DipoleCat.ExtensionLib.Networking{

    #nullable enable
    internal class RegistrySyncListMessage: ModNetworkMessage<RegistrySyncListMessage>{
        private readonly List<NamespacedId> registry_list = new();

        public IEnumerable<NamespacedId> Registries {
            get => registry_list.AsReadOnly();
        }

        public RegistrySyncListMessage(){}
        public RegistrySyncListMessage(IEnumerable<NamespacedId> registries){
            registry_list.AddRange(registries);
        }

        public override void Deserialize(RocketBinaryReader reader)
        {
            registry_list.AddRange(Serialization.ReadIdArray(reader));
        }

        public override void Serialize(RocketBinaryWriter writer)
        {
            Serialization.WriteArray(registry_list,writer);
        }

        public override void Process(long hostId)
        {
            //TODO: is there a reason to send this client->server
            if(NetworkManager.IsClient){
                ExtensionLib.Registries.BeginSync(Registries);
                NetworkManager.SendNetworkMessageToHost(NetworkChannel.GeneralTraffic,new RegistrySyncListAckMessage());
            }
        }
    }

    internal class RegistrySyncListAckMessage : ModNetworkMessage<RegistrySyncListAckMessage>
    {
        public override void Deserialize(RocketBinaryReader reader)
        {
            //Trivial; Acknowledgement
        }

        public override void Serialize(RocketBinaryWriter writer)
        {
            //Trivial; Acknowledgement
        }
    }

    internal class RegistryMessage : ModNetworkMessage<RegistryMessage>
    {
        public IRegistry? Registry {get; private set;} = null;
        public NamespacedId? RegistryId {get; private set;} = null;

        public RegistryMessage(){}
        public RegistryMessage(NamespacedId registryId, IRegistry registry){
            //NamespacedId is value type; cannot be null
            RegistryId = registryId;
            Registry= registry ?? throw new ArgumentNullException("registry");
        }

        public override void Deserialize(RocketBinaryReader reader)
        {
            Registry = ExtensionLib.Registry.Deserialize(reader);
        }

        public override void Serialize(RocketBinaryWriter writer)
        {
            if(Registry == null || RegistryId == null) throw new InvalidOperationException("Attempt to serialize default-constructed RegistryMessage");
            Registry.Serialize(writer);
        }

        public override void Process(long hostId)
        {
            //TODO: is there a reason to send this client->server
            if(NetworkManager.IsServer){
                Debug.LogError($"RegistryIndices messages sent by client {hostId}; should only be sent by server");
                return;
            }
            if(NetworkManager.IsClient){
                Registries.SetSyncedRegistry(RegistryId!.Value,Registry!);
            }
        }
    }

    internal class RegistryDataMessage : ModNetworkMessage<RegistryMessage>
    {
        //IRegistry<?>
        public IRegistry? Registry {get; private set;} = null;
        public Type? DataType {get; private set;} = null;
        public NamespacedId? RegistryId {get; private set;} = null;

        public RegistryDataMessage(){}
        internal RegistryDataMessage(NamespacedId registryId, Type dataType, IRegistry registry){
            //NamespacedId is value type; cannot be null
            RegistryId = registryId;
            DataType= dataType ?? throw new ArgumentNullException("dataType");
            Registry= registry ?? throw new ArgumentNullException("registry");
        }
        public static RegistryDataMessage Make<T>(NamespacedId registryId,IRegistry<T> registry) where T: class{
            return new RegistryDataMessage(registryId,typeof(T),registry);
        }
        public override void Deserialize(RocketBinaryReader reader)
        {
            RegistryId = new NamespacedId(reader.ReadString());
            DataType = Type.GetType(reader.ReadString());
            var codec = Registries.codecs[RegistryId.Value];
            Registry = (IRegistry)typeof(Registry)
                .GetMethod(nameof(ExtensionLib.Registry.Deserialize))
                .MakeGenericMethod(DataType)
                .Invoke(null,new object[]{codec,reader});
        }

        public override void Serialize(RocketBinaryWriter writer)
        {
            if(Registry == null || RegistryId == null || DataType == null){
                throw new InvalidOperationException("Attempt to serialize default-constructed RegistryDataMessage");
            }
            writer.WriteString(RegistryId.Value.Id);
            writer.WriteString(DataType.AssemblyQualifiedName);
            Registry.Serialize(writer);
        }

        public override void Process(long hostId)
        {
            if(Registry == null || RegistryId == null || DataType == null){
                throw new InvalidOperationException("Attempt to process default-constructed RegistryDataMessage");
            }
            //TODO: is there a reason to send this client->server
            if(NetworkManager.IsServer){
                Debug.LogError($"RegistryIndices messages sent by client {hostId}; should only be sent by server");
                return;
            }
            if(NetworkManager.IsClient){
                Registries.SetSyncedRegistry(RegistryId.Value,Registry);
            }
        }
    }

    internal class RegistrySyncDoneMessage : ModNetworkMessage<RegistrySyncDoneMessage>
    {
        public override void Deserialize(RocketBinaryReader reader)
        {
            //Trivial; Acknowledgement
        }

        public override void Serialize(RocketBinaryWriter writer)
        {
            //Trivial; Acknowledgement
        }
    }
}