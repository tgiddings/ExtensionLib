using Assets.Scripts.Atmospherics;
using System;
using System.Collections.Generic;

namespace Com.DipoleCat.ExtensionLib.Atmospherics
{
    public class VanillaMaterialBuilder{
        public NamespacedId Id {get;}
        public double MolarMass {get;}
        public double CondensedMolarVolume {get;}
        public SpecificHeat SpecificHeatCapacity { get; }
        public EvaporationCoefficients LiquidEvaporationCoefficients {get;}
        public SpecificHeat SpecificLatentHeatOfVaporization {get;}
        public TemperatureKelvin FreezingTemperature {get;}
        public TemperatureKelvin CriticalTemperature {get;}
        public PressurekPa MinCondensationPressure {get;}

        public VanillaFuelData? FuelData {get; private set;}

        public VanillaOxidizerData? OxidizerData {get; private set;}

        public VanillaMaterialBuilder(
            NamespacedId Id,
            double molarMass,
            double condensedMolarVolume,
            SpecificHeat specificHeatCapacity,
            EvaporationCoefficients liquidEvaporationCoefficients,
            SpecificHeat specificLatentHeatOfVaporization,
            TemperatureKelvin freezingTemperature,
            TemperatureKelvin criticalTemperature,
            PressurekPa minCondensationPressure
        ){
            this.Id = Id;
            MolarMass = molarMass;
            CondensedMolarVolume = condensedMolarVolume;
            SpecificHeatCapacity = specificHeatCapacity;
            LiquidEvaporationCoefficients = liquidEvaporationCoefficients;
            SpecificLatentHeatOfVaporization = specificLatentHeatOfVaporization;
            FreezingTemperature = freezingTemperature;
            CriticalTemperature = criticalTemperature;
            MinCondensationPressure = minCondensationPressure;
            FuelData = null;
        }

        public VanillaMaterialBuilder Fuel(
            MoleQuantity neededOxygen,
            IEnumerable<KeyValuePair<NamespacedId,MoleQuantity>> products,
            double reactionScale = 1.0
        )
        {
            if(OxidizerData.HasValue) throw new InvalidOperationException(
                "Hypergolic materials (materials which are both fuel and oxidizer) are not supported");
            FuelData = new VanillaFuelData(
                neededOxygen,
                products,
                reactionScale
            );
            return this;
        }

        public VanillaMaterialBuilder Oxidizer(
            MoleQuantity providedOxygen,
            IEnumerable<KeyValuePair<NamespacedId,MoleQuantity>> otherProducts,
            double reactionScale = 1.0
        )
        {
            if(FuelData.HasValue) throw new InvalidOperationException(
                "Hypergolic materials (materials which are both fuel and oxidizer) are not supported");
            OxidizerData = new VanillaOxidizerData(
                providedOxygen,
                otherProducts,
                reactionScale
            );
            return this;
        }

        public VanillaMaterialProperties Build(){
            return new VanillaMaterialProperties(
                Id,
                MolarMass,
                CondensedMolarVolume,
                SpecificHeatCapacity,
                LiquidEvaporationCoefficients,
                SpecificLatentHeatOfVaporization,
                FreezingTemperature,
                CriticalTemperature,
                MinCondensationPressure,
                FuelData,
                OxidizerData
            );
        }
    }

    public readonly struct PhaseTransition{
        public readonly IReadOnlyDictionary<NamespacedId,MoleQuantity> NewPhases {get;}
        public readonly VolumeLitres DeltaCondensedVolume {get;}
        public readonly MoleEnergy DeltaEnthalpy {get;}

        public readonly HeatCapacity DeltaCondensedHeatCapacity {get;}

        public PhaseTransition(
            IReadOnlyDictionary<NamespacedId,MoleQuantity> newPhases,
            VolumeLitres deltaCondensedVolume,
            MoleEnergy deltaEnthalpy,
            HeatCapacity deltaHeatCapacity
        ){
            NewPhases = new Dictionary<NamespacedId,MoleQuantity>(newPhases);
            DeltaCondensedVolume = deltaCondensedVolume;
            DeltaEnthalpy = deltaEnthalpy;
            DeltaCondensedHeatCapacity = deltaHeatCapacity;
        }

        public static PhaseTransition None => new (
            new Dictionary<NamespacedId,MoleQuantity>(),
            VolumeLitres.Zero,
            MoleEnergy.Zero,
            HeatCapacity.Zero);
    }

