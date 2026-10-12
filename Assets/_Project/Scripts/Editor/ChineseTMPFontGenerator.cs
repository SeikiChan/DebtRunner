using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Builds a static Simplified Chinese TMP fallback atlas from the localized project text.
/// Regenerate this asset when new Chinese characters are added; runtime text never modifies the atlas.
/// </summary>
public static class ChineseTMPFontGenerator
{
    private const string SourceFontPath = "Assets/_Project/Editor/LocalizationFontSources/ZixiaohunLiLiangCuHei.ttf";
    private const string OutputFontPath = "Assets/Resources/Localization/ZixiaohunLiLiangCuHei SDF.asset";
    private const string NotoFallbackPath = "Assets/Resources/Localization/NotoSansSC SDF.asset";
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    [MenuItem("Debt Runner/Localization/Generate Chinese TMP Font Fallback")]
    public static void GenerateFromMenu()
    {
        GenerateForBuild();
    }

    // Public so Unity can also call this with -executeMethod during asset setup.
    public static void GenerateForBuild()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
            throw new InvalidOperationException($"Could not load the Chinese source font at {SourceFontPath}.");

        TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null)
            throw new InvalidOperationException($"Could not load TextMesh Pro settings at {TmpSettingsPath}.");

        string characters = CollectLocalizedCharacters();
        if (characters.Length == 0)
            throw new InvalidOperationException("No Chinese characters were found in the localized project text.");

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            54,
            9,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true);

        if (fontAsset == null)
            throw new InvalidOperationException("TextMesh Pro could not create the Chinese font atlas.");

        fontAsset.name = "ZixiaohunLiLiangCuHei SDF";
        // The source face is already very heavy. TMP's synthetic bold dilation
        // closes its counters and makes adjacent CJK characters merge.
        fontAsset.normalSpacingOffset = 3f;
        fontAsset.boldStyle = 0f;
        fontAsset.TryAddCharacters(characters, out string missingCharacters);
        if (!string.IsNullOrEmpty(missingCharacters))
        {
            Debug.LogWarning($"The selected Chinese font does not contain {missingCharacters.Length} characters. The Noto Sans SC fallback will render them: {missingCharacters}");
        }

        // Freeze the populated atlas so the player does not need the full source TTF.
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        TMP_FontAsset notoFallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NotoFallbackPath);
        if (notoFallback != null)
            fontAsset.fallbackFontAssetTable.Add(notoFallback);
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputFontPath) != null)
            AssetDatabase.DeleteAsset(OutputFontPath);
        AssetDatabase.CreateAsset(fontAsset, OutputFontPath);

        Texture2D[] atlases = fontAsset.atlasTextures;
        for (int i = 0; i < atlases.Length; i++)
        {
            if (atlases[i] == null)
                continue;

            atlases[i].name = $"ZixiaohunLiLiangCuHei SDF Atlas {i + 1}";
            AssetDatabase.AddObjectToAsset(atlases[i], fontAsset);
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = "ZixiaohunLiLiangCuHei SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        SerializedObject settingsObject = new SerializedObject(settings);
        SerializedProperty fallbackAssets = settingsObject.FindProperty("m_fallbackFontAssets");
        if (fallbackAssets == null || !fallbackAssets.isArray)
            throw new InvalidOperationException("Could not find the TMP fallback font list in TMP Settings.");

        List<TMP_FontAsset> existingFallbacks = new List<TMP_FontAsset>();
        for (int i = 0; i < fallbackAssets.arraySize; i++)
        {
            TMP_FontAsset existing = fallbackAssets.GetArrayElementAtIndex(i).objectReferenceValue as TMP_FontAsset;
            if (existing != null && AssetDatabase.GetAssetPath(existing) != OutputFontPath)
                existingFallbacks.Add(existing);
        }

        fallbackAssets.arraySize = existingFallbacks.Count + 1;
        fallbackAssets.GetArrayElementAtIndex(0).objectReferenceValue = fontAsset;
        for (int i = 0; i < existingFallbacks.Count; i++)
            fallbackAssets.GetArrayElementAtIndex(i + 1).objectReferenceValue = existingFallbacks[i];

        settingsObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Generated static Simplified Chinese TMP fallback with {characters.Length} characters at {OutputFontPath}.");
    }

    private static string CollectLocalizedCharacters()
    {
        HashSet<char> characters = new HashSet<char>(
            "，。！？；：、（）【】《》〈〉「」『』…—·￥“”‘’～　");

        // These labels also contain numbers, prices and Latin abbreviations.
        for (char character = ' '; character <= '~'; character++)
            characters.Add(character);

        AddCharactersFromFile("Assets/DebtRunner.unity", characters);
        AddCharactersFromFile("Assets/Resources/Localization/Strings.csv", characters);
        AddCharactersFromDirectory("Assets/_Project/Data", characters, "*.asset");
        AddStringLiteralCharactersFromDirectory("Assets/_Project/Scripts/Gameplay", characters, "*.cs");
        AddStringLiteralCharactersFromDirectory("Assets/_Project/Scripts/Editor", characters, "*.cs");

        List<char> sortedCharacters = new List<char>(characters);
        sortedCharacters.Sort();
        StringBuilder result = new StringBuilder(sortedCharacters.Count);
        foreach (char character in sortedCharacters)
            result.Append(character);
        return result.ToString();
    }

    private static void AddCharactersFromDirectory(string directory, HashSet<char> characters, string pattern)
    {
        if (!Directory.Exists(directory))
            return;

        foreach (string path in Directory.GetFiles(directory, pattern, SearchOption.AllDirectories))
            AddCharactersFromFile(path, characters);
    }

    private static void AddCharactersFromFile(string path, HashSet<char> characters)
    {
        if (!File.Exists(path))
            return;

        string contents = File.ReadAllText(path, Encoding.UTF8);
        AddCjkCharacters(contents, characters);
    }

    private static void AddStringLiteralCharactersFromDirectory(string directory, HashSet<char> characters, string pattern)
    {
        if (!Directory.Exists(directory))
            return;

        foreach (string path in Directory.GetFiles(directory, pattern, SearchOption.AllDirectories))
        {
            string contents = File.ReadAllText(path, Encoding.UTF8);
            contents = Regex.Replace(contents, @"(?s)/\*.*?\*/", string.Empty);
            contents = Regex.Replace(contents, @"//[^\r\n]*", string.Empty);

            foreach (Match match in Regex.Matches(contents, "\"(?:\\\\.|[^\"\\\\])*\""))
                AddCjkCharacters(match.Value, characters);
        }
    }

    private static void AddCjkCharacters(string contents, HashSet<char> characters)
    {
        foreach (char character in contents)
        {
            if ((character >= '\u3400' && character <= '\u4DBF') ||
                (character >= '\u4E00' && character <= '\u9FFF') ||
                (character >= '\uF900' && character <= '\uFAFF'))
            {
                characters.Add(character);
            }
        }
    }
}
