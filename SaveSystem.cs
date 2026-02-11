using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

public static class SaveSystem
{
    private const string SavePath = "user://autosave.cfg";

    // --- 自动保存 ---
    public static void SaveAll()
    {
        var config = new ConfigFile();
        // 扫描 GameInfo 中的静态字段
        FieldInfo[] fields = typeof(GameInfo).GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in fields)
        {
            object value = field.GetValue(null);
            if (value == null)
                continue;

            // 如果是数组或 List (例如 PlayerCharacters)
            if (field.FieldType.IsArray || (value is IList && field.FieldType.IsGenericType))
            {
                config.SetValue("Data", field.Name, SerializeCollection(value));
            }
            // 如果是 C# 字典 (例如 FirstLevelState)
            else if (value is IDictionary)
            {
                config.SetValue("Data", field.Name, SerializeDictionary(value));
            }
            else
            {
                // 基础类型 (int, string, Vector2I, 枚举等)
                config.SetValue("Data", field.Name, Variant.From(value));
            }
        }
        config.Save(SavePath);
        GD.Print("存档已自动保存。");
    }

    // --- 自动读取 ---
    public static void LoadAll()
    {
        var config = new ConfigFile();
        if (config.Load(SavePath) != Error.Ok)
            return;

        FieldInfo[] fields = typeof(GameInfo).GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in fields)
        {
            if (!config.HasSectionKey("Data", field.Name))
                continue;

            Variant savedVariant = config.GetValue("Data", field.Name);

            // 1. 处理数组或 List
            if (
                field.FieldType.IsArray
                || (
                    typeof(IList).IsAssignableFrom(field.FieldType) && field.FieldType.IsGenericType
                )
            )
            {
                field.SetValue(null, DeserializeCollection(savedVariant, field.FieldType));
            }
            // 2. 处理 C# 字典
            else if (typeof(IDictionary).IsAssignableFrom(field.FieldType))
            {
                field.SetValue(null, DeserializeDictionary(savedVariant, field.FieldType));
            }
            // 3. 处理基础类型和枚举
            else
            {
                field.SetValue(null, AssignVariant(savedVariant, field.FieldType));
            }
        }
        GD.Print("存档已自动加载。");
    }

    // --- 辅助方法：处理复杂结构转换为 Godot 类型 ---
    private static Godot.Collections.Array SerializeCollection(object collection)
    {
        var gArray = new Godot.Collections.Array();
        foreach (var item in (IEnumerable)collection)
        {
            gArray.Add(MapObjectToVariant(item));
        }
        return gArray;
    }

    private static Godot.Collections.Dictionary SerializeDictionary(object dictObj)
    {
        var gDict = new Godot.Collections.Dictionary();
        var iDict = (IDictionary)dictObj;
        foreach (DictionaryEntry entry in iDict)
        {
            gDict[Variant.From(entry.Key)] = MapObjectToVariant(entry.Value);
        }
        return gDict;
    }

    private static Variant MapObjectToVariant(object item)
    {
        if (item == null)
            return new Variant();
        Type t = item.GetType();
        // 如果是基础类型、枚举或 Godot 原生类型
        if (t.IsPrimitive || t == typeof(string) || t.IsEnum || t.Name.StartsWith("Vector2"))
            return Variant.From(item);

        // 如果是自定义 Struct (PlayerInfoStructure)
        var dict = new Godot.Collections.Dictionary();
        foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            dict[f.Name] = MapObjectToVariant(f.GetValue(item));
        }
        return dict;
    }

    // --- 辅助方法：从 Variant 还原为 C# 类型 ---
    private static object DeserializeCollection(Variant data, Type targetType)
    {
        var gArray = data.AsGodotArray();
        Type elementType = targetType.IsArray
            ? targetType.GetElementType()
            : targetType.GetGenericArguments()[0];
        IList resultList = (IList)
            Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

        foreach (var item in gArray)
        {
            resultList.Add(MapVariantToObject(item, elementType));
        }

        if (targetType.IsArray)
        {
            var array = Array.CreateInstance(elementType, resultList.Count);
            resultList.CopyTo(array, 0);
            return array;
        }
        return resultList;
    }

    private static object DeserializeDictionary(Variant data, Type targetType)
    {
        var gDict = data.AsGodotDictionary();
        var resultDict = (IDictionary)Activator.CreateInstance(targetType);
        Type[] args = targetType.GetGenericArguments();

        foreach (var key in gDict.Keys)
        {
            object cSharpKey = AssignVariant(key, args[0]);
            object cSharpVal = MapVariantToObject(gDict[key], args[1]);
            resultDict[cSharpKey] = cSharpVal;
        }
        return resultDict;
    }

    private static object MapVariantToObject(Variant v, Type targetType)
    {
        if (
            targetType.IsPrimitive
            || targetType == typeof(string)
            || targetType.IsEnum
            || targetType.Name.StartsWith("Vector2")
        )
            return AssignVariant(v, targetType);

        // 处理自定义 Struct
        object obj = Activator.CreateInstance(targetType);
        var gDict = v.AsGodotDictionary();
        foreach (var f in targetType.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (gDict.ContainsKey(f.Name))
                f.SetValue(obj, MapVariantToObject(gDict[f.Name], f.FieldType));
        }
        return obj;
    }

    private static object AssignVariant(Variant v, Type targetType)
    {
        // 1. 处理枚举：存档里存的是 long，需要转回具体的枚举类型
        if (targetType.IsEnum)
            return Enum.ToObject(targetType, v.AsInt64());

        // 2. 处理数值类型的不匹配问题
        // Godot 存档会将所有整数转为 long，所有浮点数转为 double
        // 但 C# 反射赋值要求类型必须严格精确（long 不能直接给 int 赋值）
        if (v.VariantType == Variant.Type.Int)
        {
            long val = v.AsInt64();
            if (targetType == typeof(int))
                return (int)val;
            if (targetType == typeof(short))
                return (short)val;
            if (targetType == typeof(byte))
                return (byte)val;
            return val;
        }

        if (v.VariantType == Variant.Type.Float)
        {
            double val = v.AsDouble();
            if (targetType == typeof(float))
                return (float)val;
            return val;
        }

        // 3. 处理 Godot 特有类型 (Vector2I, String, Rect2I 等)
        // 剩下的类型直接通过 v.Obj 提取对应的 C# 对象即可
        // 如果你的 Godot 版本里 v.Obj 报错，请使用 v.As<object>()
        try
        {
            return v.Obj;
        }
        catch
        {
            // 最后的兜底方案：如果上述都不行，尝试让 Godot 自己转
            return v.As<object>();
        }
    }
}
