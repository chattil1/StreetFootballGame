using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public static class ValidationUtility
{
    public static bool IsFullyInitialized(object obj, int maxDepth = 5)
    {
        // 1. Basic Null Check (and Unity "Fake" Null check)
        if (obj == null || (obj is UnityEngine.Object unityObj && unityObj == null))
            return false;

        // 2. Stop recursion at primitive types or max depth
        Type type = obj.GetType();
        if (type.IsValueType || type == typeof(string) || maxDepth <= 0)
            return true;

        // 3. Handle Collections (Lists, Arrays, etc.)
        if (obj is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (!IsFullyInitialized(item, maxDepth - 1)) return false;
            }
            return true;
        }

        // 4. Reflect over all instance fields (including private)
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(obj);
            if (!IsFullyInitialized(value, maxDepth - 1))
            {
                Debug.LogWarning($"Null found in field: {field.Name} on {type.Name}");
                return false;
            }
        }

        return true;
    }
}
