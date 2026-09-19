#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AhmadAlzahrani.HierarchyStylerPro
{

    public class HierarchyStylerWindow : EditorWindow
    {
        HierarchyStylerSettings settings;

        [MenuItem("Tools/Hierarchy Styler/Settings")]
        public static void OpenWindow()
        {
            var window = GetWindow<HierarchyStylerWindow>("Hierarchy Styler");
            window.minSize = new Vector2(320, 400);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Hierarchy Styler Pro", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Scene settings
            settings = (HierarchyStylerSettings)EditorGUILayout.ObjectField(
                "Settings (Scene Object)",
                settings,
                typeof(HierarchyStylerSettings),
                true
            );

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "Add the HierarchyStylerSettings component to any GameObject in your scene, then drag it here.",
                    MessageType.Info
                );
                return;
            }

            EditorGUILayout.Space(6);
            DrawRuntimeControls();
            EditorGUILayout.Space(10);
            DrawThemeControls();
        }

        void DrawRuntimeControls()
        {
            EditorGUILayout.LabelField("Runtime / Refresh", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            bool auto = EditorGUILayout.Toggle("Auto Update", settings.autoUpdate);
            float interval = EditorGUILayout.Slider("Update Interval (sec)", settings.updateInterval, 0.1f, 5f);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Edit Hierarchy Styler Settings");
                settings.autoUpdate = auto;
                settings.updateInterval = interval;
                EditorUtility.SetDirty(settings);
            }
        }

        void DrawThemeControls()
        {
            EditorGUILayout.LabelField("Active Theme", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var newTheme = (HierarchyTheme)EditorGUILayout.ObjectField(
                "Theme Asset",
                settings.activeTheme,
                typeof(HierarchyTheme),
                false
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Assign Hierarchy Theme");
                settings.activeTheme = newTheme;
                EditorUtility.SetDirty(settings);
            }

            if (settings.activeTheme == null)
            {
                EditorGUILayout.HelpBox("Choose a theme or create one from Create > Hierarchy Styler > Theme", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(6);

            // toggles inside the theme
            EditorGUI.BeginChangeCheck();

            settings.activeTheme.enableAutoTypeColoring =
                EditorGUILayout.Toggle("Auto Type Coloring", settings.activeTheme.enableAutoTypeColoring);

            settings.activeTheme.showIcons =
                EditorGUILayout.Toggle("Show Icons", settings.activeTheme.showIcons);

            settings.activeTheme.enableSeparators =
                EditorGUILayout.Toggle("Enable Separators (---)", settings.activeTheme.enableSeparators);

            settings.activeTheme.enableGroupHeaders =
                EditorGUILayout.Toggle("Enable Group Headers (##)", settings.activeTheme.enableGroupHeaders);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings.activeTheme);
            }

            EditorGUILayout.Space(6);

            // preview of the main colors
            EditorGUILayout.LabelField("Auto-Type Colors", EditorStyles.miniBoldLabel);
            settings.activeTheme.cameraColor = EditorGUILayout.ColorField("Camera Color", settings.activeTheme.cameraColor);
            settings.activeTheme.lightColor = EditorGUILayout.ColorField("Light Color", settings.activeTheme.lightColor);
            settings.activeTheme.audioColor = EditorGUILayout.ColorField("Audio Color", settings.activeTheme.audioColor);
            settings.activeTheme.rigidbodyColor = EditorGUILayout.ColorField("Rigidbody Color", settings.activeTheme.rigidbodyColor);
            settings.activeTheme.uiColor = EditorGUILayout.ColorField("UI / Canvas Color", settings.activeTheme.uiColor);
            settings.activeTheme.rowSpacing = EditorGUILayout.Slider("Row Spacing", settings.activeTheme.rowSpacing, 0f, 5f);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Force Refresh Hierarchy"))
            {
                // Trick: Any repaint of the hierarchy is done when you change the selection.
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Edit the 'designs' menu directly within the Theme to add your own custom prefixes, such as @, //, or ###", MessageType.Info);
        }
    }
}
#endif
