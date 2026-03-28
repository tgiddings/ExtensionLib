using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Com.Dipolecat.ExtensionLib.Preload
{
    public static class AtmosphericsPatcher{
        [UsedImplicitly]
        public static IEnumerable<string> TargetDLLs { get; } = ["Assembly-CSharp.dll"];

        [UsedImplicitly]
        public static void Patch(AssemblyDefinition assembly){
            var gasMixtureType = assembly.MainModule.GetType("Assets.Scripts.Atmospherics.GasMixture");
            Debug.Assert(gasMixtureType!=null,"Could not find GasMixture type");
            var moleType = assembly.MainModule.GetType("Assets.Scripts.Atmospherics.Mole");
            Debug.Assert(moleType!=null,"Could not find Mole type");

            var modPhasesField = new FieldDefinition(
                "modPhases",
                FieldAttributes.Public,
                assembly.MainModule
                    .ImportReference(typeof(Dictionary<,>))
                    .MakeGenericInstanceType(
                        assembly.MainModule.TypeSystem.UInt32,
                        moleType
                    )
            );
            gasMixtureType.Fields.Add(modPhasesField);

            var modPhasesFieldCtor = assembly.MainModule.ImportReference(modPhasesField.FieldType
                .Resolve()
                .GetConstructors()
                .Where(ctor => ctor.Parameters.Count == 0)
                .First());

            modPhasesFieldCtor.DeclaringType = modPhasesField.FieldType;

            var instanceCtors = gasMixtureType.Methods
                .Where(method => method.IsConstructor)
                .Where(method => !method.IsStatic);

            foreach(var ctor in instanceCtors)
            {
                var ilProcessor = ctor.Body.GetILProcessor();
                var lastInstruction = ilProcessor.Body.Instructions.Last();
                if(lastInstruction.OpCode == OpCodes.Ret) ilProcessor.Remove(lastInstruction);

                ilProcessor.Emit(OpCodes.Ldarg_0);
                ilProcessor.Emit(
                    OpCodes.Newobj,
                    assembly.MainModule.ImportReference(
                        modPhasesFieldCtor));
                ilProcessor.Emit(OpCodes.Stfld,modPhasesField);

                if(lastInstruction.OpCode == OpCodes.Ret) ilProcessor.Append(lastInstruction);
            }
        }
    }
}