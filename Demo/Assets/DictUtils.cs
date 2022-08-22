using System;
using System.Collections.Generic;
using System.Text;

public static class DictUtils
{
    public static Dictionary<Key, Value> MergeInPlace<Key, Value>(this Dictionary<Key, Value> left,
        Dictionary<Key, Value> right)
    {
        if (left == null)
        {
            throw new ArgumentNullException("Can't merge into a null dictionary");
        }
        else if (right == null)
        {
            return left;
        }

        foreach (var kvp in right)
        {
            if (!left.ContainsKey(kvp.Key))
            {
                left.Add(kvp.Key, kvp.Value);
            }
        }

        return left;
    }

    public static string PrettyPrint<TKey, TValue>(this Dictionary<TKey, TValue> left)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("{");
        foreach (var kvp in left)
        {
            builder.AppendLine("  {'" + kvp.Key + "','" + kvp.Value + "'},");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }
}