    public interface IMaterialProperties{
        public NamespacedId Id {get;}
        public IEnumerable<IPhaseProperties> Phases {get;}
        //TODO: can I make something more general?
        public VanillaFuelData? FuelData {get;}
        //TODO: can I make something more general?
        public VanillaOxidizerData? OxidizerData {get;}
        public IEnumerable<IPhaseProperties> AllowablePhases(TemperatureKelvin temperature, PressurekPa pressure);

        //TODO: superheating and supercooling could be supported by TransitionData.NewPhases containing phase.Id,
        //but the arguments here do not provide the context to determine is superheating and supercooling should
        //remain
        public PhaseTransition CorrectForbiddenPhase(
            IPhaseProperties forbiddenPhase,
            MoleQuantity forbiddenPhaseQuantity,
            TemperatureKelvin systemTemperature,
            PressurekPa systemPressureressure,
            HeatCapacity systemHeatCapacity);
    }
    public interface IPhaseProperties{
        NamespacedId Id{get;}
        NamespacedId MaterialId{get;}

        SpecificHeat SpecificHeatCapacity {get;}

        /// <summary>
        /// The density of the material in this phase at a given temperature and pressure.
        /// In units of grams per litre or, equivalently, kilograms per cubic meter.
        /// </summary>
        /// <remarks>
        /// For vanilla solids and liquids, this is a constant. For vanilla gases, this is
        /// computed using the ideal gas law
        /// </remarks>
        public double Density(TemperatureKelvin temperature, PressurekPa pressure);

        /// <summary>
        /// The molar volume (volume per mole) of the material in this phase at a given temperature and pressure.
        /// In units of litres per mole.
        /// </summary>
        /// <remarks>
        /// For vanilla solids and liquids, this is a constant. For vanilla gases, this is
        /// computed using the ideal gas law
        /// </remarks>
        public double MolarVolume(TemperatureKelvin temperature, PressurekPa pressure);
    }

    public interface ICondensedPhaseProperties: IPhaseProperties{
        public Dictionary<NamespacedId,EvaporationCoefficients> EvaporationCoeficients{get;}
    }

    public readonly struct IdealGasPhaseProperties: IPhaseProperties{

        NamespacedId MaterialId{get;}
        public NamespacedId Id => MaterialId / "gas";
        //g/mol
        double MolarMass{get;}
        public SpecificHeat SpecificHeatCapacity {get;}

        NamespacedId IPhaseProperties.MaterialId => MaterialId;

        public IdealGasPhaseProperties(
            NamespacedId materialId,
            double molarMass,
            SpecificHeat specificHeatCapacity)
        {
            MaterialId = materialId;
            MolarMass = molarMass;
            SpecificHeatCapacity = specificHeatCapacity;
        }

        //g/l or kg/m^3
        double IPhaseProperties.Density(TemperatureKelvin temperature, PressurekPa pressure)
        {
            return MolarMass/(this as IPhaseProperties).MolarVolume(temperature,pressure);
        }

        double IPhaseProperties.MolarVolume(TemperatureKelvin temperature, PressurekPa pressure)
        {
            return 8.314*temperature.ToDouble()/pressure.ToDouble();
        }
    }

    /// <summary>
    /// Coefficients for the Antoine equation: log P = A-B/(C+T),
    /// where P the vapor pressure in kPa and T is the temperature in Kelvin
    /// </summary>
    public readonly struct EvaporationCoefficients{
        public double A {get;}
        public double B {get;}
        public double C {get;}

        public EvaporationCoefficients(
            double a,
            double b,
            double c = 0
        ){
            this.A = a;
            this.B = b;
            this.C = c;
        }

        public PressurekPa VaporPressure(
            TemperatureKelvin temperature,
            double moleFraction = 1.0){
            return new PressurekPa(moleFraction*Math.Exp(A-(B/(C+temperature.ToDouble()))));
        }

        public TemperatureKelvin Inverse(
            PressurekPa pressure,
            double moleFraction = 1.0
        ){
            return new TemperatureKelvin(
                B/(A-Math.Log(pressure.ToDouble())/moleFraction)-C
            );
        }

        public static EvaporationCoefficients VanillaSolid(PressurekPa minPressure){
            return new EvaporationCoefficients(Math.Log(minPressure.ToDouble()),0,0);
        }
    }
}