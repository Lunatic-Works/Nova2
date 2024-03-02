using System.Collections.Generic;
using Newtonsoft.Json;
using Godot;
using System;
using System.Linq;

namespace Nova;

using LocalizedStrings = Dictionary<string, string>;

using TranslationBundle = Dictionary<string, object>;

public class I18n
{
    /* https://docs.godotengine.org/en/stable/tutorials/i18n/locales.html */
    public const string LocalizedResourcesPath = "res://nova/resources/localized_resources/";
    public const string LocalizedStringsPath = "res://nova/resources/localized_strings/";

    public static readonly string[] SupportedLocales = ["zh", "en"];

    public static string DefaultLocale => SupportedLocales[0];

    private static string _currentLocale = OS.GetLocaleLanguage();

    public static event Action LocaleChanged;

    public static string CurrentLocale
    {
        get => _currentLocale;
        set
        {
            if (_currentLocale == value)
            {
                return;
            }

            _currentLocale = value;
            LocaleChanged.Invoke();
        }
    }

    private static bool Inited;

    private static void Init()
    {
        if (Inited) return;
        LoadTranslationBundles();
        Inited = true;
    }

    private static Dictionary<string, TranslationBundle> TranslationBundles = [];

    private static void LoadTranslationBundles()
    {
        foreach (var locale in SupportedLocales)
        {
            var filePath = LocalizedStringsPath + locale + ".json";
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                var err = FileAccess.GetOpenError();
                GD.PrintErr(err);
            }
            TranslationBundles[locale] = JsonConvert.DeserializeObject<TranslationBundle>(file.GetAsText());
        }
    }

    /// <summary>
    /// Get the translation specified by key and optionally deal with the plurals and format arguments. (Shorthand)<para />
    /// Translation will be automatically reloaded if the JSON file is changed.
    /// </summary>
    /// <param name="locale"></param>
    /// <param name="key">Key to specify the translation</param>
    /// <param name="args">Arguments to provide to the translation as a format string.<para />
    /// The first argument will be used to determine the quantity if needed.</param>
    /// <returns>The translated string.</returns>
    private static string __(string locale, string key, params object[] args)
    {
        Init();

        string translation = key;

        if (TranslationBundles[locale].TryGetValue(key, out var raw))
        {
            if (raw is string value)
            {
                translation = value;
            }
            else if (raw is string[] formats)
            {
                if (formats.Length == 0)
                {
                    GD.PrintErr($"Nova: Empty translation string list for: {key}");
                }
                else if (args.Length == 0)
                {
                    translation = formats[0];
                }
                else
                {
                    // The first argument will determine the quantity
                    object arg1 = args[0];
                    if (arg1 is int i)
                    {
                        translation = formats[Math.Min(i, formats.Length - 1)];
                    }
                }
            }
            else
            {
                GD.PrintErr($"Nova: Invalid translation format for: {key}");
            }

            if (args.Length > 0)
            {
                translation = string.Format(translation, args);
            }
        }
        else
        {
            GD.PrintErr($"Nova: Missing translation for: {key}");
        }

        return translation;
    }

    public static string __(string key, params object[] args)
    {
        return __(CurrentLocale, key, args);
    }

    // Get localized string with fallback to DefaultLocale
    public static string __(LocalizedStrings dict)
    {
        if (dict == null)
        {
            return null;
        }

        if (dict.ContainsKey(CurrentLocale))
        {
            return dict[CurrentLocale];
        }
        else
        {
            return dict[DefaultLocale];
        }
    }

    public static LocalizedStrings GetLocalizedStrings(string key, params object[] args)
    {
        var dict = SupportedLocales.ToDictionary(locale => locale, locale => __(locale, key, args));
        return dict;
    }
}
