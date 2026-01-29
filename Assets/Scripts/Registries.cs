using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.Networking;
using Com.DipoleCat.ExtensionLib.Networking;
using HarmonyLib;
using Unity.Properties;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Com.DipoleCat.ExtensionLib
{
    #nullable enable
    public static class Registries{
        private static readonly Dictionary<NamespacedId,IRegistry> local_registries = new();
        private static readonly Dictionary<NamespacedId,IRegistry> synced_registries = new();
        //todo: can I do better than objects?
        internal static readonly Dictionary<NamespacedId,object> codecs = new();
        public static bool IsAuthoritative {get; private set;} = true;
        public static bool IsSyncing {get; private set;} = false;

        public static IRegistry Fluids => 
            IsAuthoritative? 
            local_registries[new NamespacedId("extensionlib:fluids")]: 
            synced_registries[new NamespacedId("extensionlib:fluids")];
        private static readonly HashSet<NamespacedId> expectedRegistries = new();

        internal static void BeginSync(IEnumerable<NamespacedId> registries){
            if(NetworkManager.IsServer){
                Debug.LogError(
                    "Registries.BeginSync called from server; "+
                    "this method is for setting expected syncs sent to client by server"
                );
                return;
            }
            IsAuthoritative = false;
            expectedRegistries.UnionWith(registries);
            if(expectedRegistries.Count == 0){
                Debug.LogError("Begin registry sync for empty list of registries");
            }
            else {
                IsSyncing = true;
            }
        }

        internal static void SetSyncedRegistry(NamespacedId registryId, IRegistry registry){
            if(NetworkManager.IsServer){
                Debug.LogError(
                    "Registries.SetSyncedRegistry called from server; "+
                    "this method is for reifying registries sent to client by server"
                );
                return;
            }
            synced_registries[registryId] = registry;
            expectedRegistries.Remove(registryId);
            if(expectedRegistries.Count == 0){
                NetworkManager.SendNetworkMessageToHost(NetworkChannel.GeneralTraffic, new RegistrySyncDoneMessage());
            }
        }

        public static IRegistry? GetRegistry(NamespacedId registryId){
            throw new NotImplementedException();
        }

        public static IRegistry<T>? GetRegistry<T>(NamespacedId registryId) where T: struct{
            return GetRegistry(registryId) as IRegistry<T>;
        }

        public static IRegistry CreateRegistry(NamespacedId registryId){
            var registry = new SimpleRegistry();
            local_registries[registryId] = registry;
            return registry;
        }

        public static IRegistry<T> CreateRegistry<T>(NamespacedId registryId, INetworkCodec<T> codec) where T: struct{
            var registry = new SimpleRegistry<T>(codec);
            local_registries[registryId] = registry;
            codecs[registryId] = codec;
            return registry;
        }
    }
}