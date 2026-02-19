using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networking;
using Com.DipoleCat.ExtensionLib.Atmospherics;
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

        public static IRegistry<IMaterialProperties> Materials => 
            GetRegistry<IMaterialProperties>(MaterialRegistryId)!;
        public static IRegistry<IPhaseProperties> Phases => 
            GetRegistry<IPhaseProperties>(PhaseRegistryId)!;

        public static NamespacedId MaterialRegistryId => new("extensionlib:materials");
        public static NamespacedId PhaseRegistryId => new("extensionlib:phases");

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
            if(IsAuthoritative){
                return local_registries[registryId];
            }
            else return synced_registries[registryId];
        }

        public static IRegistry<T>? GetRegistry<T>(NamespacedId registryId)
            where T: class
        {
            return GetRegistry(registryId) as IRegistry<T>;
        }

        public static IRegistry CreateRegistry(NamespacedId registryId){
            var registry = new SimpleRegistry();
            local_registries[registryId] = registry;
            return registry;
        }

        public static IMutableRegistry<T> CreateRegistry<T>(NamespacedId registryId, INetworkCodec<T> codec)
            where T: class
        {
            var registry = new SimpleRegistry<T>(codec);
            local_registries[registryId] = registry;
            codecs[registryId] = codec;
            return registry;
        }

        public static void Register(IMaterialProperties material, bool registerPhases = true){
            if (Materials is IMutableRegistry<IMaterialProperties> mutable){
                mutable.Register(material.Id,material);
                if(registerPhases){
                    RegisterAll(material.Phases);
                }
            }
            else throw new InvalidOperationException("materials registry is frozen (synced from connected server)");
        }

        public static void Register(IPhaseProperties phase){
            if (Phases is IMutableRegistry<IPhaseProperties> mutable){
                mutable.Register(phase.Id,phase);
            }
            else throw new InvalidOperationException("phases registry is frozen (synced from connected server)");
        }

        public static void RegisterAll(IEnumerable<IPhaseProperties> phases){
            foreach (var phase in phases){
                Register(phase);
            }
        }

        private static EvaporationCoefficients GetVanillaLiquidCoefficients(
            Chemistry.GasType gasType
        )
        {
            var antoineAMethod = typeof(MoleHelper).GetMethod(
                "EvaporationCoefficientA",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            var antoineBMethod = typeof(MoleHelper).GetMethod(
                "EvaporationCoefficientB",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            return new EvaporationCoefficients(
                (double)antoineAMethod.Invoke(null, new object[]{gasType}),
                (double)antoineBMethod.Invoke(null, new object[]{gasType})
            );
        }

        private static VanillaMaterialBuilder BuildVanilla(
            string name,
            Chemistry.GasType gasType
        ){
            var liquidType = MoleHelper.CondensationType(gasType);
            return new VanillaMaterialBuilder(
                new NamespacedId("stationeers",name),
                Mole.MolarMass(gasType),
                Mole.MolarVolume(liquidType).ToDouble(),
                new SpecificHeat(Chemistry.SpecificHeat(gasType)),
                GetVanillaLiquidCoefficients(liquidType),
                new SpecificHeat(Mole.LatentHeatOfVaporization(gasType)),
                Mole.FreezingTemperature(gasType),
                Mole.MaxLiquidTemperature(gasType),
                Mole.MinLiquidPressure(gasType)
            );
        }

        static Registries(){
            CreateRegistry(MaterialRegistryId,new JsonSerializationCodec<IMaterialProperties>());
            CreateRegistry(PhaseRegistryId,new JsonSerializationCodec<IPhaseProperties>());

            //materials and their phases
            var oxygen = BuildVanilla(
                "oxygen",
                Chemistry.GasType.Oxygen
            )
            .Oxidizer(
                MoleQuantity.One,
                new List<KeyValuePair<NamespacedId,MoleQuantity>>(){
                    new (new NamespacedId("stationeers","pollutant"), MoleQuantity.One),
                    new (new NamespacedId("stationeers","carbon_dioxide"), MoleQuantity.One)
                }
            )
            .Build();
            Register(oxygen);

            var carbonDioxide = BuildVanilla(
                "carbon_dioxide",
                Chemistry.GasType.CarbonDioxide
            )
            .Build();
            Register(carbonDioxide);

            var volatiles = BuildVanilla(
                "volatiles",
                Chemistry.GasType.Volatiles
            )
            .Fuel(
                MoleQuantity.One,
                new List<KeyValuePair<NamespacedId,MoleQuantity>>(){
                    new (new NamespacedId("stationeers","carbon_dioxide"), new MoleQuantity(2.0))
                }
            )
            .Build();
            Register(volatiles);

            var nitrogen = BuildVanilla(
                "nitrogen",
                Chemistry.GasType.Nitrogen
            )
            .Build();
            Register(nitrogen);

            var pollutant = BuildVanilla(
                "pollutant",
                Chemistry.GasType.Pollutant
            )
            .Build();
            Register(pollutant);

            var water = BuildVanilla(
                "water",
                Chemistry.GasType.Steam
            )
            .Build();
            Register(water);

            //TODO: polluted water, changes materials on evaporation

            var nitrousOxide = BuildVanilla(
                "nitrous_oxide",
                Chemistry.GasType.NitrousOxide
            )
            .Build();
            Register(nitrousOxide);
        }
    }
}