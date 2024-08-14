using System.Collections.Generic;
using Godot;

namespace Nova;

public static class GDRuntime
{
    private const string RuntimeBlockScript = "res://nova/sources/gdscript/runtime_block.gd";
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

    public static RefCounted BaseRuntimeBlock => GetScript(RuntimeBlockScript);

    private static RefCounted Compile(string script)
    {
        var gdScript = new GDScript { SourceCode = script };
        gdScript.Reload();
        return gdScript.New().As<RefCounted>();
    }

    private static string WrapStatements(string baseClass, string script)
    {
        script = string.IsNullOrWhiteSpace(script) ? "" : script.Trim().Replace("\n", "\n    ");
        return $"extends {baseClass}\nfunc __eval():\n    pass\n    {script}\n";
    }

    private static string WrapExpression(string baseClass, string script)
    {
        return $"extends {baseClass}\nfunc __eval():\n    return {script.Trim()}\n";
    }

    public static RefCounted CompileBaseBlock(string script)
    {
        return Compile(WrapStatements("BaseBlock", script));
    }

    public static RefCounted CompileCondition(string expression)
    {
        return Compile(WrapExpression("ConditionBlock", expression));
    }

    public static bool InvokeCondition(RefCounted script)
    {
        return script?.Call("run").AsBool() ?? true;
    }
}
