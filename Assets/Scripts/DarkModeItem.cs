using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Se pone en la raíz de un prefab (o de cualquier panel) para que siga el modo oscuro.
/// Al activarse se registra en <see cref="DarkModeManager"/> y recibe los colores actuales,
/// así que funciona con prefabs instanciados en runtime (por ejemplo, la lista desplegable
/// de un Dropdown) y con paneles que estaban apagados cuando cambió el modo.
/// Al desactivarse deja de recibir cambios.
/// <para>
/// Las listas son del propio prefab: se llenan abriéndolo en el modo de edición de prefabs
/// (o, en un Dropdown, seleccionando el Template aunque esté apagado).
/// No incluyas imágenes que deban conservar su color (fotos de plantas, banderas, botones verdes).
/// </para>
/// </summary>
public class DarkModeItem : MonoBehaviour
{
    /// <summary>Fondos del prefab que toman el color de fondo del modo actual.</summary>
    [Header("Fondos")]
    [SerializeField] private List<Image> backgrounds = new List<Image>();

    /// <summary>Textos normales del prefab que toman el color de texto del modo actual.</summary>
    [Header("Textos")]
    [SerializeField] private List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();

    /// <summary>Imágenes (íconos) del prefab que toman el color de texto del modo actual.</summary>
    [Header("Imágenes")]
    [SerializeField] private List<Image> images = new List<Image>();

    /// <summary>Texto que escribe el usuario dentro de los inputs del prefab.</summary>
    [Header("Inputs: texto escrito")]
    [SerializeField] private List<TextMeshProUGUI> textosInput = new List<TextMeshProUGUI>();

    /// <summary>Placeholders de los inputs del prefab.</summary>
    [Header("Inputs: placeholders")]
    [SerializeField] private List<TextMeshProUGUI> placeholders = new List<TextMeshProUGUI>();

    /// <summary>Sombras u Outlines del prefab que cambian a gris, conservando su transparencia.</summary>
    [Header("Sombras")]
    [SerializeField] private List<Shadow> sombras = new List<Shadow>();

    /// <summary>
    /// Toggles o botones del prefab cuyo tinte (normal, hover, presionado) sigue el modo.
    /// En un Dropdown, aquí va el Toggle del "Item". Su Image debe estar en blanco.
    /// </summary>
    [Header("Selectables (ítems de dropdown, toggles)")]
    [SerializeField] private List<Selectable> selectables = new List<Selectable>();

    /// <summary>Al activarse, se registra en el manager y recibe el modo actual.</summary>
    void OnEnable()
    {
        DarkModeManager.Registrar(this);
    }

    /// <summary>Al desactivarse, deja de recibir cambios de modo.</summary>
    void OnDisable()
    {
        DarkModeManager.Quitar(this);
    }

    /// <summary>Aplica los colores del modo actual a todo lo registrado en este prefab.</summary>
    /// <param name="fondo">Color de fondo del modo actual.</param>
    /// <param name="texto">Color de texto del modo actual.</param>
    /// <param name="placeholder">Color de placeholder del modo actual.</param>
    /// <param name="sombra">Gris de las sombras del modo actual (solo su RGB).</param>
    public void Aplicar(Color fondo, Color texto, Color placeholder, Color sombra)
    {
        foreach (Image b in backgrounds)
            if (b != null) b.color = fondo;

        foreach (TextMeshProUGUI t in texts)
            if (t != null) t.color = texto;

        foreach (Image i in images)
            if (i != null) i.color = texto;

        foreach (TextMeshProUGUI t in textosInput)
            if (t != null) t.color = texto;

        foreach (TextMeshProUGUI p in placeholders)
            if (p != null) p.color = placeholder;

        foreach (Shadow s in sombras)
            DarkModeManager.AplicarSombra(s, sombra);

        foreach (Selectable s in selectables)
            DarkModeManager.AplicarSelectable(s, fondo, texto);
    }
}