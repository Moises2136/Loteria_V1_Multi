using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla cómo se ve una carta en la pantalla (UI).
/// Se encarga de mostrar la imagen y el nombre de la carta,
/// y avisa en la consola si algo no está bien configurado.
/// </summary>
public class CardCartaUI : MonoBehaviour
{
    /// <summary>
    /// Aquí se guarda la imagen de la carta.
    /// Se debe arrastrar el componente "Image" desde el Inspector de Unity.
    /// </summary>
    public Image imgCarta;

    /// <summary>
    /// Aquí se guarda el texto con el nombre de la carta.
    /// Se debe arrastrar el componente de texto (TMP_Text) desde el Inspector.
    /// </summary>
    public TMP_Text txtNombreCarta;

    /// <summary>
    /// Esta función sirve para "llenar" la carta con su nombre y su imagen.
    /// Se usa cuando quieres mostrar una carta distinta en pantalla.
    /// </summary>
    /// <param name="nombre">El nombre que se va a mostrar en la carta.</param>
    /// <param name="sprite">La imagen que se va a mostrar en la carta.</param>
    /// <remarks>
    /// Si "txtNombreCarta" o "imgCarta" no están asignados en el Inspector,
    /// el juego no truena: solo se muestra una advertencia en la consola.
    /// </remarks>
    public void Configurar(string nombre, Sprite sprite)
    {
        // Mensaje de aviso en consola para saber qué carta se está configurando
        Debug.Log("Configurando CardCartaUI: " + nombre);

        // Si el texto está asignado, le ponemos el nombre nuevo
        if (txtNombreCarta != null)
        {
            txtNombreCarta.text = nombre;
        }
        else
        {
            Debug.LogWarning("txtNombreCarta no está asignado en el prefab.");
        }

        // Si la imagen está asignada, le ponemos el sprite nuevo
        if (imgCarta != null)
        {
            imgCarta.sprite = sprite;
            imgCarta.color = Color.white; // Aseguramos que se vea con color normal, sin transparencia
            imgCarta.preserveAspect = true; // Evita que la imagen se vea estirada o deformada
        }
        else
        {
            Debug.LogWarning("imgCarta no está asignado en el prefab.");
        }
    }
}