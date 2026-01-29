using UnityEngine;
using LaunchPadBooster;
using System.Collections.Generic;
using Mono.Cecil;
using Assets.Scripts.Objects;
using JetBrains.Annotations;
using System;
using Assets.Scripts.Networking;

namespace Com.DipoleCat.ExtensionLib
{
    public class ExtensionLibMod : MonoBehaviour
    {
        public static readonly Mod MOD = new("com.dipolecat.ExtensionLibMod", "0.1.0");

        [UsedImplicitly]
        public void OnLoaded(List<GameObject> prefabs)
        {
            MOD.AddPrefabs(prefabs);

            

#if DEVELOPMENT_BUILD
            Debug.Log($"Loaded {prefabs.Count} prefabs");
#endif

        }
    }
}
