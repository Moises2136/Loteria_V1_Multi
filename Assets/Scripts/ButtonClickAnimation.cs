using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Este script le da a un botón (o cualquier objeto tocable) una pequeña
/// animación de "presión": se encoge un poco al tocarlo, y regresa
/// suavemente a su tamaño normal al soltarlo. Da la sensación de que el
/// botón responde físicamente al toque.
/// </summary>
public class ButtonClickAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    /// <summary>El tamaño (escala) original del objeto, guardado al iniciar.</summary>
    private Vector3 escalaOriginal;

    /// <summary>Qué tanto se encoge el botón al presionarlo (0.85 = 85% de su tamaño normal).</summary>
    public float escalaReducida = 0.85f;

    /// <summary>Qué tan rápida es la animación de encoger/regresar.</summary>
    public float velocidad = 10f;

    /// <summary>Referencia a la animación en curso, para poder detenerla si se vuelve a tocar antes de que termine.</summary>
    private Coroutine animacion;

    /// <summary>
    /// Al iniciar, guarda el tamaño original del objeto para poder
    /// regresar a él más adelante.
    /// </summary>
    void Start()
    {
        escalaOriginal = transform.localScale;
    }

    /// <summary>
    /// Se llama automáticamente cuando el jugador presiona (toca) este objeto.
    /// Anima el encogimiento hacia <see cref="escalaReducida"/>.
    /// </summary>
    /// <param name="eventData">Datos del evento de toque, provistos por Unity.</param>
    public void OnPointerDown(PointerEventData eventData)
    {
        // Se hace pequeño
        if (animacion != null) StopCoroutine(animacion);
        animacion = StartCoroutine(AnimarEscala(escalaOriginal * escalaReducida));
    }

    /// <summary>
    /// Se llama automáticamente cuando el jugador suelta el toque sobre
    /// este objeto. Anima el regreso al tamaño original.
    /// </summary>
    /// <param name="eventData">Datos del evento de toque, provistos por Unity.</param>
    public void OnPointerUp(PointerEventData eventData)
    {
        // Regresa a normal (con efecto suave)
        if (animacion != null) StopCoroutine(animacion);
        animacion = StartCoroutine(AnimarEscala(escalaOriginal));
    }

    /// <summary>
    /// Anima suavemente el tamaño del objeto hasta llegar al tamaño indicado.
    /// </summary>
    /// <param name="objetivo">El tamaño (escala) al que se quiere llegar.</param>
    IEnumerator AnimarEscala(Vector3 objetivo)
    {
        while (Vector3.Distance(transform.localScale, objetivo) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, objetivo, Time.deltaTime * velocidad);
            yield return null;
        }

        transform.localScale = objetivo;
    }
}