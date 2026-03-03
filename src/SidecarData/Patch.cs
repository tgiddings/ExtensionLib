using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Networks;
using UnityEngine;

namespace Com.DipoleCat.ExtensionLib
{
    [HarmonyPatch]
    public static class SidecarSerializationPatch {
        [HarmonyPatch(typeof(XmlSaveLoad), nameof(XmlSaveLoad.LoadWorld))]
        [HarmonyPrefix]
        public static void XmlSaveLoad_LoadWorldPrefix()
        {
            SidecarData.Clear();
        }

        [HarmonyPatch(typeof(XmlSaveLoad), nameof(XmlSaveLoad.LoadThing))]
        [HarmonyPrefix]
        public static bool XmlSaveLoad_LoadThingPrefix(ThingSaveData thingData)
        {
            if(thingData is SidecarSerializable serializable){
                SidecarData.LoadFromSerializable(serializable);
                // SidecarSerializable is a fake thingsavedata for least-intrusive serialization.
                // Should not be turned into an actual thing
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(XmlSaveLoad), nameof(XmlSaveLoad.GetWorldData))]
        [HarmonyPostfix]
        public static void XmlSaveLoadGetWorldDataPostfix(XmlSaveLoad.WorldData __result)
        {
            if (__result is null)
                return;
            __result.OrderedThings.AddRange(SidecarData.GetSerializables());
        }

        [HarmonyPatch(typeof(XmlSaveLoad), nameof(XmlSaveLoad.AddExtraTypes))]
        [HarmonyPostfix]
        public static void XmlSaveLoad_AddExtraTypesPostfix(List<Type> extraTypes)
        {
            Debug.Assert(extraTypes != null);
            extraTypes!.Add(typeof(SidecarSerializable));
        }
    }

    /// <summary>
    /// Patches to detect when referenceables are removed from the game, and remove their sidecar data.
    /// This is only done reactively, not on world save/load, so that if future updates add new types of referenceables,
    /// sidecars for them will not be accidentally removed. Save bloat is better than data loss.
    /// </summary>
    [HarmonyPatch]
    public static class SidecarCleanupPatch{
        [HarmonyPatch(typeof(Referencable), nameof(Referencable.Deregister))]
        [HarmonyPostfix]
        public static void Referencable_Deregister(IReferencable iReferencable){
            SidecarData.RemoveAllForReferenceable(iReferencable.ReferenceId);
        }
    }
}