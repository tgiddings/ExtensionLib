using UnityEngine;
using LaunchPadBooster;
using System.Collections.Generic;

namespace ExtensionLib
{
    public class ExtensionLib : MonoBehaviour
    {
        public static readonly Mod MOD = new("ExtensionLib", "0.1.0");

        public void OnLoaded(List<GameObject> prefabs)
        {
            MOD.AddPrefabs(prefabs);

#if DEVELOPMENT_BUILD
            Debug.Log($"Loaded {prefabs.Count} prefabs");
#endif

        }
    }
}
