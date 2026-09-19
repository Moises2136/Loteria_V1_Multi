#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AhmadAlzahrani.HierarchyStylerPro
{

    [CustomPropertyDrawer(typeof(IconType))]
    public class IconTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // نعرض الـEnum العادي
            string[] enumNames = property.enumDisplayNames;
            GUIContent[] displayed = new GUIContent[enumNames.Length];

            for (int i = 0; i < enumNames.Length; i++)
            {
                IconType value = (IconType)i;
                string emoji = IconTypeHelper.GetEmoji(value);
                displayed[i] = new GUIContent($"{emoji} {enumNames[i]}");
            }

            int newIndex = EditorGUI.Popup(position, new GUIContent(label.text), property.enumValueIndex, displayed);
            if (newIndex != property.enumValueIndex)
                property.enumValueIndex = newIndex;

            EditorGUI.EndProperty();
        }
    }
#endif
}