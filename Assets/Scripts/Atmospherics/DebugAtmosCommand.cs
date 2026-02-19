

using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using UnityEngine;
using Util.Commands;
using static Assets.Scripts.Atmospherics.Chemistry;

namespace Com.DipoleCat.ExtensionLib.Atmospherics
{
#nullable enable
    public class DebugAtmosCommand : CommandBase
    {
        public override string HelpText => 
            "debugatmos set\n"+
            "sets an atmosphere to a particular mixture\n"+
            "\tdebugatmos set pipe {pipenetwork} {phase} {quantity}... {phase} {quantity}\n"+
            "\tsets a pipe network to the given mixture. {phase} are namespaced IDs. "+
            "If the namespace is missing, stationeers will be tried first, and if there isn't a stationeers "+
            "phase with that name, will try to find exactly one mod phase with that name. "+
            "note that vanilla phase names are of the form {material}/{state}, such as \"water/gas\" "+
            "quantity defaults to moles. You can use the suffix L for litres (for liquid) or kPa for "+
            "kilopascals (for gas)";

        public override string[] Arguments => new[]{"subcommand"};

        public override bool IsLaunchCmd => false;

        private static readonly string temperatureKeyword = "temperature";
        private static readonly string pressureKeyword = "pressure";
        private static readonly string compositionKeyword = "composition";
        private static IEnumerable<string> Keywords => new List<string>(){temperatureKeyword,pressureKeyword,compositionKeyword};

        public override string Execute(string[] args)
        {
            Debug.LogWarning($"begin command: debugatmos {string.Join(" ",args)}");
            if(args.Length == 0){
                return HelpText;
            }
            var subcommand = args[0];
            if(subcommand == "set"){
                Debug.LogWarning($"subcommand set");
                ExecuteSet(args[1..]);
                return "";
            }
            return $"unrecognized subcommand {subcommand}";
        }

        private void ExecuteSet(string[] args){
            if(args.Length < 4 || args.Length % 2 == 1){
                Debug.LogWarning("wrong arg count");
                ConsoleWindow.Print(HelpText);
                return;
            }
            var scope = args[0];
            if(scope == "pipe"){
                Debug.LogWarning($"scope pipe");
                var networkArg = args[1];
                
                var network = ParsePipeNetwork(networkArg);

                var atmosphere = network.Atmosphere;
                Debug.LogWarning($"existing atmosphere: {atmosphere.Temperature.ToDouble()}K and {atmosphere.PressureGassesAndLiquids.ToDouble()}kPa ({atmosphere.GasMixture.DebugPrint()})");
                
                var composition = ParseComposition(
                    (args[2..] as IEnumerable<string>).GetEnumerator());

                var temperature = network.Atmosphere.Temperature;
                if(
                    temperature.Equals(TemperatureKelvin.Zero) &&
                    composition.quantities.Values.Any((q)=>q.unit == ParsedComposition.Unit.KILOPASCALS)
                )
                {
                    ConsoleWindow.Print(
                        $"pipe network {networkArg} has temperature 0K. gas pressure is ill-defined");
                    ConsoleWindow.Print(
                        $"setting temperature of pipe network {networkArg} to standard temperature (25C)");
                    temperature = TemperatureKelvin.FromCelsius(25f);
                }

                var replacementMixture = composition.Resolve(
                    temperature,
                    atmosphere.PressureGassesAndLiquids,
                    atmosphere.Volume
                );
                
                Debug.LogWarning($"replacement mixture is {replacementMixture.DebugPrint()}");
                Debug.LogWarning("setting replacement mixture");
                atmosphere.GasMixture = replacementMixture;
                Debug.LogWarning("done");
            }
            else if(scope == "room"){
                Debug.LogWarning($"scope room");
                var roomArg = args[1];
                
                var room = ParseRoom(roomArg);

                var composition = ParseComposition(
                    (args[2..] as IEnumerable<string>).GetEnumerator());

                foreach(var grid in room.Grids){
                    var atmosphere = AtmosphericsManager.Find(grid);
                    Debug.LogWarning(
                        $"existing atmosphere: {atmosphere.Temperature.ToDouble()}K and {atmosphere.PressureGassesAndLiquids.ToDouble()}kPa ({atmosphere.GasMixture.DebugPrint()})");
                    var temperature = atmosphere.Temperature;
                    if(
                        temperature.Equals(TemperatureKelvin.Zero) &&
                        composition.quantities.Values.Any((q)=>q.unit == ParsedComposition.Unit.KILOPASCALS)
                    )
                    {
                        ConsoleWindow.Print(
                            $"pipe network {roomArg} has temperature 0K. gas pressure is ill-defined");
                        ConsoleWindow.Print(
                            $"setting temperature of pipe network {roomArg} to standard temperature (25C)");
                        temperature = TemperatureKelvin.FromCelsius(25f);
                    }

                    var replacementMixture = composition.Resolve(
                        temperature,
                        atmosphere.PressureGassesAndLiquids,
                        atmosphere.Volume,
                        room.Grids.Count
                    );
                    
                    Debug.LogWarning($"replacement mixture is {replacementMixture.DebugPrint()}");
                    Debug.LogWarning($"setting replacement mixturef for grid {grid}");
                    atmosphere.GasMixture = replacementMixture;
                }

                Debug.LogWarning("done");
            }
        }

