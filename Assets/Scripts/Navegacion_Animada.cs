using System.Collections;
using UnityEngine;

/// <summary>
/// Se pone en cada pantalla principal. Cada vez que se activa,
/// entra con fade + un deslizamiento horizontal corto.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class Navegacion_Animada : MonoBehaviour
{
    [Tooltip("Marcar solo en el menú principal (entra desde la izquierda, como 'atrás').")]
    public bool entraDesdeIzquierda = false;

    public float duracion = 0.25f;
    public float desplazamiento = 40f;

    [Tooltip("Si está apagado, la primera activación al iniciar el juego no se anima.")]
    public bool animarPrimeraVez = false;

    CanvasGroup cg;
    RectTransform rt;
    Vector2 posBase;
    bool primeraVez = true;
    Coroutine rutina;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        rt = (RectTransform)transform;
        posBase = rt.anchoredPosition;
    }

    void OnEnable()
    {
        if (primeraVez)
        {
            primeraVez = false;
            if (!animarPrimeraVez) return;
        }

        if (duracion <= 0f) return;
        rutina = StartCoroutine(Entrar());
    }

    void OnDisable()
    {
        // Si se apagó a media animación, lo deja limpio para la próxima vez
        if (rutina != null) { StopCoroutine(rutina); rutina = null; }
        if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; }
        if (rt != null) rt.anchoredPosition = posBase;
    }

    IEnumerator Entrar()
    {
        float lado = entraDesdeIzquierda ? -1f : 1f;
        Vector2 inicio = posBase + new Vector2(desplazamiento * lado, 0f);

        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        rt.anchoredPosition = inicio;

        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            float n = t / duracion;
            float p = 1f - Mathf.Pow(1f - n, 3f); // ease-out
            cg.alpha = p;
            rt.anchoredPosition = Vector2.Lerp(inicio, posBase, p);
            yield return null;
        }

        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        rt.anchoredPosition = posBase;
        rutina = null;
    }
}