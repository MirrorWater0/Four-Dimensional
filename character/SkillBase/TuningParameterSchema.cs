using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Godot;

// Read parameter references from the compiled code, including callbacks and async methods.
// Do not execute battle effects or require C# source files in an exported game.
public static class TuningParameterSchema {
    public sealed class Parameters {
        public HashSet<string> Keys { get; } = new(StringComparer.Ordinal);
        public bool CanRemoveObsolete { get; internal set; } = true;
    }

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null))
        .GroupBy(opCode => opCode.Value)
        .ToDictionary(group => group.Key, group => group.First());
    private static readonly Dictionary<Type, Parameters> CardParameters = new();
    private static readonly Lazy<Dictionary<string, Parameters>> EnemyParameters = new(BuildEnemyParameters);

    public static Parameters GetCard(string key) {
        if (!Enum.TryParse<SkillID>(key, out var id))
            return null;
        Type implementation = Skill.GetTuningImplementationType(id);
        if (implementation == null)
            return null;
        if (CardParameters.TryGetValue(implementation, out var cached))
            return cached;

        var parameters = new Parameters();
        parameters.Keys.UnionWith(new[] {
            "EnergyCost", "EnemyEnergyCost", "ExhaustAfterExecutions",
        });
        for (Type type = implementation; type != null && type != typeof(Skill); type = type.BaseType) {
            if (!Scan(type, (method, instructions, index, called) => {
                if (called.DeclaringType != typeof(Skill) || called.Name != "V")
                    return;
                string parameter = GetString(method, instructions, index - 1, index);
                if (parameter == null)
                    parameters.CanRemoveObsolete = false;
                else
                    parameters.Keys.Add(parameter);
            }))
                parameters.CanRemoveObsolete = false;
        }
        if (!parameters.CanRemoveObsolete)
            GD.PushWarning($"[Tuning] {key} 存在无法静态识别的参数引用，保留其已有字段以免丢失数值。请使用 V(\"参数名\") 或 V(nameof(参数名))。");
        CardParameters.Add(implementation, parameters);
        return parameters;
    }

    public static Parameters GetEnemy(string key) {
        return EnemyParameters.Value.GetValueOrDefault(key);
    }

    private static Dictionary<string, Parameters> BuildEnemyParameters() {
        var schemas = EnemyCatalog.Keys.ToDictionary(key => key, key => new Parameters(), StringComparer.OrdinalIgnoreCase);
        foreach (var parameters in schemas.Values)
            parameters.Keys.UnionWith(new[] {
                "MaxLife", "Power", "Survivability", "BasePowerContribution", "BaseSurvivabilityContribution",
            });
        bool complete = true;
        foreach (Type type in typeof(EnemyRegedit).Assembly.GetTypes().Where(type => !type.IsNested)) {
            if (type == typeof(EnemyTuning))
                continue;
            if (!Scan(type, (method, instructions, index, called) => {
                if (called.DeclaringType != typeof(EnemyTuning) || called.Name != nameof(EnemyTuning.GetRequiredInt))
                    return;
                // ReadStat uses dynamic names for the five common fields included above.
                if (method.DeclaringType == typeof(EnemyRegedit) && method.Name == "ReadStat")
                    return;
                string enemy = GetString(method, instructions, index - 2, index);
                string parameter = GetString(method, instructions, index - 1, index);
                if (enemy == null || parameter == null) {
                    complete = false;
                    return;
                }
                if (!schemas.TryGetValue(enemy, out var parameters)) {
                    parameters = new Parameters();
                    schemas.Add(enemy, parameters);
                }
                parameters.Keys.Add(parameter);
            }))
                complete = false;
        }
        foreach (var parameters in schemas.Values)
            parameters.CanRemoveObsolete = complete;
        if (!complete)
            GD.PushWarning("[Tuning] 敌人参数引用未能完整识别，保留已有字段以免丢失数值。请使用常量敌人 ID 和参数名。");
        return schemas;
    }

    private readonly record struct Instruction(OpCode OpCode, int Token, int Offset, bool IsBranchTarget = false);

    private static string GetString(MethodBase method, List<Instruction> instructions, int index, int callIndex) {
        // A ternary/switch can load a different string on another incoming branch.
        if (index < 0 || instructions[index].OpCode != OpCodes.Ldstr
            || instructions.Skip(index + 1).Take(callIndex - index).Any(instruction => instruction.IsBranchTarget))
            return null;
        return method.Module.ResolveString(instructions[index].Token);
    }

    private static bool Scan(Type type, Action<MethodBase, List<Instruction>, int, MethodBase> visit) {
        const BindingFlags flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static;
        bool complete = true;
        foreach (MethodBase method in type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags))) {
            try {
                byte[] code = method.GetMethodBody()?.GetILAsByteArray();
                if (code == null) {
                    // Runtime-provided delegate methods and native entry points contain no managed references.
                    if (!method.IsAbstract && (method.Attributes & MethodAttributes.PinvokeImpl) == 0
                        && (method.GetMethodImplementationFlags() & (MethodImplAttributes.Runtime | MethodImplAttributes.InternalCall)) == 0)
                        complete = false;
                    continue;
                }
                var instructions = new List<Instruction>();
                var branchTargets = new HashSet<int>();
                for (int offset = 0; offset < code.Length;) {
                    int instructionOffset = offset;
                    short value = code[offset++];
                    if (value == 0xfe)
                        value = unchecked((short)(0xfe00 | code[offset++]));
                    OpCode opCode = OpCodesByValue[value];
                    int token = opCode.OperandType is OperandType.InlineMethod or OperandType.InlineString
                        ? BitConverter.ToInt32(code, offset) : 0;
                    int size = opCode.OperandType switch {
                        OperandType.InlineNone => 0,
                        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                        OperandType.InlineVar => 2,
                        OperandType.InlineI8 or OperandType.InlineR => 8,
                        OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(code, offset),
                        _ => 4,
                    };
                    if (opCode.OperandType == OperandType.ShortInlineBrTarget)
                        branchTargets.Add(offset + size + unchecked((sbyte)code[offset]));
                    else if (opCode.OperandType == OperandType.InlineBrTarget)
                        branchTargets.Add(offset + size + BitConverter.ToInt32(code, offset));
                    else if (opCode.OperandType == OperandType.InlineSwitch) {
                        for (int entry = offset + 4; entry < offset + size; entry += 4)
                            branchTargets.Add(offset + size + BitConverter.ToInt32(code, entry));
                    }
                    offset += size;
                    instructions.Add(new Instruction(opCode, token, instructionOffset));
                }
                for (int index = 0; index < instructions.Count; index++)
                    instructions[index] = instructions[index] with { IsBranchTarget = branchTargets.Contains(instructions[index].Offset) };
                for (int index = 0; index < instructions.Count; index++) {
                    OpCode opCode = instructions[index].OpCode;
                    if (opCode != OpCodes.Call && opCode != OpCodes.Callvirt
                        && opCode != OpCodes.Ldftn && opCode != OpCodes.Ldvirtftn)
                        continue;
                    MethodBase called = method.Module.ResolveMethod(instructions[index].Token, type.GetGenericArguments(),
                        method.IsGenericMethod ? method.GetGenericArguments() : null);
                    if (called != null)
                        visit(method, instructions, index, called);
                }
            } catch (Exception e) {
                complete = false;
                GD.PushWarning($"[Tuning] 无法读取参数引用 {type.Name}.{method.Name}：{e.Message}");
            }
        }
        foreach (Type nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            complete &= Scan(nested, visit);
        return complete;
    }
}
