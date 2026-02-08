using Assets.Scripts.Atmospherics;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Com.DipoleCat.ExtensionLib.Atmospherics
{
    #nullable enable

    public readonly struct VanillaSolidPhaseProperties : ICondensedPhaseProperties{
        public NamespacedId Id{get;}
        public NamespacedId MaterialId{get;}

        public double Density {get;}
        public double MolarMass { get; }
        public double MolarVolume { get; }
        public SpecificHeat SpecificHeatCapacity {get;}

        Dictionary<NamespacedId, EvaporationCoefficients> ICondensedPhaseProperties.EvaporationCoeficients => new();

        public VanillaSolidPhaseProperties(
            NamespacedId materialId,
            double molarMass,
            double molarVolume,
            SpecificHeat specificHeatCapacity){
            Id = materialId / "liquid";
            MaterialId = materialId;
            MolarMass = molarMass;
            MolarVolume = molarVolume;
            SpecificHeatCapacity = specificHeatCapacity;
            Density = MolarMass/MolarVolume;
        }

        double IPhaseProperties.Density(TemperatureKelvin temperature, PressurekPa pressure)
        {
            return Density;
        }

        double IPhaseProperties.MolarVolume(TemperatureKelvin temperature, PressurekPa pressure)
        {
            return MolarVolume;
        }
    }
    public readonly struct VanillaLiquidPhaseProperties: ICondensedPhaseProperties{
        public NamespacedId MaterialId{get;}

        public NamespacedId Id {get;}
        public EvaporationCoefficients EvaporationCoeficients {get;}
        Dictionary<NamespacedId, EvaporationCoefficients> ICondensedPhaseProperties.EvaporationCoeficients => new(){
            {MaterialId / "gas", EvaporationCoeficients}
        };
        ///<summary> equivalent to Vanilla's MaxLiquidTemperature </summary>
        public TemperatureKelvin CriticalTemperature {get;}

        public double Density {get;}
        public double MolarMass {get;}
        public double MolarVolume {get;}
        public SpecificHeat SpecificHeatCapacity {get;}

        public VanillaLiquidPhaseProperties(
            NamespacedId materialId,
            EvaporationCoefficients evaporationCoeficients,
            TemperatureKelvin criticalTemperature,
            double molarMass,
            double molarVolume,
            SpecificHeat specificHeatCapacity
        ){
            Id = materialId / "liquid";
            MaterialId = materialId;
            EvaporationCoeficients = evaporationCoeficients;
            CriticalTemperature = criticalTemperature;
            MolarMass = molarMass;
            MolarVolume = molarVolume;
            SpecificHeatCapacity = specificHeatCapacity;
            Density = MolarMass/MolarVolume;
        }

        double IPhaseProperties.Density(TemperatureKelvin temperature, PressurekPa pressure)
        {
            return Density;
        }

        double IPhaseProperties.MolarVolume(TemperatureKelvin temperature, PressurekPa pressure)
        {
            return MolarVolume;
        }
    }

    public readonly struct VanillaMaterialProperties: IMaterialProperties{
        public NamespacedId Id {get;}
        ///<summary> Molar Mass, in units of grams per mole </summary>
        public double MolarMass {get;}
        public double LiquidMolarVolume {get;}
        public double SolidMolarVolume =>LiquidMolarVolume;
        public SpecificHeat SpecificHeatCapacity {get;}
        public VanillaFuelData? FuelData{get;}
        public VanillaOxidizerData? OxidizerData{get;}
        public NamespacedId GasPhaseId => Id / "gas";
        public NamespacedId LiquidPhaseId => Id / "liquid";
        public NamespacedId SolidPhaseId => Id / "solid";

        IEnumerable<IPhaseProperties> IMaterialProperties.Phases => new List<IPhaseProperties>(){
            SolidPhaseData,
            LiquidPhaseData,
            GasPhaseData
        };

        public VanillaLiquidPhaseProperties LiquidPhaseData => new (
            Id,
            EvaporationCoefficients,
            CriticalTemperature,
            MolarMass,
            LiquidMolarVolume,
            SpecificHeatCapacity);
        public VanillaSolidPhaseProperties SolidPhaseData => new(
            Id,
            MolarMass,
            SolidMolarVolume,
            SpecificHeatCapacity);
        public IdealGasPhaseProperties GasPhaseData => new(
            Id,
            MolarMass,
            SpecificHeatCapacity);
        public EvaporationCoefficients EvaporationCoefficients{get;}
        /// <summary>
        /// The temperature at which the liquid phase freezes. Invariant of pressure for vanilla materials
        /// </summary>
        public TemperatureKelvin FreezingTemperature {get;}
        /// <summary>
        /// The temperature at which the liquid phase boils at 100kPA. Used only for the stationpedia.
        /// </summary>
        public TemperatureKelvin BoilingTemperature {get;}
        /// <summary>
        /// The temperature of the critical point. In real life, this is the point where the distinction
        /// between the liquid and gas phase is lost. In Stationeers, this is the highest temperature
        /// at which a liquid can exist -- automatically boiling regardless of pressure.
        /// Equivalent to vanilla <see cref="Mole.MaxLiquidTemperature()"/>
        /// </summary>
        public TemperatureKelvin CriticalTemperature {get;}

        /// <summary>
        /// In Stationeers, unlike in reality, a vanilla material cannot be solid or liquid above a particular
        /// pressure. For a vanilla material, this is equal to <see cref="TriplePointPressure"/>
        /// Equivalent to <see cref="Mole.MinLiquidPressure()"/>
        /// </summary>
        public PressurekPa MinCondensationPressure {get;}

        /// <Summary>
        /// The Specific Latent Heat of Vaporization for the material, measured in kiloJoules per mole.
        /// This is the amount of energy needed to convert a mole of liquid to a mole of gas, without
        /// changing the temperature or pressure. 
        /// Note that tables of Specific Latent Heat usually use units of kiloJoules per kilogram, and must
        /// be converted.
        /// Equivalent to vanilla <see cref="Mole.LatentHeatOfVaporization()"/>
        /// </summary>
        public SpecificHeat SpecificLatentHeatOfVaporization {get;}

        /// <Summary>
        /// The Specific Latent Heat of Fusion for the material, measured in kiloJoules per mole.
        /// This is the amount of energy needed to convert a mole of solid to a mole of liquid, without
        /// changing the temperature or pressure. 
        /// Note that tables of Specific Latent Heat usually use units of kiloJoules per kilogram, and must
        /// be converted.
        /// Equivalent to vanilla <see cref="Mole.LatentHeatOfFusion"/>
        /// </summary>
        public SpecificHeat SpecificLatentHeatOfFusion => 
            SpecificLatentHeatOfVaporization/Mole.FUSION_TO_VAPORIZATION_LATENT_HEAT_DENOMINATOR;

        /// <Summary>
        /// The Specific Latent Heat of Sublimation for the material, measured in kiloJoules per mole.
        /// This is the amount of energy needed to convert a mole of solid to a mole of gas, without
        /// changing the temperature or pressure.
        /// Calculated as <see cref="SpecificLatentHeatOfVaporization"/>+<see cref="SpecificLatentHeatOfFusion"/>.
        /// Note that tables of Specific Latent Heat usually use units of kiloJoules per kilogram, and must
        /// be converted.
        /// </summary>
        public SpecificHeat SpecificLatentHeatOfSublimation => 
            SpecificLatentHeatOfVaporization+SpecificLatentHeatOfFusion;

        public PressurekPa TriplePointPressure => MinCondensationPressure;
        public TemperatureKelvin TriplePointTemperature {get;}

        public VanillaMaterialProperties(
            NamespacedId id,
            double molarMass,
            double condensedMolarVolume,
            SpecificHeat specificHeatCapacity,
            EvaporationCoefficients evaporationCoefficients,
            SpecificHeat specificLatentHeatOfVaporization,
            TemperatureKelvin freezingTemperature,
            TemperatureKelvin criticalTemperature,
            PressurekPa minCondensationPressure,
            VanillaFuelData? fuelData = null,
            VanillaOxidizerData? oxidizerData = null
        )
        {
            if(fuelData.HasValue && oxidizerData.HasValue) {
                //TODO: is there an easy way to allow this?
                throw new ArgumentException(
                    "Hypergolic materials (materials which are both fuel and oxidizer) are not supported");
            }
            Id = id;
            MolarMass = molarMass;
            LiquidMolarVolume = condensedMolarVolume;
            SpecificHeatCapacity = specificHeatCapacity;
            EvaporationCoefficients = evaporationCoefficients;
            SpecificLatentHeatOfVaporization = specificLatentHeatOfVaporization;
            FreezingTemperature = freezingTemperature;
            CriticalTemperature = criticalTemperature;
            BoilingTemperature = EvaporationCoefficients.Inverse(new PressurekPa(100));
            MinCondensationPressure = minCondensationPressure;
            FuelData = fuelData;
            OxidizerData = oxidizerData;
            TriplePointTemperature = EvaporationCoefficients.Inverse(MinCondensationPressure);
        }

        public IEnumerable<IPhaseProperties> AllowablePhases(TemperatureKelvin temperature, PressurekPa pressure)
        {
            if (pressure < MinCondensationPressure){
                return new List<IPhaseProperties>(){
                    GasPhaseData
                };
            }
            else if (temperature < FreezingTemperature){
                return new List<IPhaseProperties>(){
                    SolidPhaseData
                };
            }
            else if (EvaporationCoefficients.VaporPressure(temperature) < pressure){
                return new List<IPhaseProperties>(){
                    LiquidPhaseData
                };
            }
            else return new List<IPhaseProperties>(){
                GasPhaseData
            };
        }

        /// <summary>
        /// Decides how to resolve the presence of a phase which is forbidden at this temperature and pressure.
        /// <paramref name="forbiddenPhase"/> must be a phase belonging to this material and not present in
        /// <c>AllowablePhases(temperature, pressure)</c>
        /// </summary>
        public PhaseTransition CorrectForbiddenPhase(
            IPhaseProperties forbiddenPhase,
            MoleQuantity forbiddenPhaseQuantity,
            TemperatureKelvin systemTemperature,
            PressurekPa systenPressure,
            HeatCapacity systemHeatCapacity)
        {
            if(forbiddenPhase is VanillaLiquidPhaseProperties){
                if (systenPressure < MinCondensationPressure){
                    //evaporate liquid completely
                    return EvaporateTransition(
                        forbiddenPhaseQuantity,
                        MoleQuantity.Zero
                    );
                }
                else if (systemTemperature > CriticalTemperature){
                    //evaporate liquid
                    throw new NotImplementedException();
                }
                else if (systemTemperature > FreezingTemperature){
                    //evaporate liquid
                    if (systenPressure < EvaporationCoefficients.VaporPressure(systemTemperature)){
                        Debug.LogError(
                            $"CorrectForbiddenPhase: liquid phase {forbiddenPhase}({forbiddenPhase.Id}) "+
                            $"is not forbidden at {systemTemperature.ToDouble()}K, {systenPressure.ToDouble()}kPa "+
                            $"in material {this}({Id})");
                        return PhaseTransition.None;
                    }
                    throw new NotImplementedException();
                }
                else {
                    //freeze liquid
                    throw new NotImplementedException();
                }
            }
            else if(forbiddenPhase is VanillaSolidPhaseProperties){
                if(systenPressure < MinCondensationPressure){
                    //sublimate solid completely
                    return SublimateTransition(
                        forbiddenPhaseQuantity,
                        MoleQuantity.Zero
                    );
                }
                else if (systemTemperature > CriticalTemperature){
                    //sublimate solid
                    throw new NotImplementedException();
                }
                else if (systemTemperature > FreezingTemperature){
                    //melt or sublimate solid
                    if (systenPressure > EvaporationCoefficients.VaporPressure(systemTemperature)){
                        //sublimate solid
                        throw new NotImplementedException();
                    }
                    else {
                        //melt solid
                        throw new NotImplementedException();
                    }
                }
                else {
                    Debug.LogError(
                        $"CorrectForbiddenPhase: solid phase {forbiddenPhase}({forbiddenPhase.Id}) "+
                        $"is not forbidden at {systemTemperature.ToDouble()}K, {systenPressure.ToDouble()}kPa "+
                        $"in material {this}({Id})");
                    return PhaseTransition.None;
                }
            }
            else if(forbiddenPhase is IdealGasPhaseProperties){
                if (systemTemperature > FreezingTemperature){
                    //condense gas
                    if (systenPressure > EvaporationCoefficients.VaporPressure(systemTemperature)){
                        Debug.LogError(
                            $"CorrectForbiddenPhase: gas phase {forbiddenPhase}({forbiddenPhase.Id}) "+
                            $"is not forbidden at {systemTemperature.ToDouble()}K, {systenPressure.ToDouble()}kPa "+
                            $"in material {this}({Id})");
                        return PhaseTransition.None;
                    }
                    throw new NotImplementedException();
                }
                else {
                    //deposit gas 
                    if (systenPressure < MinCondensationPressure){
                        Debug.LogError(
                            $"CorrectForbiddenPhase: gas phase {forbiddenPhase}({forbiddenPhase.Id}) "+
                            $"is not forbidden at {systemTemperature.ToDouble()}K, {systenPressure.ToDouble()}kPa "+
                            $"in material {this}({Id})");
                        return PhaseTransition.None;
                    }
                    //deposit gas completely
                    return DepositTransition(
                        forbiddenPhaseQuantity,
                        MoleQuantity.Zero
                    );
                }
            }
            else {
                Debug.LogError($"CorrectForbiddenPhase: phase {forbiddenPhase}({forbiddenPhase.Id}) does not belong to material {this}({Id})");
                return PhaseTransition.None;
            }
        }

        private PhaseTransition MeltTransition(
            MoleQuantity meltedQuantity,
            MoleQuantity residualQuantity
        ){
            if(meltedQuantity <= MoleQuantity.Zero){
                throw new ArgumentOutOfRangeException($"meltedQuantity must be positive. Got {meltedQuantity}");
            }
            if(residualQuantity < MoleQuantity.Zero){
                throw new ArgumentOutOfRangeException($"residualQuantity must be positive or zero. Got {meltedQuantity}");
            }
            Dictionary<NamespacedId,MoleQuantity> newPhases = new(2){
                {LiquidPhaseId,meltedQuantity}
            };
            if(residualQuantity>MoleQuantity.Zero) newPhases.Add(
                SolidPhaseId,
                residualQuantity
            );

            return new PhaseTransition(
                newPhases,
                VolumeLitres.Zero,
                new MoleEnergy(meltedQuantity,SpecificLatentHeatOfFusion.ToDouble()),
                HeatCapacity.Zero
            );
        }

        private PhaseTransition FreezeTransition(
            MoleQuantity frozenQuantity,
            MoleQuantity residualQuantity
        ){
            if(frozenQuantity <= MoleQuantity.Zero){
                throw new ArgumentOutOfRangeException($"frozenQuantity must be positive. Got {frozenQuantity}");
            }
            if(residualQuantity < MoleQuantity.Zero){
                throw new ArgumentOutOfRangeException($"residualQuantity must be positive or zero. Got {frozenQuantity}");
            }
            Dictionary<NamespacedId,MoleQuantity> newPhases = new(2){
                {SolidPhaseId,frozenQuantity}
            };
            if(residualQuantity>MoleQuantity.Zero) newPhases.Add(
                LiquidPhaseId,
                residualQuantity
            );

            return new PhaseTransition(
                newPhases,
                VolumeLitres.Zero,
                new MoleEnergy(frozenQuantity,-SpecificLatentHeatOfFusion.ToDouble()),
                HeatCapacity.Zero
            );
        }

        private PhaseTransition EvaporateTransition(
            MoleQuantity evaporatedQuantity,
            MoleQuantity residualQuantity
        ){
            if(evaporatedQuantity <= MoleQuantity.Zero){
                throw new ArgumentOutOfRangeException($"evaporatedQuantity must be positive. Got {evaporatedQuantity}");
            }
            if(residualQuantity < MoleQuantity.Zero){
                throw new ArgumentOutOfRangeException($"residualQuantity must be positive or zero. Got {evaporatedQuantity}");
            }
            Dictionary<NamespacedId,MoleQuantity> newPhases = new(2){
                {GasPhaseId,evaporatedQuantity}
            };
            if(residualQuantity>MoleQuantity.Zero) newPhases.Add(
                LiquidPhaseId,
                residualQuantity
            );

            return new PhaseTransition(
                newPhases,
                new VolumeLitres(-evaporatedQuantity.ToDouble()*LiquidMolarVolume),
                new MoleEnergy(evaporatedQuantity,SpecificLatentHeatOfVaporization.ToDouble()),
                HeatCapacity.Zero
            );
        }

        private PhaseTransition CondenseTransition(
            MoleQuantity condensedQuantity,
            MoleQuantity residualQuantity
        ){
            if(condensedQuantity <= MoleQuantity.Zero){
                throw new ArgumentOutOfRangeException($"condensedQuantity must be positive. Got {condensedQuantity}");
            }
            if(residualQuantity < MoleQuantity.Zero){
                throw new ArgumentOutOfRangeException($"residualQuantity must be positive or zero. Got {condensedQuantity}");
            }
            Dictionary<NamespacedId,MoleQuantity> newPhases = new(2){
                {LiquidPhaseId,condensedQuantity}
            };
            if(residualQuantity>MoleQuantity.Zero) newPhases.Add(
                GasPhaseId,
                residualQuantity
            );

            return new PhaseTransition(
                newPhases,
                new VolumeLitres(condensedQuantity.ToDouble()*LiquidMolarVolume),
                new MoleEnergy(-condensedQuantity,SpecificLatentHeatOfVaporization.ToDouble()),
                HeatCapacity.Zero
            );
        }

        private PhaseTransition SublimateTransition(
            MoleQuantity sublimatedQuantity,
            MoleQuantity residualQuantity
        ){
            if(sublimatedQuantity <= MoleQuantity.Zero){
                throw new ArgumentOutOfRangeException($"sublimatedQuantity must be positive. Got {sublimatedQuantity}");
            }
            if(residualQuantity < MoleQuantity.Zero){
                throw new ArgumentOutOfRangeException($"residualQuantity must be positive or zero. Got {sublimatedQuantity}");
            }
            Dictionary<NamespacedId,MoleQuantity> newPhases = new(2){
                {GasPhaseId,sublimatedQuantity}
            };
            if(residualQuantity>MoleQuantity.Zero) newPhases.Add(
                SolidPhaseId,
                residualQuantity
            );

            return new PhaseTransition(
                newPhases,
                new VolumeLitres(-sublimatedQuantity.ToDouble()*SolidMolarVolume),
                new MoleEnergy(sublimatedQuantity,SpecificLatentHeatOfSublimation.ToDouble()),
                HeatCapacity.Zero
            );
        }

        private PhaseTransition DepositTransition(
            MoleQuantity depositedQuantity,
            MoleQuantity residualQuantity
        ){
            if(depositedQuantity <= MoleQuantity.Zero){
                throw new ArgumentOutOfRangeException($"depositedQuantity must be positive. Got {depositedQuantity}");
            }
            if(residualQuantity < MoleQuantity.Zero){
                throw new ArgumentOutOfRangeException($"residualQuantity must be positive or zero. Got {depositedQuantity}");
            }
            Dictionary<NamespacedId,MoleQuantity> newPhases = new(2){
                {SolidPhaseId,depositedQuantity}
            };
            if(residualQuantity>MoleQuantity.Zero) newPhases.Add(
                GasPhaseId,
                residualQuantity
            );

            return new PhaseTransition(
                newPhases,
                new VolumeLitres(depositedQuantity.ToDouble()*SolidMolarVolume),
                new MoleEnergy(-depositedQuantity,SpecificLatentHeatOfSublimation.ToDouble()),
                HeatCapacity.Zero
            );
        }
    }

    

    public readonly struct VanillaFuelData{
        public readonly MoleQuantity NeededOxygen {get;}
        public readonly IReadOnlyDictionary<NamespacedId,MoleQuantity> Products {get;}
        public readonly double ReactionScale {get;}

        public VanillaFuelData(
            MoleQuantity neededOxygen,
            IEnumerable<KeyValuePair<NamespacedId,MoleQuantity>> products,
            double reactionScale
        ){
            NeededOxygen = neededOxygen;
            Products = new Dictionary<NamespacedId,MoleQuantity>(products);
            ReactionScale = reactionScale;
        }
    }
    public readonly struct VanillaOxidizerData{
        public readonly MoleQuantity ProvidedOxygen {get;}
        public readonly IReadOnlyDictionary<NamespacedId,MoleQuantity> OtherProducts {get;}
        public readonly double ReactionScale {get;}

        public VanillaOxidizerData(
            MoleQuantity providedOxygen,
            IEnumerable<KeyValuePair<NamespacedId,MoleQuantity>> otherProducts,
            double reactionScale
        ){
            ProvidedOxygen = providedOxygen;
            OtherProducts = new Dictionary<NamespacedId,MoleQuantity>(otherProducts);
            ReactionScale = reactionScale;
        }
    }
}