using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Atmospherics;
using Com.DipoleCat.ExtensionLib.Networking;
using static Assets.Scripts.Atmospherics.Chemistry;

namespace Com.DipoleCat.ExtensionLib.Atmospherics
{
    #nullable enable
    public static class VanillaMaterials {
        public static NamespacedId Oxygen => new NamespacedId("stationeers","oxygen");
        public static NamespacedId Nitrogen => new NamespacedId("stationeers","nitrogen");
        public static NamespacedId CarbonDioxide => new NamespacedId("stationeers","carbon_dioxide");
        public static NamespacedId Volatiles => new NamespacedId("stationeers","volatiles");
        public static NamespacedId Pollutant => new NamespacedId("stationeers","pollutant");
        public static NamespacedId NitrousOxide => new NamespacedId("stationeers","nitrous_oxide");

        public static NamespacedId Water => new NamespacedId("stationeers","water");
        public static NamespacedId PollutedWater => new NamespacedId("stationeers","polluted_water");

        public static GasType? GasType(NamespacedId phaseId){
            return phaseId.Id switch
            {
                "stationeers:oxygen/gas" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.Oxygen,
                "stationeers:oxygen/liquid" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidOxygen,
                "stationeers:nitrogen/gas" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.Nitrogen,
                "stationeers:nitrogen/liquid" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidNitrogen,
                "stationeers:carbon_dioxide/gas" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.CarbonDioxide,
                "stationeers:carbon_dioxide/liquid" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidCarbonDioxide,
                "stationeers:volatiles/gas" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.Volatiles,
                "stationeers:volatiles/liquid" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidVolatiles,
                "stationeers:pollutant/gas" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.Pollutant,
                "stationeers:pollutant/liquid" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidPollutant,
                "stationeers:nitrous_oxide/gas" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.NitrousOxide,
                "stationeers:nitrous_oxide/liquid" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidNitrousOxide,
                "stationeers:water/gas" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.Steam,
                "stationeers:water/liquid" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.Water,
                "stationeers:polluted_water/liquid" => (GasType?)Assets.Scripts.Atmospherics.Chemistry.GasType.PollutedWater,
                _ => null,
            };
        }

        public static NamespacedId? MaterialId(GasType gasType){
            return gasType switch
            {
                Assets.Scripts.Atmospherics.Chemistry.GasType.Oxygen or Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidOxygen => (NamespacedId?)Oxygen,
                Assets.Scripts.Atmospherics.Chemistry.GasType.Nitrogen or Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidNitrogen => (NamespacedId?)Nitrogen,
                Assets.Scripts.Atmospherics.Chemistry.GasType.CarbonDioxide or Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidCarbonDioxide => (NamespacedId?)CarbonDioxide,
                Assets.Scripts.Atmospherics.Chemistry.GasType.Volatiles or Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidVolatiles => (NamespacedId?)Volatiles,
                Assets.Scripts.Atmospherics.Chemistry.GasType.Pollutant or Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidPollutant => (NamespacedId?)Pollutant,
                Assets.Scripts.Atmospherics.Chemistry.GasType.NitrousOxide or Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidNitrousOxide => (NamespacedId?)NitrousOxide,
                Assets.Scripts.Atmospherics.Chemistry.GasType.Steam or Assets.Scripts.Atmospherics.Chemistry.GasType.Water => (NamespacedId?)Water,
                Assets.Scripts.Atmospherics.Chemistry.GasType.PollutedWater => (NamespacedId?)PollutedWater,
                _ => null,
            };
        }

        public static NamespacedId? PhaseId(GasType gasType){
            return gasType switch
            {
                Assets.Scripts.Atmospherics.Chemistry.GasType.Oxygen => Oxygen / "gas",
                Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidOxygen => Oxygen / "liquid",
                Assets.Scripts.Atmospherics.Chemistry.GasType.Nitrogen => Nitrogen / "gas",
                Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidNitrogen => Nitrogen / "liquid",
                Assets.Scripts.Atmospherics.Chemistry.GasType.CarbonDioxide => CarbonDioxide,
                Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidCarbonDioxide => CarbonDioxide / "liquid",
                Assets.Scripts.Atmospherics.Chemistry.GasType.Volatiles => Volatiles / "gas",
                Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidVolatiles => Volatiles / "liquid",
                Assets.Scripts.Atmospherics.Chemistry.GasType.Pollutant => Pollutant / "gas",
                Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidPollutant => Pollutant / "liquid",
                Assets.Scripts.Atmospherics.Chemistry.GasType.NitrousOxide => NitrousOxide / "gas",
                Assets.Scripts.Atmospherics.Chemistry.GasType.LiquidNitrousOxide => NitrousOxide / "liquid",
                Assets.Scripts.Atmospherics.Chemistry.GasType.Steam => Water / "gas",
                Assets.Scripts.Atmospherics.Chemistry.GasType.Water => Water / "liquid",
                Assets.Scripts.Atmospherics.Chemistry.GasType.PollutedWater => PollutedWater / "liquid",
                _ => null,
            };
        }

        internal static void RegisterVanillaMaterials(){
            Registries.CreateRegistry(Registries.MaterialRegistryId,new JsonSerializationCodec<IMaterialProperties>());
            Registries.CreateRegistry(Registries.PhaseRegistryId,new JsonSerializationCodec<IPhaseProperties>());

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
            Registries.Register(oxygen);

            var carbonDioxide = BuildVanilla(
                "carbon_dioxide",
                Chemistry.GasType.CarbonDioxide
            )
            .Build();
            Registries.Register(carbonDioxide);

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
            Registries.Register(volatiles);

            var nitrogen = BuildVanilla(
                "nitrogen",
                Chemistry.GasType.Nitrogen
            )
            .Build();
            Registries.Register(nitrogen);

            var pollutant = BuildVanilla(
                "pollutant",
                Chemistry.GasType.Pollutant
            )
            .Build();
            Registries.Register(pollutant);

            var water = BuildVanilla(
                "water",
                Chemistry.GasType.Steam
            )
            .Build();
            Registries.Register(water);

            //TODO: polluted water, changes materials on evaporation

            var nitrousOxide = BuildVanilla(
                "nitrous_oxide",
                Chemistry.GasType.NitrousOxide
            )
            .Build();
            Registries.Register(nitrousOxide);
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
    }
}