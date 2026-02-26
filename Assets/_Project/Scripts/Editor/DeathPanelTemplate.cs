using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 死亡结算面板模板生成工具
/// 在编辑器中快速生成完整的死亡结算 UI 面板
/// 使用方式：GameObject -> DebtRunner -> Create Death Panel Template
/// </summary>
public class DeathPanelTemplate
{
    [MenuItem("GameObject/DebtRunner/Create Death Panel Template")]
    public static void CreateDeathPanelTemplate()
    {
        // 检查是否存在 Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("错误", "场景中需要存在 Canvas。请先创建 Canvas！", "确定");
            return;
        }

        // 检查是否已存在Death Panel
        Transform existingDeathPanel = canvas.transform.Find("Panel_Death");
        if (existingDeathPanel != null)
        {
            EditorUtility.DisplayDialog("提示", "Panel_Death 已存在！", "确定");
            return;
        }

        // 创建根 Panel
        GameObject panelRoot = new GameObject("Panel_Death");
        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRoot.AddComponent<CanvasGroup>();
        Image panelImage = panelRoot.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0);
        
        panelRect.SetParent(canvas.transform, false);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // 创建背景图像 (可选的深色覆盖)
        GameObject bgOverlay = new GameObject("Background");
        RectTransform bgRect = bgOverlay.AddComponent<RectTransform>();
        Image bgImage = bgOverlay.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.6f);
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
        bgImg.color = Color.gray; // 默认灰色，便于识别
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
        contentRect.sizeDelta = new Vector2(1000, 600);
        contentRect.anchoredPosition = Vector2.zero;

        // 创建标题文本 (Death Title)
        GameObject titleObj = new GameObject("DeathTitle");
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();

        titleRect.SetParent(contentRect, false);
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(900, 150);
        titleRect.anchoredPosition = new Vector2(0, 150);

        titleText.text = "YOU DIED!";
        titleText.fontSize = 100;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1, 0.2f, 0.2f, 1); // 红色
        titleText.outlineWidth = 0.3f;
        titleText.outlineColor = Color.black;
        titleText.raycastTarget = false;

        // 创建死因类型文本 (Death Reason)
        GameObject reasonObj = new GameObject("DeathReason");
        RectTransform reasonRect = reasonObj.AddComponent<RectTransform>();
        TextMeshProUGUI reasonText = reasonObj.AddComponent<TextMeshProUGUI>();

        reasonRect.SetParent(contentRect, false);
        reasonRect.anchorMin = new Vector2(0.5f, 0.5f);
        reasonRect.anchorMax = new Vector2(0.5f, 0.5f);
        reasonRect.sizeDelta = new Vector2(800, 60);
        reasonRect.anchoredPosition = new Vector2(0, 60);

        reasonText.text = "KILLED BY MONSTER";
        reasonText.fontSize = 40;
        reasonText.alignment = TextAlignmentOptions.Center;
        reasonText.fontStyle = FontStyles.Bold;
        reasonText.color = new Color(1, 0.8f, 0.2f, 1); // 金色
        reasonText.outlineWidth = 0.2f;
        reasonText.outlineColor = Color.black;
        reasonText.raycastTarget = false;

        // 创建描述文本 (Death Description)
        GameObject descObj = new GameObject("DeathDescription");
        RectTransform descRect = descObj.AddComponent<RectTransform>();
        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();

        descRect.SetParent(contentRect, false);
        descRect.anchorMin = new Vector2(0.5f, 0.5f);
        descRect.anchorMax = new Vector2(0.5f, 0.5f);
        descRect.sizeDelta = new Vector2(800, 100);
        descRect.anchoredPosition = new Vector2(0, -30);

        descText.text = "You were defeated by enemies in battle.";
        descText.fontSize = 28;
        descText.alignment = TextAlignmentOptions.Center;
        descText.color = new Color(1, 1, 1, 0.8f);
        descText.outlineWidth = 0.2f;
        descText.outlineColor = Color.black;
        descText.raycastTarget = false;

        // 创建提示文本 (Tip)
        GameObject tipObj = new GameObject("TipText");
        RectTransform tipRect = tipObj.AddComponent<RectTransform>();
        TextMeshProUGUI tipText = tipObj.AddComponent<TextMeshProUGUI>();

        tipRect.SetParent(contentRect, false);
        tipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tipRect.sizeDelta = new Vector2(800, 60);
        tipRect.anchoredPosition = new Vector2(0, -140);

        tipText.text = "Press Any Key to Continue...";
        tipText.fontSize = 24;
        tipText.alignment = TextAlignmentOptions.Center;
        tipText.fontStyle = FontStyles.Italic;
        tipText.color = new Color(0.8f, 0.8f, 0.8f, 0.6f);
        tipText.raycastTarget = false;

        // 创建按钮容器
        GameObject buttonsContainer = new GameObject("ButtonsContainer");
        RectTransform buttonsRect = buttonsContainer.AddComponent<RectTransform>();
        buttonsRect.SetParent(contentRect, false);
        buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsRect.sizeDelta = new Vector2(600, 80);
        buttonsRect.anchoredPosition = new Vector2(0, -240);

        // 创建 Restart 按钮
        GameObject restartBtnObj = new GameObject("Btn_Restart", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform restartRect = restartBtnObj.GetComponent<RectTransform>();
        restartRect.SetParent(buttonsRect, false);
        restartRect.anchorMin = new Vector2(0.5f, 0.5f);
        restartRect.anchorMax = new Vector2(0.5f, 0.5f);
        restartRect.sizeDelta = new Vector2(220, 60);
        restartRect.anchoredPosition = new Vector2(-120, 0);

        Image restartImg = restartBtnObj.GetComponent<Image>();
        restartImg.color = new Color(0.2f, 0.6f, 1f, 1f);

        Button restartBtn = restartBtnObj.GetComponent<Button>();

        GameObject restartLabel = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform restartLabelRect = restartLabel.GetComponent<RectTransform>();
        restartLabelRect.SetParent(restartRect, false);
        restartLabelRect.anchorMin = Vector2.zero;
        restartLabelRect.anchorMax = Vector2.one;
        restartLabelRect.offsetMin = Vector2.zero;
        restartLabelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI restartTMP = restartLabel.GetComponent<TextMeshProUGUI>();
        restartTMP.text = "Restart";
        restartTMP.alignment = TextAlignmentOptions.Center;
        restartTMP.fontSize = 28;
        restartTMP.color = Color.white;

        // 创建 Main Menu 按钮
        GameObject menuBtnObj = new GameObject("Btn_MainMenu", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform menuRect = menuBtnObj.GetComponent<RectTransform>();
        menuRect.SetParent(buttonsRect, false);
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.sizeDelta = new Vector2(220, 60);
        menuRect.anchoredPosition = new Vector2(120, 0);

        Image menuImg = menuBtnObj.GetComponent<Image>();
        menuImg.color = new Color(0.2f, 0.6f, 1f, 1f);

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
        menuTMP.color = Color.white;

        // 给根Panel添加DeathPanel组件
        DeathPanel deathPanelScript = panelRoot.AddComponent<DeathPanel>();

        // 将生成的按钮引用赋值到 DeathPanel（如果组件存在）
        if (deathPanelScript != null)
        {
            deathPanelScript.restartButton = restartBtn;
            deathPanelScript.mainMenuButton = menuBtn;
        }

        // 默认隐藏，只在死亡时显示
        panelRoot.SetActive(false);

        // 在Inspector中设置引用
        EditorGUIUtility.PingObject(panelRoot);
        Selection.activeGameObject = panelRoot;

        EditorUtility.DisplayDialog("成功", 
            "Death Panel Template 已创建！\n\n" +
            "下一步：\n" +
            "1. 将此 Panel_Death 拖拽到 GameFlowController 的 \"Panel Death\" 字段\n" +
            "2. 在 Panel_Death 的 Inspector 中配置各项参数\n" +
            "3. 为不同的死因设置不同的背景素材和文案", 
            "确定");

        Debug.Log("Death Panel Template created successfully!");
    }
}

#endif
