using System.Collections.Generic;
using UnityEngine;

namespace AhmadAlzahrani.HierarchyStylerPro
{

    [System.Serializable]
    public class ColorDesign
    {
        [Header("Prefix Matching")]
        [Tooltip("If the GameObject name begins with this prefix, this style applies. Example: //, @, or ###")]
        public string keyChar = "//";

        [Header("Text Style")]
        public Color textColor = Color.white;
        public FontStyle fontStyle = FontStyle.Bold;
        public TextAnchor textAlignment = TextAnchor.MiddleLeft;

        [Header("Row Background")]
        public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        [Header("Custom Icon")]
        [Tooltip("Choose an icon to display next to GameObjects with this prefix.")]
        public IconType iconChoice = IconType.None;
        [Tooltip("Used only when Icon Choice = Custom")]
        public string customIcon = "";

        // Internal cash - not stored
        [System.NonSerialized] public GUIStyle cachedStyle;
    }

    public enum IconType
    {
        None,
        Tag,
        Star,
        Light,
        Sound,
        Gear,
        Brick,
        Skull,
        Target,
        Trophy,
        Custom
    }

    public static class IconTypeHelper
    {
        public static string GetEmoji(IconType type, string customIcon = "")
        {
            switch (type)
            {
                case IconType.Tag: return "🏷️";
                case IconType.Star: return "⭐";
                case IconType.Light: return "💡";
                case IconType.Sound: return "🔊";
                case IconType.Gear: return "⚙️";
                case IconType.Brick: return "🧱";
                case IconType.Skull: return "💀";
                case IconType.Target: return "🎯";
                case IconType.Trophy: return "🏆";
                case IconType.Custom: return string.IsNullOrEmpty(customIcon) ? "✨" : customIcon;
                default: return "";
            }
        }

        public static string GetEmoji(ColorDesign design)
        {
            if (design == null)
                return "";
            return GetEmoji(design.iconChoice, design.customIcon);
        }

        public static string GetEmoji(HierarchyTheme theme)
        {
            if (theme == null)
                return "";
            return GetEmoji(theme.groupIcon, theme.customIcon);
        }
    }


    [CreateAssetMenu(fileName = "NewHierarchyTheme", menuName = "Hierarchy Styler/Theme", order = 0)]
    public class HierarchyTheme : ScriptableObject
    {
        [Header("Custom Prefix-Based Styles")]
        public List<ColorDesign> designs = new List<ColorDesign>();

        [Header("Automatic Type Coloring")]
        public bool enableAutoTypeColoring = true;

        public Color cameraColor = new Color(0.2f, 0.6f, 1f, 0.8f);
        public Color lightColor = new Color(1f, 1f, 0.3f, 0.8f);
        public Color audioColor = new Color(1f, 0.6f, 0.2f, 0.8f);
        public Color rigidbodyColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        public Color uiColor = new Color(0.7f, 0.3f, 1f, 0.8f);

        [Header("Layout Settings")]
        [Tooltip("The vertical distance between each element and the next within the Hierarchy (in pixels).")]
        [Range(0f, 5f)]
        public float rowSpacing = 1f;

        [Header("Icons")]
        public bool showIcons = true;

        [Header("Groups & Separators")]
        [Tooltip("If the name begins with ---, it is considered a visual divider (Divider Row).")]
        public bool enableSeparators = true;
        public Color separatorColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        [Tooltip("If the name begins with ##, it is considered a Section Title / Group Header.")]
        public bool enableGroupHeaders = true;
        public Color groupHeaderBackground = new Color(0.15f, 0.15f, 0.15f, 1f);
        public Color groupHeaderTextColor = Color.white;
        [Tooltip("Choose an icon to display next to GameObjects with this prefix.")]
        public IconType groupIcon = IconType.None;
        [Tooltip("Used only when Icon Choice = Custom")]
        public string customIcon = "";
    }
}
