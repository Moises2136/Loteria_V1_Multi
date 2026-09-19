#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AhmadAlzahrani.HierarchyStylerPro
{

    [InitializeOnLoad]
    public static class HierarchyColorizer
    {
        static HierarchyStylerSettings runtimeSettings;
        static HierarchyTheme cachedTheme;
        static List<ColorDesign> cachedDesigns = new List<ColorDesign>();

        static double lastUpdateTime;
        static double interval = 1.5f; // default fallback

        static GUIStyle groupHeaderStyle; // For groups ##
        static string currentGroupParent = null;
        static Color currentGroupColor = Color.clear;
        static bool insideGroup = false;

        static HierarchyColorizer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            EditorApplication.update += UpdateCache;
            EditorApplication.delayCall += () =>
            {
                if (groupHeaderStyle == null)
                    RebuildStyles();
            };
        }

        static void RebuildStyles()
        {
            // Do not build any style before you are sure that Unity is ready.
            if (!AreEditorStylesReady())
                return;

            try
            {
                groupHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Bold
                };
                groupHeaderStyle.normal.textColor = Color.white;
            }
            catch
            {
                // If an error occurs while loading EditorStyles, temporarily create an empty style.
                groupHeaderStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Bold
                };
                groupHeaderStyle.normal.textColor = Color.white;
            }
        }

        static void UpdateCache()
        {
            // We control the timing (for example, every second interval)
            if (EditorApplication.timeSinceStartup - lastUpdateTime < interval)
                return;

            lastUpdateTime = EditorApplication.timeSinceStartup;

            // 1) try to find the settings in the scene.
            if (runtimeSettings == null)
            {
                runtimeSettings = Object.FindObjectOfType<HierarchyStylerSettings>();
            }

            // If there are no settings in the scene, we have no work to do right now.
            if (runtimeSettings == null)
            {
                cachedTheme = null;
                cachedDesigns.Clear();
                return;
            }

            // 2) If autoUpdate is turned off, it will not update.
            if (!runtimeSettings.autoUpdate)
                return;

            // 3) Set the interval from the script
            interval = runtimeSettings.updateInterval;

            // 4) Pin the current theme
            if (runtimeSettings.activeTheme != cachedTheme)
            {
                cachedTheme = runtimeSettings.activeTheme;
            }

            if (cachedTheme == null)
            {
                cachedDesigns.Clear();
                return;
            }

            // 5) If the Editor styles are not yet complete, do not complete them.
            // Sometimes, after the domain reload, EditorStyles itself is not fully ready.
            // That's why we protect ourselves with an additional condition
            if (!AreEditorStylesReady())
            {
                return;
            }

            // 6) Prepare the list
            cachedDesigns = cachedTheme.designs ?? new List<ColorDesign>();

            // 7) Prepare the appropriate style for each design.
            foreach (var d in cachedDesigns)
            {
                if (d == null) continue;

                // Fix transparency if the user selects a color with an alpha=0 background
                if (d.backgroundColor.a <= 0f)
                    d.backgroundColor.a = 1f;

                // Build the style if it hasn't been built before.
                if (d.cachedStyle == null)
                {
                    d.cachedStyle = BuildSafeGUIStyle(d);
                }

                // Even if the style reverts to null (there are still no ready EditorStyles for any reason), don't break it.
                if (d.cachedStyle != null)
                {
                    d.cachedStyle.alignment = d.textAlignment;
                    d.cachedStyle.fontStyle = d.fontStyle;
                    d.cachedStyle.normal.textColor = d.textColor;
                }
            }

            // 8) Prepare the section heading style based on the theme.
            if (cachedTheme.enableGroupHeaders)
            {
                if (groupHeaderStyle == null)
                    groupHeaderStyle = new GUIStyle();

                // We build it safely without assuming a ready-made bold label.
                if (EditorStyles.boldLabel != null)
                {
                    groupHeaderStyle.normal.textColor = cachedTheme.groupHeaderTextColor;
                    groupHeaderStyle.fontStyle = FontStyle.Bold;
                    groupHeaderStyle.alignment = TextAnchor.MiddleLeft;
                }
            }
        }


        static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (runtimeSettings == null || cachedTheme == null) return;

            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            string objectName = obj.name;

            // -------------------------------------------------
            // 1) separator (---something)
            // -------------------------------------------------
            if (cachedTheme.enableSeparators && objectName.StartsWith("---"))
            {

                EditorGUI.DrawRect(selectionRect, cachedTheme.separatorColor);


                var _labelRect = selectionRect;
                _labelRect.x += 4;
                if (Event.current.type == EventType.Repaint && AreEditorStylesReady())
                {
                    try
                    {
                        EditorGUI.DropShadowLabel(_labelRect, objectName.TrimStart('-').Trim(), EditorStyles.miniBoldLabel);
                    }
                    catch
                    {

                    }
                }


                return; // We will continue drawing the rest of the things here.
            }

            // -------------------------------------------------
            // 2) Group/section title (## something)
            // -------------------------------------------------
            if (cachedTheme.enableGroupHeaders && objectName.StartsWith("##"))
            {
                float spacing = cachedTheme != null ? cachedTheme.rowSpacing : 1f;
                Rect spacedRect = new Rect(selectionRect.x, selectionRect.y + spacing, selectionRect.width, selectionRect.height - spacing * 2);

                Color groupBg = cachedTheme.groupHeaderBackground;
                if (groupBg.a <= 0f)
                    groupBg = new Color(0.15f, 0.15f, 0.15f, 1f);

                EditorGUI.DrawRect(spacedRect, groupBg);

                string cleanName = objectName.TrimStart('#').TrimStart();

                var headerRect = selectionRect;
                headerRect.x += 20f;
                if (Event.current.type == EventType.Repaint && AreEditorStylesReady())
                {
                    try
                    {
                        EditorGUI.DropShadowLabel(headerRect, cleanName, groupHeaderStyle);
                    }
                    catch { }
                }

                string emoji = IconTypeHelper.GetEmoji(cachedTheme);
                Rect emojiRect = new Rect(selectionRect.x + 2, selectionRect.y + 1, 16, 16);
                GUIStyle emojiStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft
                };
                emojiStyle.normal.textColor = Color.white;
                if (cachedTheme.groupIcon != IconType.None)
                {
                    if (!string.IsNullOrEmpty(emoji))
                    {
                        GUI.Label(emojiRect, emoji, emojiStyle);
                    }
                }
                else
                {
                    GUI.Label(emojiRect, "📁", emojiStyle);
                }

                // ✅ Save this group and color it
                currentGroupParent = objectName;
                currentGroupColor = groupBg;
                insideGroup = true;
                return;
            }

            if (objectName.StartsWith("##"))
            {
                insideGroup = false;
                currentGroupParent = null;
                currentGroupColor = Color.clear;
            }

            // -------------------------------------------------
            // 2.5) Inherit color from current group (## parent)
            // -------------------------------------------------
            if (insideGroup && obj.transform.parent != null)
            {
                Transform parent = obj.transform.parent;
                if (parent.name == currentGroupParent)
                {
                    float spacing = cachedTheme != null ? cachedTheme.rowSpacing : 1f;
                    Rect spacedRect = new Rect(selectionRect.x, selectionRect.y + spacing, selectionRect.width, selectionRect.height - spacing * 2);

                    // 🎨 We color the background first
                    Color childBg = currentGroupColor * new Color(1f, 1f, 1f, 0.7f);
                    if (childBg.a <= 0.3f) childBg.a = 0.3f;
                    EditorGUI.DrawRect(spacedRect, childBg);

                    // 🏷️ We draw the name over the background
                    Rect labelRect2 = spacedRect;
                    labelRect2.x += 20f; // We move it slightly to the left (for the sake of the icon or spacing)
                    GUIStyle childStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft
                    };
                    childStyle.normal.textColor = Color.white; // You can make it a little lighter depending on the theme.

                    if (Event.current.type == EventType.Repaint && AreEditorStylesReady())
                    {
                        try
                        {
                            EditorGUI.DropShadowLabel(labelRect2, obj.name, childStyle);
                        }
                        catch { }
                    }
                }
            }


            // -------------------------------------------------
            // 3) Coloring according to the prefix
            //     Example: "@Player", "//UI", "### MANAGERS"
            //     We take the longest match
            // -------------------------------------------------
            ColorDesign matchedDesign = null;

            foreach (var design in cachedDesigns)
            {
                if (string.IsNullOrEmpty(design.keyChar)) continue;
                if (objectName.StartsWith(design.keyChar))
                {
                    // Prevent partial matching such as //// What is the key //
                    int prefixLen = design.keyChar.Length;
                    if (objectName.Length > prefixLen && objectName[prefixLen] == design.keyChar[0])
                        continue;

                    // Take the longest one
                    if (matchedDesign == null || design.keyChar.Length > matchedDesign.keyChar.Length)
                        matchedDesign = design;
                }
            }

            // We need the name clean, without the prefix.
            string displayName = objectName;
            if (matchedDesign != null)
            {
                displayName = objectName.Substring(matchedDesign.keyChar.Length).TrimStart();
            }


            bool drewCustomBackground = false;
            if (matchedDesign != null)
            {
                Color bg = matchedDesign.backgroundColor;
                Color fn = matchedDesign.textColor;
                if (fn.a <= 0f) fn.a = 1f;
                if (bg.a <= 0f) bg.a = 1f;
                float spacing = cachedTheme != null ? cachedTheme.rowSpacing : 1f;
                Rect spacedRect = new Rect(selectionRect.x, selectionRect.y + spacing, selectionRect.width, selectionRect.height - spacing * 2);
                EditorGUI.DrawRect(spacedRect, bg);
                drewCustomBackground = true;
            }

            // -------------------------------------------------
            // 🧱 Default Unity Icon (for plain GameObjects)
            // -------------------------------------------------
            if (matchedDesign != null && matchedDesign.iconChoice == IconType.None)
            {

                Rect emojiRect = new Rect(selectionRect.x + 2, selectionRect.y + 1, 16, 16);
                GUIStyle emojiStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft
                };
                emojiStyle.normal.textColor = Color.white;
                GUI.Label(emojiRect, "📦", emojiStyle);
            }

            // -------------------------------------------------
            // ✅ Custom Icon from ColorDesign (Emoji dropdown)
            // -------------------------------------------------
            if (matchedDesign != null && matchedDesign.iconChoice != IconType.None)
            {
                string emoji = IconTypeHelper.GetEmoji(matchedDesign);
                if (!string.IsNullOrEmpty(emoji))
                {
                    Rect emojiRect = new Rect(selectionRect.x + 2, selectionRect.y + 1, 16, 16);
                    GUIStyle emojiStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 12,
                        alignment = TextAnchor.MiddleLeft
                    };
                    emojiStyle.normal.textColor = Color.white;
                    GUI.Label(emojiRect, emoji, emojiStyle);
                }
            }

            // -------------------------------------------------
            // 4) Automatic coloring based on component type
            //     We only apply if there is a matchedDesign
            // -------------------------------------------------
            if (matchedDesign == null && cachedTheme.enableAutoTypeColoring)
            {
                Color? autoColor = null;

                if (obj.GetComponent<Camera>()) autoColor = cachedTheme.cameraColor;
                else if (obj.GetComponent<Light>()) autoColor = cachedTheme.lightColor;
                else if (obj.GetComponent<AudioSource>()) autoColor = cachedTheme.audioColor;
                else if (obj.GetComponent<Rigidbody>()) autoColor = cachedTheme.rigidbodyColor;
                else if (obj.GetComponent<Canvas>() || obj.GetComponent<RectTransform>()) autoColor = cachedTheme.uiColor;

                if (autoColor.HasValue)
                {
                    float spacing = cachedTheme != null ? cachedTheme.rowSpacing : 1f;
                    Rect spacedRect = new Rect(selectionRect.x, selectionRect.y + spacing, selectionRect.width, selectionRect.height - spacing * 2);
                    EditorGUI.DrawRect(spacedRect, autoColor.Value);

                    drewCustomBackground = true;
                }
            }

            // -------------------------------------------------
            // 5) Smart icons
            // -------------------------------------------------
            Rect iconRect = selectionRect;
            iconRect.width = 16;
            iconRect.height = 16;
            iconRect.x += 2;
            iconRect.y += 1;

            if (cachedTheme.showIcons)
            {
                GUIContent iconContent = null;

                if (obj.GetComponent<Camera>())
                    iconContent = EditorGUIUtility.IconContent("Camera Icon");
                else if (obj.GetComponent<Light>())
                    iconContent = EditorGUIUtility.IconContent("Light Icon");
                else if (obj.GetComponent<AudioSource>())
                    iconContent = EditorGUIUtility.IconContent("AudioSource Icon");
                else if (obj.GetComponent<Rigidbody>())
                    iconContent = EditorGUIUtility.IconContent("Rigidbody Icon");
                else if (obj.GetComponent<Canvas>() || obj.GetComponent<RectTransform>())
                    iconContent = EditorGUIUtility.IconContent("Canvas Icon");

                if (iconContent != null)
                {
                    GUI.Label(iconRect, iconContent);
                }
            }

            // -------------------------------------------------
            // 6) Draw the final text (name of the displayed item)
            // -------------------------------------------------

            // Where do we start writing the name? If there's an icon, we move the text slightly to the right.
            Rect labelRect = selectionRect;
            labelRect.x += 20f /*drewIcon ? 20f : 4f*/;

            if (matchedDesign != null && matchedDesign.cachedStyle != null)
            {
                // Use a custom design style
                var style = matchedDesign.cachedStyle;
                style.normal.textColor = matchedDesign.textColor; // just in case
                if (Event.current.type == EventType.Repaint && AreEditorStylesReady())
                {
                    try
                    {
                        EditorGUI.DropShadowLabel(labelRect, displayName, style);
                    }
                    catch
                    {

                    }
                }

            }
            else
            {
                // If there's no design:
                if (drewCustomBackground)
                {
                    GUIStyle autoStyle = new GUIStyle(EditorStyles.label);
                    autoStyle.fontStyle = FontStyle.Bold;
                    autoStyle.normal.textColor = Color.white;
                    if (Event.current.type == EventType.Repaint && AreEditorStylesReady())
                    {
                        try
                        {
                            EditorGUI.DropShadowLabel(labelRect, displayName, autoStyle);
                        }
                        catch
                        {

                        }
                    }

                }
            }



        }

        static bool AreEditorStylesReady()
        {
            // In some early timeframes, EditorStyles returns null internally.
            // We're testing safely. Any direct touch on EditorStyles.boldLabel/label
            // Without this protection, you could potentially destroy NullReference from within Unity.
            try
            {
                // The same access can throw NullReference from the depths.
                // We put it in try/catch.
                var _ = EditorStyles.label;
                var __ = EditorStyles.boldLabel;
                return (_ != null && __ != null);
            }
            catch
            {
                return false;
            }
        }

        static GUIStyle BuildSafeGUIStyle(ColorDesign d)
        {
            // We are trying to build GUIStyle based on EditorStyles.label if available.
            if (!AreEditorStylesReady())
                return null;

            GUIStyle s;
            try
            {
                s = new GUIStyle(EditorStyles.label);
            }
            catch
            {
                // If Unity rejects it because it's still waking up, we'll revert to a blank style instead of breaking it.
                s = new GUIStyle();
            }

            s.alignment = d.textAlignment;
            s.fontStyle = d.fontStyle;
            s.normal.textColor = d.textColor;
            return s;
        }

    }
}
#endif
