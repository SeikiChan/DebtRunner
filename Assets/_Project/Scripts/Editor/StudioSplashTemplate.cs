using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Creates a studio splash screen panel: white background + centered logo.
/// Menu: GameObject > DebtRunner > Create Studio Splash Screen
/// </summary>
public static class StudioSplashTemplate
{
    [MenuItem("GameObject/DebtRunner/Create Studio Splash Screen")]
    public static void CreateStudioSplashScreen()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found. Create a Canvas first.", "OK");
            return;
        }

        const string panelName = "Panel_StudioSplash";
        Transform existing = canvas.transform.Find(panelName);
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Hint", $"{panelName} already exists.", "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Root panel — fullscreen white
        GameObject panelRoot = new GameObject(panelName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.SetParent(canvas.transform, false);
        Stretch(panelRect);

        Image rootBg = panelRoot.GetComponent<Image>();
        rootBg.color = Color.white;
        rootBg.raycastTarget = true;

        CanvasGroup cg = panelRoot.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        StudioSplashScreen splashComp = panelRoot.AddComponent<StudioSplashScreen>();
        panelRoot.SetActive(false);

        // Studio logo — centered
        GameObject logoGo = new GameObject("StudioLogo", typeof(RectTransform), typeof(Image));
        RectTransform logoRect = logoGo.GetComponent<RectTransform>();
        logoRect.SetParent(panelRect, false);
        logoRect.anchorMin = new Vector2(0.5f, 0.5f);
        logoRect.anchorMax = new Vector2(0.5f, 0.5f);
        logoRect.pivot = new Vector2(0.5f, 0.5f);
        logoRect.sizeDelta = new Vector2(512f, 300f);
        logoRect.anchoredPosition = Vector2.zero;

        Image logoImg = logoGo.GetComponent<Image>();
        logoImg.color = Color.white;
        logoImg.preserveAspect = true;
        logoImg.raycastTarget = false;

        // Auto-bind references
        SerializedObject so = new SerializedObject(splashComp);
        so.FindProperty("canvasGroup").objectReferenceValue = cg;
        so.FindProperty("studioLogoImage").objectReferenceValue = logoImg;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Auto-bind to GameFlowController
        GameFlowController gfc = Object.FindObjectOfType<GameFlowController>();
        if (gfc != null)
        {
            SerializedObject gfcSo = new SerializedObject(gfc);
            SerializedProperty prop = gfcSo.FindProperty("studioSplash");
            if (prop != null)
            {
                prop.objectReferenceValue = splashComp;
                gfcSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        Undo.RegisterCreatedObjectUndo(panelRoot, "Create Studio Splash Screen");
        Selection.activeGameObject = panelRoot;
        EditorUtility.DisplayDialog("Done",
            $"{panelName} created.\n\nAssign your studio logo PNG to StudioLogo > Image > Source Image.", "OK");
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
