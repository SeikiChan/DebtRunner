using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 胜利结算面板模板生成工具
/// 在编辑器中快速生成完整的胜利结算 UI 面板
/// 使用方式：GameObject -> DebtRunner -> Create Victory Panel Template
/// </summary>
public class VictoryPanelTemplate
{
    [MenuItem("GameObject/DebtRunner/Create Victory Panel Template")]
    public static void CreateVictoryPanelTemplate()
    {
        // 检查是否存在 Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("错误", "场景中需要存在 Canvas。请先创建 Canvas！", "确定");
            return;
        }

        // 检查是否已存在Victory Panel
        Transform existingVictoryPanel = canvas.transform.Find("Panel_Victory");
        if (existingVictoryPanel != null)
        {
            EditorUtility.DisplayDialog("提示", "Panel_Victory 已存在！", "确定");
            return;
        }

        // 创建根 Panel
        GameObject panelRoot = new GameObject("Panel_Victory");
        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRoot.AddComponent<CanvasGroup>();
        Image panelImage = panelRoot.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0);
        
        panelRect.SetParent(canvas.transform, false);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // 创建背景图像 (深色覆盖)
        GameObject bgOverlay = new GameObject("Background");
        RectTransform bgRect = bgOverlay.AddComponent<RectTransform>();
        Image bgImage = bgOverlay.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        bgImage.raycastTarget = true;

        bgRect.SetParent(panelRect, false);
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 创建背景精灵占位符 (用于替换不同的背景素材)
        GameObject bgImageObj = new GameObject("BackgroundImage");
        RectTransform bgImgRect = bgImageObj.AddComponent<RectTransform>();
        Image bgImg = bgImageObj.AddComponent<Image>();
        bgImg.color = new Color(0.8f, 0.7f, 0.3f, 0.6f); // 金色/橙色
        bgImg.raycastTarget = false;

        bgImgRect.SetParent(panelRect, false);
        bgImgRect.anchorMin = Vector2.zero;
        bgImgRect.anchorMax = Vector2.one;
        bgImgRect.offsetMin = Vector2.zero;
        bgImgRect.offsetMax = Vector2.zero;

        // 创建内容容器（用于布局）
        GameObject contentContainer = new GameObject("Content");
        RectTransform contentRect = contentContainer.AddComponent<RectTransform>();
        contentContainer.AddComponent<LayoutElement>();

        contentRect.SetParent(panelRect, false);
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(1000, 700);
        contentRect.anchoredPosition = Vector2.zero;

        // 创建标题文本 (Victory Title)
        GameObject titleObj = new GameObject("VictoryTitle");
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();

        titleRect.SetParent(contentRect, false);
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(900, 150);
        titleRect.anchoredPosition = new Vector2(0, 200);

        titleText.text = "DEBT CLEAR";
        titleText.fontSize = 100;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1, 0.8f, 0.2f, 1); // 金色
        titleText.outlineWidth = 0.3f;
        titleText.outlineColor = Color.black;
        titleText.raycastTarget = false;

        // 创建副标题文本 (Victory Subtitle)
        GameObject subtitleObj = new GameObject("VictorySubtitle");
        RectTransform subtitleRect = subtitleObj.AddComponent<RectTransform>();
        TextMeshProUGUI subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();

        subtitleRect.SetParent(contentRect, false);
        subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
        subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
        subtitleRect.sizeDelta = new Vector2(800, 100);
        subtitleRect.anchoredPosition = new Vector2(0, 100);

        subtitleText.text = "YOU'RE FREE";
        subtitleText.fontSize = 60;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.fontStyle = FontStyles.Bold;
        subtitleText.color = Color.white;
        subtitleText.outlineWidth = 0.2f;
        subtitleText.outlineColor = Color.black;
        subtitleText.raycastTarget = false;

        // 创建统计信息文本 (Victory Stats)
        GameObject statsObj = new GameObject("VictoryStats");
        RectTransform statsRect = statsObj.AddComponent<RectTransform>();
        TextMeshProUGUI statsText = statsObj.AddComponent<TextMeshProUGUI>();

        statsRect.SetParent(contentRect, false);
        statsRect.anchorMin = new Vector2(0.5f, 0.5f);
        statsRect.anchorMax = new Vector2(0.5f, 0.5f);
        statsRect.sizeDelta = new Vector2(800, 120);
        statsRect.anchoredPosition = new Vector2(0, -20);

        statsText.text = "Total Rounds Survived: 11\nTotal Cash Earned: 5000\nFinal Level: 25";
        statsText.fontSize = 24;
        statsText.alignment = TextAlignmentOptions.Center;
        statsText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        statsText.outlineWidth = 0.15f;
        statsText.outlineColor = Color.black;
        statsText.raycastTarget = false;

        // 创建按钮容器
        GameObject buttonsContainer = new GameObject("ButtonsContainer");
        RectTransform buttonsRect = buttonsContainer.AddComponent<RectTransform>();
        buttonsRect.SetParent(contentRect, false);
        buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsRect.sizeDelta = new Vector2(600, 80);
        buttonsRect.anchoredPosition = new Vector2(0, -180);

        // 创建 End Game 按钮
        GameObject endGameBtnObj = new GameObject("Btn_EndGame", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform endGameRect = endGameBtnObj.GetComponent<RectTransform>();
        endGameRect.SetParent(buttonsRect, false);
        endGameRect.anchorMin = new Vector2(0.5f, 0.5f);
        endGameRect.anchorMax = new Vector2(0.5f, 0.5f);
        endGameRect.sizeDelta = new Vector2(220, 60);
        endGameRect.anchoredPosition = new Vector2(-120, 0);

        Image endGameImg = endGameBtnObj.GetComponent<Image>();
        endGameImg.color = new Color(1, 0.7f, 0.2f, 1); // 橙色

        Button endGameBtn = endGameBtnObj.GetComponent<Button>();

        GameObject endGameLabel = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform endGameLabelRect = endGameLabel.GetComponent<RectTransform>();
        endGameLabelRect.SetParent(endGameRect, false);
        endGameLabelRect.anchorMin = Vector2.zero;
        endGameLabelRect.anchorMax = Vector2.one;
        endGameLabelRect.offsetMin = Vector2.zero;
        endGameLabelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI endGameTMP = endGameLabel.GetComponent<TextMeshProUGUI>();
        endGameTMP.text = "End Game";
        endGameTMP.alignment = TextAlignmentOptions.Center;
        endGameTMP.fontSize = 28;
        endGameTMP.fontStyle = FontStyles.Bold;
        endGameTMP.color = Color.white;

        // 创建 Main Menu 按钮
        GameObject menuBtnObj = new GameObject("Btn_MainMenu", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform menuRect = menuBtnObj.GetComponent<RectTransform>();
        menuRect.SetParent(buttonsRect, false);
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.sizeDelta = new Vector2(220, 60);
        menuRect.anchoredPosition = new Vector2(120, 0);

        Image menuImg = menuBtnObj.GetComponent<Image>();
        menuImg.color = new Color(1, 0.7f, 0.2f, 1); // 橙色

        Button menuBtn = menuBtnObj.GetComponent<Button>();

        GameObject menuLabel = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform menuLabelRect = menuLabel.GetComponent<RectTransform>();
        menuLabelRect.SetParent(menuRect, false);
        menuLabelRect.anchorMin = Vector2.zero;
        menuLabelRect.anchorMax = Vector2.one;
        menuLabelRect.offsetMin = Vector2.zero;
        menuLabelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI menuTMP = menuLabel.GetComponent<TextMeshProUGUI>();
        menuTMP.text = "Main Menu";
        menuTMP.alignment = TextAlignmentOptions.Center;
        menuTMP.fontSize = 28;
        menuTMP.fontStyle = FontStyles.Bold;
        menuTMP.color = Color.white;

        // 给根Panel添加VictoryPanel组件
        VictoryPanel victoryPanelScript = panelRoot.AddComponent<VictoryPanel>();

        // 将生成的按钮引用赋值到 VictoryPanel
        if (victoryPanelScript != null)
        {
            victoryPanelScript.endGameButton = endGameBtn;
            victoryPanelScript.mainMenuButton = menuBtn;
        }

        // 设置统计文本引用
        if (victoryPanelScript != null)
        {
            // 通过反射或直接赋值（如果VictoryPanel中有对应字段）
            // 这里假设VictoryPanel有victoryStatsText字段，通过序列化赋值
        }

        // 默认隐藏，只在胜利时显示
        panelRoot.SetActive(false);

        // 在Inspector中设置引用
        EditorGUIUtility.PingObject(panelRoot);
        Selection.activeGameObject = panelRoot;

        EditorUtility.DisplayDialog("成功", 
            "Victory Panel Template 已创建！\n\n" +
            "下一步：\n" +
            "1. 将此 Panel_Victory 拖拽到 GameFlowController 的 \"Panel Victory\" 字段\n" +
            "2. 在 Panel_Victory 的 Inspector 中配置各项参数\n" +
            "3. 为胜利界面设置对应的背景素材", 
            "确定");

        Debug.Log("Victory Panel Template created successfully!");
    }
}

#endif
