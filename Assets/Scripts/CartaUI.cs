using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla cómo se ve una carta en la pantalla (UI).
/// Se encarga de mostrar la imagen y el nombre de la carta.
/// </summary>
public class CartaUI : MonoBehaviour
{
    // =========================
    // ELEMENTOS UI
    // =========================

    /// <summary>
    /// Aquí se guarda la imagen de la carta.
    /// Se debe arrastrar el componente "Image" desde el Inspector de Unity.
    /// </summary>
    public Image imagen;

    /// <summary>
    /// Aquí se guarda el texto con el nombre de la carta.
    /// Se debe arrastrar el componente de texto (TMP_Text) desde el Inspector.
    /// </summary>
    public TMP_Text nombre;

    // =========================
    // CONFIGURACIÓN
    // =========================

    /// <summary>
    /// Esta función sirve para "llenar" la carta con su imagen y su nombre.
    /// Se usa cuando quieres cambiar qué carta se muestra en pantalla.
    /// </summary>
    /// <param name="sprite">La imagen que se va a mostrar en la carta.</param>
    /// <param name="texto">El nombre que se va a mostrar debajo o encima de la imagen.</param>
    /// <remarks>
    /// Si "imagen" o "nombre" no están asignados en el Inspector,
    /// esa parte simplemente no se actualiza (no truena el juego, solo no hace nada).
    /// </remarks>
    public void SetData(Sprite sprite, string texto)
    {
        // Si tenemos una imagen asignada, le ponemos el sprite nuevo
        if (imagen != null)
            imagen.sprite = sprite;

        // Si tenemos un texto asignado, le ponemos el nombre nuevo
        if (nombre != null)
            nombre.text = texto;
    }
}