        private static PipeNetwork ParsePipeNetwork(string networkArg){
            if(!long.TryParse(networkArg,out long networkId)){
                throw new ParseException($"could not parse network id {networkArg}");
            }
            Debug.LogWarning($"finding network {networkId}");
            PipeNetwork? network = null;
            foreach(var candidateNetwork in PipeNetwork.AllPipeNetworks)
            {
                if (candidateNetwork.ReferenceId == networkId){
                    network = candidateNetwork;
                }
            }
            if(network == null){
                throw new ParseException($"could not find pipenetwork {networkId}");
            }
            Debug.LogWarning($"found network {networkId}");
            return network;
        }

        private static Room ParseRoom(string roomArg){
            if(!long.TryParse(roomArg,out long roomId)){
                throw new ParseException($"could not parse room id {roomArg}");
            }
            Debug.LogWarning($"finding network {roomId}");
            Room? room = null;
            foreach(var candidateRoom in Room.AllRooms)
            {
                if (candidateRoom.RoomId == roomId){
                    room = candidateRoom;
                }
            }
            if(room == null){
                throw new ParseException($"could not find room {roomId}");
            }
            Debug.LogWarning($"found room {roomId}");
            return room;
        }

        private static ParsedComposition ParseComposition(
            IEnumerator<string> compositionArgs
        )
        {
            var composition = new ParsedComposition();
            Debug.LogWarning($"parsing phases and quantities");
            while(compositionArgs.MoveNext()){
                var phaseName = compositionArgs.Current;
                Debug.LogWarning($"parsing phase {phaseName}");
                var phaseId = ResolvePhaseId(phaseName) ?? 
                    throw new ParseException($"could not unambiguously resolve phase {phaseName}");
                if(!compositionArgs.MoveNext()){
                    throw new ParseException($"no quantity given for phase {phaseId}");
                }
                var quantityString = compositionArgs.Current;
                Debug.LogWarning($"parsing quantity {quantityString}");
                var useLitres = false;
                var useKiloPascals = false;
                if(quantityString.ToLowerInvariant().EndsWith("l")){
                    useLitres = true;
                    quantityString = quantityString[..^1];
                }
                if(quantityString.ToLowerInvariant().EndsWith("kpa")){
                    useKiloPascals = true;
                    quantityString = quantityString[..^3];
                }
                if(!double.TryParse(quantityString, out double quantity)){
                    throw new ParseException($"could not parse quantity {quantityString}");
                }
                if(useLitres){
                    composition.quantities.Add(phaseId,new ParsedComposition.Quantity(){
                        value = quantity,
                        unit = ParsedComposition.Unit.LITRES
                    });
                }
                else if(useKiloPascals){
                    composition.quantities.Add(phaseId,new ParsedComposition.Quantity(){
                        value = quantity,
                        unit = ParsedComposition.Unit.KILOPASCALS
                    });
                }
                else {
                    composition.quantities.Add(phaseId,new ParsedComposition.Quantity(){
                        value = quantity,
                        unit = ParsedComposition.Unit.MOLES
                    });
                }
            }
            return composition;
        }

