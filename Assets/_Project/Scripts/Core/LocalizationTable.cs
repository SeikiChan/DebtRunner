using System.Collections.Generic;
using System;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Loads player-facing strings from Assets/Resources/Localization/Strings.csv.
/// Keys are stable identifiers; translators can edit locale columns without changing scenes.
/// </summary>
public static class LocalizationTable
{
    private const string ResourcePath = "Localization/Strings";
    private const string DefaultLocale = "zh-CN";
    private const string LocalePreferenceKey = "localization.locale";

    private static readonly Dictionary<string, Dictionary<string, string>> Entries =
        new Dictionary<string, Dictionary<string, string>>();
    private static readonly HashSet<string> MissingKeys = new HashSet<string>();

    private static bool loaded;
    private static string currentLocale = DefaultLocale;
    public static string CurrentLocale => currentLocale;
    public static event Action LocaleChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        Entries.Clear();
        MissingKeys.Clear();
        loaded = false;
        currentLocale = DefaultLocale;
        LocaleChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadSavedLocale()
    {
        string savedLocale = PlayerPrefs.GetString(LocalePreferenceKey, DefaultLocale);
        currentLocale = IsSupportedLocale(savedLocale) ? savedLocale : DefaultLocale;
    }

    public static void SetLocale(string locale)
    {
        string nextLocale = string.IsNullOrWhiteSpace(locale) ? DefaultLocale : locale.Trim();
        if (!IsSupportedLocale(nextLocale))
            nextLocale = DefaultLocale;

        if (currentLocale == nextLocale)
            return;

        currentLocale = nextLocale;
        PlayerPrefs.SetString(LocalePreferenceKey, currentLocale);
        PlayerPrefs.Save();
        LocaleChanged?.Invoke();
    }

    public static string GetLocaleDisplayName(string locale)
    {
        return Get(locale == "en" ? "settings.language.value.en" : "settings.language.value.zh-CN",
            locale == "en" ? "English" : "简体中文");
    }

    public static void SetText(TMP_Text text, string key, string fallback = null)
    {
        if (text == null)
            return;

        text.text = Get(key, fallback);
    }

    public static string Get(string key, string fallback = null)
    {
        EnsureLoaded();

        if (!string.IsNullOrWhiteSpace(key) && Entries.TryGetValue(key, out Dictionary<string, string> values))
        {
            if (values.TryGetValue(currentLocale, out string translated) && !string.IsNullOrWhiteSpace(translated))
                return translated;

            if (values.TryGetValue("en", out string english) && !string.IsNullOrWhiteSpace(english))
                return english;
        }

        if (!string.IsNullOrWhiteSpace(key) && MissingKeys.Add(key))
            Debug.LogWarning($"Localization key '{key}' is missing from {ResourcePath}.csv.");

        return fallback ?? key ?? string.Empty;
    }

    private static void EnsureLoaded()
    {
        if (loaded)
            return;

        loaded = true;
        TextAsset table = Resources.Load<TextAsset>(ResourcePath);
        if (table == null)
        {
            Debug.LogWarning($"Localization table not found at Resources/{ResourcePath}.csv.");
            return;
        }

        if (!TryParseCsv(table.text, out List<List<string>> rows))
        {
            Debug.LogWarning($"Localization table at Resources/{ResourcePath}.csv has an unterminated quoted field.");
            return;
        }

        if (rows.Count == 0)
            return;

        List<string> headers = rows[0];
        int keyColumn = headers.FindIndex(header => header.Trim() == "key");
        if (keyColumn < 0)
        {
            Debug.LogWarning($"Localization table at Resources/{ResourcePath}.csv has no 'key' column.");
            return;
        }

        Dictionary<string, int> localeColumns = new Dictionary<string, int>();
        for (int i = 0; i < headers.Count; i++)
        {
            string locale = headers[i].Trim();
            if (i != keyColumn && locale != "context" && locale.Length > 0 && !localeColumns.ContainsKey(locale))
                localeColumns.Add(locale, i);
        }

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> row = rows[rowIndex];
            if (row.Count <= keyColumn)
                continue;

            string key = row[keyColumn].Trim();
            if (key.Length == 0)
                continue;

            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (KeyValuePair<string, int> column in localeColumns)
            {
                if (column.Value < row.Count)
                    values[column.Key] = row[column.Value];
            }

            if (Entries.ContainsKey(key))
            {
                Debug.LogWarning($"Duplicate localization key '{key}' in {ResourcePath}.csv; keeping the first row.");
                continue;
            }

            Entries.Add(key, values);
        }
    }

    private static bool IsSupportedLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            return false;

        EnsureLoaded();
        if (Entries.Count == 0)
            return locale == "en" || locale == DefaultLocale;

        foreach (Dictionary<string, string> values in Entries.Values)
        {
            if (values.ContainsKey(locale))
                return true;
        }

        return false;
    }

    private static bool TryParseCsv(string text, out List<List<string>> rows)
    {
        rows = new List<List<string>>();
        if (string.IsNullOrEmpty(text))
            return true;

        if (text[0] == '\uFEFF')
            text = text.Substring(1);

        List<string> row = new List<string>();
        StringBuilder field = new StringBuilder();
        bool insideQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (insideQuotes)
            {
                if (character == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        insideQuotes = false;
                    }
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            if (character == '"' && field.Length == 0)
            {
                insideQuotes = true;
            }
            else if (character == ',')
            {
                row.Add(field.ToString());
                field.Length = 0;
            }
            else if (character == '\r' || character == '\n')
            {
                row.Add(field.ToString());
                field.Length = 0;
                AddNonEmptyRow(rows, row);
                row = new List<string>();

                if (character == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;
            }
            else
            {
                field.Append(character);
            }
        }

        if (insideQuotes)
            return false;

        row.Add(field.ToString());
        AddNonEmptyRow(rows, row);
        return true;
    }

    private static void AddNonEmptyRow(List<List<string>> rows, List<string> row)
    {
        for (int i = 0; i < row.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(row[i]))
            {
                rows.Add(row);
                return;
            }
        }
    }
}
