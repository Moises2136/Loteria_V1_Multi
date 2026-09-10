using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla un botón de información (usualmente un ícono de
/// interrogación "?"): al presionarlo, muestra un panel con un mensaje
/// de ayuda que aparece con una animación de escala, se queda visible
/// unos segundos, y luego desaparece automáticamente con la misma animación.
/// </summary>
public class BotonInfo : MonoBehaviour
{
    /// <summary>Botón (usualmente con un ícono de interrogación) que muestra la información.</summary>
    [Header("Botón")]
    public Button botonInterrogacion;

    /// <summary>Panel que aparece con el mensaje de información.</summary>
    [Header("Panel de información")]
    public GameObject panelInfo;

    /// <summary>Texto donde se muestra el mensaje de ayuda.</summary>
    [Header("Texto de información")]
    public TMP_Text textoInfo;

    /// <summary>El mensaje de ayuda que se muestra al presionar el botón.</summary>
    [TextArea(3, 6)]
    public string mensaje = "Este es un mensaje para ayudar al usuario xd";

    /// <summary>Cuántos segundos permanece visible el panel antes de desaparecer solo.</summary>
    [Header("Configuración")]
    public float duracionVisible = 3f;    // segundos que permanece visible

    /// <summary>Qué tan rápida es la animación de aparecer/desaparecer.</summary>
    public float velocidadEscala = 8f;    // velocidad de la animación

    /// <summary>Referencia a la animación en curso, para poder detenerla si se presiona el botón de nuevo.</summary>
    private Coroutine coroutineActual;

    /// <summary>
    /// Al iniciar, deja el panel de información oculto (con escala en cero),
    /// coloca el mensaje configurado, y conecta el botón con su función.
    /// </summary>
    void Start()
    {
        if (panelInfo != null)
        {
            panelInfo.SetActive(false);
            panelInfo.transform.localScale = Vector3.zero;
        }

        if (textoInfo != null)
            textoInfo.text = mensaje;

        if (botonInterrogacion != null)
        {
            botonInterrogacion.onClick.RemoveAllListeners();
            botonInterrogacion.onClick.AddListener(MostrarInfo);
        }
    }

    /// <summary>
    /// Muestra el panel de información. Si ya se estaba mostrando (o
    /// desapareciendo) por una presión anterior del botón, cancela esa
    /// animación y empieza una nueva desde cero.
    /// </summary>
    public void MostrarInfo()
    {
        if (coroutineActual != null)
            StopCoroutine(coroutineActual);

        coroutineActual = StartCoroutine(FlujoInfo());
    }

    /// <summary>
    /// Controla toda la secuencia: activa el panel, lo anima para que
    /// aparezca, espera <see cref="duracionVisible"/> segundos, y luego
    /// lo anima para que desaparezca y lo desactiva.
    /// </summary>
    IEnumerator FlujoInfo()
    {
        // Activar y aparecer
        panelInfo.SetActive(true);
        yield return StartCoroutine(AnimarEscala(Vector3.zero, Vector3.one));

        // Esperar
        yield return new WaitForSeconds(duracionVisible);

        // Desaparecer y desactivar
        yield return StartCoroutine(AnimarEscala(Vector3.one, Vector3.zero));
        panelInfo.SetActive(false);
    }

    /// <summary>
    /// Anima suavemente la escala del panel de información desde un
    /// tamaño inicial hasta uno final, usando una curva de suavizado
    /// (<c>SmoothStep</c>) para que el movimiento se vea natural.
    /// </summary>
    /// <param name="desde">Escala inicial (por ejemplo, <c>Vector3.zero</c> para invisible).</param>
    /// <param name="hasta">Escala final (por ejemplo, <c>Vector3.one</c> para tamaño normal).</param>
    IEnumerator AnimarEscala(Vector3 desde, Vector3 hasta)
    {
        float t = 0f;
        panelInfo.transform.localScale = desde;

        while (t < 1f)
        {
            t += Time.deltaTime * velocidadEscala;
            panelInfo.transform.localScale = Vector3.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        panelInfo.transform.localScale = hasta;
    }
}