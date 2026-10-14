using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Traduce varios textos fijos de un prefab desde un solo componente.
/// Se pone en la raíz del prefab y se llena la lista con cada texto y sus
/// 4 traducciones (igual que la lista de LanguageManager). Se aplica al
/// aparecer el prefab y cada vez que cambia el idioma.
/// </summary>
public class TextoPrefabs : MonoBehaviour
{
    /// <summary>Textos fijos del prefab y sus traducciones.</summary>
    public List<LocalizedText> textos = new List<LocalizedText>();

    void OnEnable()
    {
        LanguageManager.OnIdiomaAplicado += Aplicar;

        if (LanguageManager.Instance != null)
            Aplicar(LanguageManager.Instance.currentLanguage);
    }

    void OnDisable()
    {
        LanguageManager.OnIdiomaAplicado -= Aplicar;
    }

    /// <summary>
    /// Pone cada texto de la lista en el idioma indicado.
    /// Si una traducción está vacía, usa la de español.
    /// </summary>
    void Aplicar(LanguageManager.Language idioma)
    {
        foreach (LocalizedText t in textos)
        {
            if (t == null || t.tmpText == null) continue;

            string valor = idioma switch
            {
                LanguageManager.Language.English => t.english,
                LanguageManager.Language.French  => t.french,
                LanguageManager.Language.Italian => t.italian,
                _ => t.spanish
            };

            t.tmpText.text = string.IsNullOrEmpty(valor) ? t.spanish : valor;
        }
    }
}