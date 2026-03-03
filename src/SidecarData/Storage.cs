using System.Collections.Generic;
using Assets.Scripts.Util;
using UnityEngine;
using System.Linq;
using System.Threading;
using System;

namespace Com.DipoleCat.ExtensionLib
{
    public static class SidecarData{
        private static readonly ReaderWriterLockSlim @lock = new();
        private static Dictionary<long,Dictionary<NamespacedId,object>> ByReferenceId {get;}= [];
        private static Dictionary<NamespacedId,Dictionary<long,object>> BySidecarId {get;} = [];

        public static T? GetAs<T>(long referenceId, NamespacedId sidecarId) where T: class{
            using var guard = new ReadGuard();
            var value = Get(referenceId,sidecarId);
            if(value == null) return null;
            if(value is not T typed) return null;
            return typed;
        }

        public static T? GetSidecarAs<T>(this IReferencable referencable, NamespacedId sidecarId) where T: class
            => SidecarData.GetAs<T>(referencable.ReferenceId,sidecarId);

        public static object? Get(long referenceId, NamespacedId sidecarId){
            using var guard = new ReadGuard();
            if(!ByReferenceId.TryGetValue(referenceId, out var innerBySidecarId)) return null;
            if(!innerBySidecarId.TryGetValue(sidecarId, out var value)) return null;
            return value;
        }

        public static object? GetSidecar(this IReferencable referencable, NamespacedId sidecarId)
            => SidecarData.Get(referencable.ReferenceId,sidecarId);

        public static IEnumerable<KeyValuePair<NamespacedId,object>> GetAllForReference(long referenceId){
            using var guard = new ReadGuard();
            if(!ByReferenceId.TryGetValue(referenceId,out var innerBySidecarId)) return new List<KeyValuePair<NamespacedId,object>>();
            return new List<KeyValuePair<NamespacedId,object>>(innerBySidecarId);
        }

        public static IEnumerable<KeyValuePair<NamespacedId,object>> GetAllSidecars(this IReferencable referencable)
            => SidecarData.GetAllForReference(referencable.ReferenceId);

        public static IEnumerable<KeyValuePair<long,object>> GetAllForSidecarId(NamespacedId sidecarId){
            using var guard = new ReadGuard();
            if(!BySidecarId.TryGetValue(sidecarId,out var innerByReferenceId)) return new List<KeyValuePair<long,object>>();
            return new List<KeyValuePair<long,object>>(innerByReferenceId);
        }

        public static void Set(long referenceId, NamespacedId sidecarId, object value){
            if(referenceId == IReferencable.INVALID) throw new ArgumentOutOfRangeException(
                $"referenceId cannot be IReferencable.INVALID ({IReferencable.INVALID})"
            );
            using var guard = new WriteGuard();
            Dictionary<NamespacedId,object> innerBySidecarId;
            if(!ByReferenceId.TryGetValue(referenceId,out innerBySidecarId)) {
                innerBySidecarId = new();
                ByReferenceId[referenceId] = innerBySidecarId;
            }
            Dictionary<long,object> innerByReferenceId;
            if(!BySidecarId.TryGetValue(sidecarId,out innerByReferenceId)) {
                innerByReferenceId = new();
                BySidecarId[sidecarId] = innerByReferenceId;
            }

            innerBySidecarId[sidecarId] = value;
            innerByReferenceId[referenceId] = value;
        }

        public static void SetSidecar(this IReferencable referencable, NamespacedId sidecarId, object value)
            => SidecarData.Set(referencable.ReferenceId,sidecarId, value);

        public static void Remove(long referenceId, NamespacedId sidecarId){
            using var guard = new WriteGuard();
            if(!ByReferenceId.TryGetValue(referenceId, out var innerBySidecarId)) return;
            innerBySidecarId.Remove(sidecarId);
            Debug.Assert(BySidecarId.ContainsKey(sidecarId));
            BySidecarId[sidecarId].Remove(referenceId);
        }

        public static void RemoveSidecar(this IReferencable referencable, NamespacedId sidecarId)
            => SidecarData.Remove(referencable.ReferenceId,sidecarId);

        public static void RemoveAllForReferenceable(long referenceId){
            using var guard = new WriteGuard();
            if(ByReferenceId.Remove(referenceId)){
                foreach(var innerByReferenceId in BySidecarId.Values){
                    innerByReferenceId.Remove(referenceId);
                }
            }
        }

        public static void RemoveAllSidecars(this IReferencable referencable)
            => SidecarData.RemoveAllForReferenceable(referencable.ReferenceId);

        public static void Clear(){
            using var guard = new WriteGuard();
            ByReferenceId.Clear();
            BySidecarId.Clear();
        }

        internal static List<SidecarSerializable> GetSerializables(){
            using var guard = new ReadGuard();
            return [.. 
                from kv in ByReferenceId
                let referenceId = kv.Key
                from kv2 in kv.Value
                let sidecarId = kv2.Key
                let data = kv2.Value
                select new SidecarSerializable(sidecarId,referenceId,data)
            ];
        }

        internal static void LoadFromSerializables(IEnumerable<SidecarSerializable> serializables){
            foreach(var serializable in serializables){
                LoadFromSerializable(serializable);
            }
        }

        internal static void LoadFromSerializable(SidecarSerializable serializable){
            if(serializable.DataWrapper.Data == null) {
                Debug.LogError(
                    $"deserialized null sidecar for ref {serializable.AttachedReferenceId}, id {serializable.SidecarId}. "+
                    "Serialization error?");
                return;
            }
            if(serializable.AttachedReferenceId == IReferencable.INVALID){
                Debug.LogError(
                    $"deserialized sidecar with invalid 0-reference for id {serializable.SidecarId}, value {serializable.DataWrapper}. "+
                    "Serialization error?");
                return;
            }
            if(serializable.SidecarId == NamespacedId.EMPTY.ToString()){
                Debug.LogError(
                    $"deserialized sidecar with empty Id for ref {serializable.AttachedReferenceId}, value {serializable.DataWrapper}. "+
                    "Serialization error?");
                return;
            }

            Set(serializable.AttachedReferenceId,new NamespacedId(serializable.SidecarId),serializable.DataWrapper);
        }

        private sealed class ReadGuard : IDisposable
        {
            private bool disposed = false;
            public ReadGuard(){
                @lock.EnterReadLock();
            }
            public void Dispose()
            {
                if(!disposed) @lock.ExitReadLock();
                disposed = true;
            }
            ~ReadGuard(){
                Dispose();
            }
        }

        private sealed class WriteGuard : IDisposable
        {
            private bool disposed = false;
            public WriteGuard(){
                @lock.EnterWriteLock();
            }
            public void Dispose()
            {
                if(!disposed) @lock.ExitWriteLock();
                disposed = true;
            }
            ~WriteGuard(){
                Dispose();
            }
        }
    }
}