using System;
using System.Collections.Generic;

public static class ResourceKeys
{
    private static readonly List<Func<string, string>> Resolvers = new()
    {
        DataKeys.GetDataPath,
        PrefabKeys.GetPrefabPath,
    };

    public static string GetPath(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        foreach (var resolver in Resolvers)
        {
            var path = resolver(key);
            if (!string.IsNullOrEmpty(path))
                return path;
        }

        return string.Empty;
    }
}
