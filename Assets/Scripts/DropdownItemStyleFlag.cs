using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DropdownItemStyleFlag : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image flag;
    [SerializeField] private GameObject separator;
    [SerializeField] private Color activeColor = new Color(0.2f, 0.2f, 0.2f);
    [SerializeField] private Color inactiveColor = new Color(0.47f, 0.47f, 0.47f);

    private void Start()
    {
        // El primer hijo de Content es el Item plantilla (apagado),
        // así que la opción 0 está en el índice 1
        int index = transform.GetSiblingIndex() - 1;

        if (flag != null && LanguageManager.Instance != null)
            flag.sprite = LanguageManager.Instance.Flags[index];

        label.fontStyle = toggle.isOn ? FontStyles.Bold : FontStyles.Normal;
        label.color = toggle.isOn ? activeColor : inactiveColor;

        // La última opción no lleva línea abajo
        if (separator != null)
            separator.SetActive(transform.GetSiblingIndex() != transform.parent.childCount - 1);
    }
}