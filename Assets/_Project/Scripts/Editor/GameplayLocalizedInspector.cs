using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
public class GameplayLocalizedInspector : Editor
{
    private static readonly Regex TokenRegex = new Regex("[A-Z]?[a-z]+|[0-9]+", RegexOptions.Compiled);

    private static readonly Dictionary<string, string> Exact = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "spawnInterval", "刷怪间隔" },
        { "spawnRadius", "刷怪半径" },
        { "maxAlive", "最大存活数" },
        { "spawnPerTick", "每次刷怪数量" },
        { "offscreenPadding", "屏幕外额外距离" },
        { "minSpawnDistance", "最小刷怪距离" },
        { "globalEnemyHpMultiplier", "全局敌人血量倍率" },
        { "globalEnemySpeedMultiplier", "全局敌人速度倍率" },
        { "moveSpeed", "移动速度" },
        { "fireInterval", "攻击间隔" },
        { "projectileSpeed", "子弹速度" },
        { "damage", "伤害" },
        { "lifeSeconds", "持续时间(秒)" },
        { "followSpeed", "跟随速度" },
        { "iFrameSeconds", "无敌帧时长" },
        { "maxHP", "最大生命值" },
        { "xpDrop", "经验掉落" },
        { "cashValue", "金币奖励" },
        { "healAmount", "回复量" },
        { "amount", "数量" },
        { "weightPercent", "权重百分比" }
    };

    private static readonly Dictionary<string, string> Words = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "spawn", "刷怪" },
        { "interval", "间隔" },
        { "radius", "半径" },
        { "max", "最大" },
        { "min", "最小" },
        { "alive", "存活" },
        { "count", "数量" },
        { "tick", "每次" },
        { "outside", "外部" },
        { "camera", "镜头" },
        { "view", "视野" },
        { "offscreen", "屏幕外" },
        { "padding", "额外距离" },
        { "distance", "距离" },
        { "enemy", "敌人" },
        { "hp", "生命" },
        { "health", "生命" },
        { "speed", "速度" },
        { "global", "全局" },
        { "round", "回合" },
        { "curve", "曲线" },
        { "pickup", "拾取物" },
        { "boss", "Boss" },
        { "player", "玩家" },
        { "projectile", "子弹" },
        { "projectiles", "子弹" },
        { "root", "根节点" },
        { "move", "移动" },
        { "follow", "跟随" },
        { "text", "文本" },
        { "intro", "开场" },
        { "clear", "通关" },
        { "overlay", "遮罩" },
        { "fade", "淡入淡出" },
        { "seconds", "秒" },
        { "auto", "自动" },
        { "collect", "吸取" },
        { "delay", "延迟" },
        { "wait", "等待" },
        { "ui", "界面" },
        { "level", "等级" },
        { "xp", "经验" },
        { "debt", "债务" },
        { "cash", "现金" },
        { "due", "应付" },
        { "remaining", "剩余" },
        { "duration", "时长" },
        { "base", "基础" },
        { "step", "增量" },
        { "growth", "增长" },
        { "pool", "池" },
        { "shop", "商店" },
        { "panel", "面板" },
        { "title", "标题" },
        { "debug", "调试" },
        { "key", "按键" },
        { "reset", "重置" },
        { "stats", "属性" },
        { "knockback", "击退" },
        { "chance", "概率" },
        { "multiplier", "倍率" },
        { "angle", "角度" },
        { "spread", "散射" },
        { "orbit", "环绕" },
        { "cooldown", "冷却" },
        { "target", "目标" },
        { "prefab", "预制体" },
        { "sfx", "音效" },
        { "volume", "音量" },
        { "resolution", "分辨率" },
        { "windowed", "窗口化" }
    };

    public override void OnInspectorGUI()
    {
        serializedObject.UpdateIfRequiredOrScript();

        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (iterator.propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(iterator, true);
                continue;
            }

            if (HasLocalizedLabelAttribute(iterator))
            {
                EditorGUILayout.PropertyField(iterator, true);
                continue;
            }

            string localized = TranslatePropertyPath(iterator.name);
            if (string.IsNullOrEmpty(localized))
            {
                EditorGUILayout.PropertyField(iterator, true);
                continue;
            }

            GUIContent content = new GUIContent($"{iterator.displayName} / {localized}", iterator.tooltip);
            EditorGUILayout.PropertyField(iterator, content, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private bool HasLocalizedLabelAttribute(SerializedProperty property)
    {
        FieldInfo fieldInfo = ResolveFieldInfo(target.GetType(), property.propertyPath);
        if (fieldInfo == null)
            return false;

        return fieldInfo.GetCustomAttribute<LocalizedLabelAttribute>(true) != null;
    }

    private static string TranslatePropertyPath(string propertyName)
    {
        if (Exact.TryGetValue(propertyName, out string exact))
            return exact;

        MatchCollection matches = TokenRegex.Matches(propertyName);
        if (matches.Count == 0)
            return null;

        List<string> cn = new List<string>(matches.Count);
        for (int i = 0; i < matches.Count; i++)
        {
            string token = matches[i].Value;
            if (Words.TryGetValue(token, out string mapped))
                cn.Add(mapped);
        }

        if (cn.Count == 0)
            return null;

        return string.Join(" ", cn);
    }

    private static FieldInfo ResolveFieldInfo(Type type, string propertyPath)
    {
        if (type == null || string.IsNullOrEmpty(propertyPath))
            return null;

        string[] parts = propertyPath.Split('.');
        Type currentType = type;
        FieldInfo fieldInfo = null;

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];
            if (part == "Array")
            {
                i++; // Skip "data[n]"
                continue;
            }

            int bracketIndex = part.IndexOf('[');
            if (bracketIndex >= 0)
                part = part.Substring(0, bracketIndex);

            fieldInfo = GetFieldFromTypeHierarchy(currentType, part);
            if (fieldInfo == null)
                return null;

            currentType = fieldInfo.FieldType;
            if (currentType.IsArray)
            {
                currentType = currentType.GetElementType();
            }
            else if (currentType.IsGenericType && typeof(IList).IsAssignableFrom(currentType))
            {
                Type[] args = currentType.GetGenericArguments();
                currentType = args.Length > 0 ? args[0] : currentType;
            }
        }

        return fieldInfo;
    }

    private static FieldInfo GetFieldFromTypeHierarchy(Type type, string fieldName)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        while (type != null)
        {
            FieldInfo field = type.GetField(fieldName, flags);
            if (field != null)
                return field;
            type = type.BaseType;
        }

        return null;
    }
}
