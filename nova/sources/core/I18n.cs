using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Godot;

namespace Nova;
using LocalizedStrings = IReadOnlyDictionary<string, string>;
using TranslationBundle = Dictionary<string, object>;

public partial class I18n : ISingleton
{
    // https://docs.godotengine.org/en/stable/tutorials/i18n/locales.html

    public const string LocalizedResourcesPath = Assets.ResourceRoot + "localized_resources/";
    public const string LocalizedStringsPath = LocalizedResourcesPath + "localized_strings/";

    public static readonly string[] SupportedLocales = ["zh", "en"];
    public static string DefaultLocale => SupportedLocales[0];

    private readonly Dictionary<string, TranslationBundle> _translationBundles = [];

    private string _currentLocale;
    public Event LocaleChanged = new();
    public string CurrentLocale
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

    public void OnEnter()
    {
        _currentLocale = OS.GetLocaleLanguage();
        foreach (var locale in SupportedLocales)
        {
            var filePath = LocalizedStringsPath + locale + ".json";
            var translation = Utils.GetFileAsText(filePath);
            _translationBundles[locale] = JsonConvert.DeserializeObject<TranslationBundle>(translation);
        }
    }

    public void OnReady() { }

    public void OnExit()
    {
        _translationBundles.Clear();
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
    private string Tranlate(string locale, string key, params object[] args)
    {
        var translation = key;

        if (_translationBundles[locale].TryGetValue(key, out var raw))
        {
            if (raw is string value)
            {
                translation = value;
            }
            else if (raw is string[] formats)
            {
                if (formats.Length == 0)
                {
                    Utils.Warn($"Empty translation string list for: {key}");
                }
                else if (args.Length == 0)
                {
                    translation = formats[0];
                }
                else
                {
                    // The first argument will determine the quantity
                    var arg1 = args[0];
                    if (arg1 is int i)
                    {
                        translation = formats[Math.Min(i, formats.Length - 1)];
                    }
                }
            }
            else
            {
                Utils.Warn($"Invalid translation format for: {key}");
            }

            if (args.Length > 0)
            {
                translation = string.Format(translation, args);
            }
        }
        else
        {
            Utils.Warn($"Missing translation for: {key}");
        }

        return translation;
    }

    public string Translate(string key, params object[] args)
    {
        return Tranlate(CurrentLocale, key, args);
    }

    // Get localized string with fallback to DefaultLocale
    public string Translate(LocalizedStrings dict)
    {
        if (dict == null)
        {
            return null;
        }

        if (dict.TryGetValue(CurrentLocale, out var value))
        {
            return value;
        }
        else
        {
            return dict[DefaultLocale];
        }
    }

    public LocalizedStrings GetLocalizedStrings(string key, params object[] args)
    {
        var dict = SupportedLocales.ToDictionary(locale => locale, locale => Translate(locale, key, args));
        return dict;
    }

    public static I18n Instance => NovaController.Instance.GetObj<I18n>();

    public static string __(string key, params object[] args)
    {
        return Instance.Translate(key, args);
    }

    public static string __(LocalizedStrings dict)
    {
        return Instance.Translate(dict);
    }
}
