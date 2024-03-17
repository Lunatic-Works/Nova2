using System.Collections.Generic;
using Godot;

namespace Nova;

public static class GDRuntime
{
    private const string LazyBlockBaseScript = "res://nova/sources/gdscript/lazy_block.gd";
    private const string EagerBlockBaseScript = "res://nova/sources/gdscript/eager_block.gd";
    private static readonly Dictionary<string, RefCounted> s_cachedScript = [];

    private static RefCounted GetScript(string path)
    {
        if (!s_cachedScript.TryGetValue(path, out var script))
        {
            script = ResourceLoader.Load<GDScript>(path).New().As<RefCounted>();
            s_cachedScript.Add(path, script);
        }
        return script;
    }

    public static RefCounted BaseLazyBlock => GetScript(LazyBlockBaseScript);
    public static RefCounted BaseEagerBlock => GetScript(EagerBlockBaseScript);

    private static RefCounted Compile(string script)
    {
        var gdScript = new GDScript { SourceCode = script };
        gdScript.Reload();
        return gdScript.New().As<RefCounted>();
    }

    private static string WrapStatements(string baseClass, string script)
    {
        script = string.IsNullOrWhiteSpace(script) ? "    pass" :
            script.Trim().Replace("\n", "\n    ");
        return $"extends {baseClass}\nfunc __eval():\n    {script}\n";
    }

    private static string WrapExpression(string baseClass, string script)
    {
        return $"extends {baseClass}\nfunc __eval():\n    return {script.Trim()}\n";
    }

    public static RefCounted CompileLazyBlock(string script)
    {
        return Compile(WrapStatements("LazyBlock", script));
    }

    public static RefCounted CompileEagerBlock(string script)
    {
        return Compile(WrapStatements("EagerBlock", script));
    }

    public static RefCounted CompileCondition(string expression)
    {
        return Compile(WrapExpression("ConditionBlock", expression));
    }
}
