using System;
using UnityEngine;

namespace Com.DipoleCat.ExtensionLib
{
    public readonly struct NamespacedId {
        public static readonly NamespacedId EMPTY = new();
        public readonly string Id {get;}
        public readonly string Namespace => Id.Split(":")[0];
        public readonly string Name => Id.Split(":")[1];

        public NamespacedId(string @namespace, string name){
            Id = $"{@namespace}:{name}";
        }
        public NamespacedId(string id){
            var parts = id.Split(":");
            if (parts.Length == 1){
                Debug.LogError($"missing namespace for ID {id}");
                Id = "default:"+id;
            }
            else {
                Id = id;
            }
        }

        public override readonly int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is NamespacedId id && this == id;
        }

        public static bool operator ==(NamespacedId id1, NamespacedId id2){
            return id1.Id == id2.Id;
        }

        public static bool operator !=(NamespacedId id1, NamespacedId id2){
            return id1.Id != id2.Id;
        }

        public override string ToString()
        {
            return Id;
        }

        public static NamespacedId operator /(NamespacedId id, string suffix){
            return new NamespacedId($"{id}/{suffix}");
        }
    }
}