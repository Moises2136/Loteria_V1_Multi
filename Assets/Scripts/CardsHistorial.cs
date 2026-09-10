using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla un espacio del historial de cartas cantadas
/// (los 3 slots que vimos en <c>PanelJuegaLoteriaSoloUI</c> y
/// <c>PanelJuegaLoteriaMultiUI</c>). Cada instancia muestra una carta
/// imagen y nombre, y se puede limpiar o copiar desde otro slot —
/// esto último es justo lo que se usa para "correr" las cartas del
/// historial un lugar hacia atrás cuando se canta una nueva.
/// </summary>
public class CardsHistorial : MonoBehaviour
{
    /// <summary>Imagen de la carta mostrada en este slot del historial.</summary>
    public Image imagen;

    /// <summary>Texto con el nombre de la carta mostrada en este slot.</summary>
    public TMP_Text nombre;

    /// <summary>
    /// Llena este slot con una carta específica (imagen y nombre), y lo
    /// activa en pantalla. Si no hay imagen, deja el slot transparente
    /// en vez de mostrar un espacio en blanco.
    /// </summary>
    /// <param name="sprite">Imagen de la carta a mostrar.</param>
    /// <param name="texto">Nombre de la carta a mostrar.</param>
    public void SetData(Sprite sprite, string texto)
    {
        if (imagen != null)
        {
            imagen.sprite = sprite;
            imagen.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            imagen.preserveAspect = true;
        }

        if (nombre != null)
            nombre.text = texto;

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Vacía este slot quita imagen y texto y lo oculta, dejándolo
    /// listo como si nunca hubiera mostrado ninguna carta.
    /// </summary>
    public void ClearData()
    {
        if (imagen != null)
        {
            imagen.sprite = null;
            imagen.color = new Color(1f, 1f, 1f, 0f);
        }

        if (nombre != null)
            nombre.text = "";

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Copia la carta imagen y nombre de otro slot del historial hacia
    /// este. Si el otro slot está vacío o no tiene nada que copiar, este
    /// slot también se deja vacío.
    /// </summary>
    /// <param name="other">El slot del historial del que se va a copiar la carta.</param>
    /// <remarks>
    /// Este método es la pieza clave detrás de <c>AgregarHistorial</c> en
    /// los paneles de juego: para "correr" las 3 cartas del historial,
    /// primero se copia el slot 2 hacia el slot 1, luego el slot 3 hacia
    /// el slot 2, y por último se llena el slot 3 con la carta nueva.
    /// </remarks>
    public void CopyFrom(CardsHistorial other)
    {
        if (other == null)
        {
            ClearData();
            return;
        }

        Sprite sprite = null;
        string texto = "";

        if (other.imagen != null)
            sprite = other.imagen.sprite;

        if (other.nombre != null)
            texto = other.nombre.text;

        bool tieneAlgo = sprite != null || !string.IsNullOrWhiteSpace(texto);

        if (!tieneAlgo)
        {
            ClearData();
            return;
        }

        SetData(sprite, texto);
    }
}