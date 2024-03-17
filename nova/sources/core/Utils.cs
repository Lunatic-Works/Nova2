using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Nova;

public static class Utils
{
    public const string ResourceRoot = "res://nova/resources/";

    public static FileAccess OpenFile(string path, FileAccess.ModeFlags mode)
    {
        var fs = FileAccess.Open(path, mode);
        if (fs == null)
        {
            var err = FileAccess.GetOpenError();
            var msg = $"Open {path} failed: {err}";
            throw new SystemException(msg);
        }
        return fs;
    }

    public static string GetFileAsText(string path)
    {
        using var fs = OpenFile(path, FileAccess.ModeFlags.Read);
        return fs.GetAsText();
    }

    public static void RuntimeAssert(bool cond, string msg)
    {
        if (!cond)
        {
            GD.PrintErr(msg);
            throw new ApplicationException($"Assert failed: {msg}");
        }
    }

    // Knuth's golden ratio multiplicative hashing
    public static ulong HashList(IEnumerable<ulong> hashes)
    {
        var r = 0UL;
        unchecked
        {
            foreach (var x in hashes)
            {
                r += x;
                r *= 11400714819323199563UL;
            }
        }

        return r;
    }

    public static ulong HashList<T>(IEnumerable<T> list) where T : class
    {
        return HashList(list.Select(x => (ulong)x.GetHashCode()));
    }
}
