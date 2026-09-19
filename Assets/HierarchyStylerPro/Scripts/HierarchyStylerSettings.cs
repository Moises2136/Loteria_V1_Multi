#if UNITY_EDITOR
using UnityEngine;

namespace AhmadAlzahrani.HierarchyStylerPro
{

    public class HierarchyStylerSettings : MonoBehaviour
    {
        [Header("Live Update / Auto Refresh")]
        public bool autoUpdate = true;
        [Tooltip("How many seconds between each cache update?")]
        [Range(0.1f, 5f)]
        public float updateInterval = 1.5f;

        [Header("Active Theme")]
        public HierarchyTheme activeTheme;

        private void Reset()
        {
            autoUpdate = true;
            updateInterval = 1.5f;
        }
    }
}
#endif