        private static TemperatureKelvin ParseTemperature(string temperatureArg){
            throw new NotImplementedException();
        }

        private static TemperatureKelvin ParsePressure(string temperatureArg){
            throw new NotImplementedException();
        }

        private static NamespacedId? ResolvePhaseId(string commandArgument){
            if(commandArgument.Contains(':')){
                return new NamespacedId(commandArgument);
            }
            NamespacedId stationeersId = new("stationeers",commandArgument);
            if(Registries.Phases.OrderedIds.Contains(stationeersId)){
                return stationeersId;
            }
            var candidates = new List<NamespacedId>();
            foreach(var id in Registries.Phases.OrderedIds){
                if(id.Name == commandArgument){
                    candidates.Add(id);
                }
            }
            if(candidates.Count == 1){
                return candidates.First();
            }
            return null;
        }

        private class ParseException: ArgumentException{
            internal ParseException(string cause): base(cause){

            }
        }

        private class ParsedComposition{
            internal Dictionary<NamespacedId,Quantity> quantities = new();
            internal enum Unit{
                MOLES,
                LITRES,
                KILOPASCALS
            }

            internal struct Quantity{
                internal double value;
                internal Unit unit;
            }

            internal GasMixture Resolve(
                TemperatureKelvin temperature,
                PressurekPa pressure,
                VolumeLitres volume,
                int atmosphereCount = 1
            )
            {
                var parsedMixture = new GasMixture(MoleQuantity.Zero);
                foreach ((var phaseId, var quantity) in quantities){
                    var phaseProperties = Registries.Phases.GetData(phaseId) ??
                        throw new ParseException($"could not unambiguously resolve phase {phaseId}");
                    Debug.LogWarning($"finding vanilla GasType for {phaseId}");
                    GasType? vanillaGasType = VanillaMaterials.GasType(phaseId);
                    if(vanillaGasType == null){
                        throw new ParseException($"non-vanilla phase {vanillaGasType} not yet implemented");
                    }
                    Debug.LogWarning($"creating Mole for {phaseId}({vanillaGasType})");
                    Mole mole = Mole.Create(vanillaGasType.Value);
                    if(quantity.unit == Unit.LITRES){
                        if(phaseProperties is not ICondensedPhaseProperties condensedProperties){
                            throw new ParseException(
                                $"phase {phaseId} had a quantity specified in litres, but is not a liquid");
                        }
                        Debug.LogWarning($"adding {quantity.value / atmosphereCount}L of {phaseId}");
                        mole.Quantity = new MoleQuantity(
                            quantity.value /
                            condensedProperties.MolarVolume(
                                temperature,
                                pressure
                            ) /
                            atmosphereCount);
                    }
                    else if (quantity.unit == Unit.KILOPASCALS){
                        if(phaseProperties is not IdealGasPhaseProperties){
                            throw new ParseException(
                                $"phase {phaseId} had a quantity specified in kPa, but is not a gas");
                        }
                        Debug.LogWarning($"adding {quantity.value}kPa of {phaseId}");
                        mole.Quantity = IdealGas.Quantity(
                            new PressurekPa(quantity.value),
                            volume,
                            temperature
                        );
                    }
                    else {
                        Debug.LogWarning($"adding {quantity.value / atmosphereCount}mol of {phaseId}");
                        mole.Quantity = new MoleQuantity(
                            quantity.value /
                            atmosphereCount);
                    }
                    Debug.LogWarning(
                        $"mole: {mole.Quantity.ToDouble()}mol of {mole.Type}");
                    Debug.LogWarning(
                        $"mole valid: {mole.IsValid}");
                    Debug.LogWarning($"adding mole for {phaseId} to replacement mixture");
                    parsedMixture.Add(mole);
                }

                parsedMixture.TotalEnergy = new MoleEnergy(
                    parsedMixture.HeatCapacity,
                    temperature
                );
                Debug.LogWarning($"mixture temperature: {parsedMixture.Temperature.ToDouble()}K ({parsedMixture.TotalEnergy})J");
                return parsedMixture;
            }
        }
    }
}