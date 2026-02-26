using UnityEngine;

public class LocalizedLabelAttribute : PropertyAttribute
{
    public readonly string Label;
    public readonly string Tooltip;

    public LocalizedLabelAttribute(string label)
    {
        Label = label;
        Tooltip = string.Empty;
    }

    public LocalizedLabelAttribute(string label, string tooltip)
    {
        Label = label;
        Tooltip = tooltip ?? string.Empty;
    }
}
