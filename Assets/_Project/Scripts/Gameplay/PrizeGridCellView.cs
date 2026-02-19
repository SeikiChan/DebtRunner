using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PrizeGridCellView
{
    public RectTransform root;
    public Image icon;
    public TMP_Text label;
    public Image highlightOverlay;

    [NonSerialized] public Vector3 baseScale;
}
