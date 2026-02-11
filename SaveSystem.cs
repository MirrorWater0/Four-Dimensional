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
        FieldInfo[] fields = typeof(GameInfo).GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in fields)
        {
            object value = field.GetValue(null);
            if (value == null)
                continue;

            // 统一使用 MapObjectToVariant 转换所有类型的静态变量
            config.SetValue("Data", field.Name, MapObjectToVariant(value));
        }
        config.Save(SavePath);
        GD.Print("存档成功。");
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

        // 1. 处理基础类型（这些类型直接转，不经过 object 泛型）
        if (item is string s)
            return s;
        if (item is bool b)
            return b;
        if (item is int i)
            return i;
        if (item is long l)
            return l;
        if (item is float f)
            return f;
        if (item is double d)
            return d;
        if (item is Vector2 v2)
            return v2;
        if (item is Vector2I v2i)
            return v2i;
        if (item is Rect2 r2)
            return r2;
        if (item is Rect2I r2i)
            return r2i;
        if (item is Color col)
            return col;

        // 2. 处理枚举 (枚举在 Godot 中存为 long)
        if (t.IsEnum)
            return Convert.ToInt64(item);

        // 3. 处理字典 (Dictionary)
        if (item is IDictionary iDict)
        {
            var gDict = new Godot.Collections.Dictionary();
            foreach (DictionaryEntry entry in iDict)
            {
                gDict[MapObjectToVariant(entry.Key)] = MapObjectToVariant(entry.Value);
            }
            return gDict;
        }

        // 4. 处理集合 (数组或 List)
        if (item is IList iList)
        {
            var gArray = new Godot.Collections.Array();
            foreach (var element in iList)
            {
                gArray.Add(MapObjectToVariant(element));
            }
            return gArray;
        }

        // 5. 处理自定义结构体 (如 PlayerInfoStructure)
        // 如果不是基础类型，我们将其视为对象，通过反射拆解字段
        if (!t.IsPrimitive && t != typeof(string))
        {
            var dict = new Godot.Collections.Dictionary();
            // 获取所有公开字段
            FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var k in fields)
            {
                dict[k.Name] = MapObjectToVariant(k.GetValue(item));
            }
            return dict;
        }

        // 最后的兜底方案
        return Variant.From((dynamic)item);
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
        // 1. 处理基础类型、枚举、Godot类型
        if (
            targetType.IsPrimitive
            || targetType == typeof(string)
            || targetType.IsEnum
            || targetType.Name.StartsWith("Vector2")
        )
            return AssignVariant(v, targetType);

        // 2. 处理数组和集合（必须在自定义结构体之前检查）
        if (targetType.IsArray || typeof(IList).IsAssignableFrom(targetType))
        {
            return DeserializeCollection(v, targetType);
        }

        // 3. 处理字典
        if (typeof(IDictionary).IsAssignableFrom(targetType))
        {
            return DeserializeDictionary(v, targetType);
        }

        // 4. 处理自定义 Struct
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
        if (targetType == typeof(Vector2I))
            return v.AsVector2I();
        if (targetType == typeof(Vector2))
            return v.AsVector2();
        if (targetType == typeof(string))
            return v.AsString();

        return v.Obj;
    }
}
