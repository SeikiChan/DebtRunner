using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

[CustomPropertyDrawer(typeof(LocalizedLabelAttribute))]
public class LocalizedLabelDrawer : PropertyDrawer
{
    private static readonly Regex LatinRegex = new Regex("[A-Za-z]", RegexOptions.Compiled);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        LocalizedLabelAttribute localized = (LocalizedLabelAttribute)attribute;
        string text = BuildLabelText(localized.Label, label.text);
        string tooltip = string.IsNullOrWhiteSpace(localized.Tooltip) ? label.tooltip : localized.Tooltip;
        GUIContent content = new GUIContent(text, tooltip);
        EditorGUI.PropertyField(position, property, content, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    private static string BuildLabelText(string localizedLabel, string fallbackEnglishLabel)
    {
        if (string.IsNullOrWhiteSpace(localizedLabel))
            return fallbackEnglishLabel;

        // If caller already gave bilingual/English text, keep it as-is.
        if (LatinRegex.IsMatch(localizedLabel))
            return localizedLabel;

        if (string.IsNullOrWhiteSpace(fallbackEnglishLabel))
            return localizedLabel;

        return $"{fallbackEnglishLabel} / {localizedLabel}";
    }
}
