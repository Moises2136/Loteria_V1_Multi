using System.Collections;
using UnityEngine;

/// <summary>
/// Se pone en la raíz de un popup (la que tiene el fondo oscuro).
/// Cada vez que se activa, el fondo aparece con fade y la tarjeta
/// crece con un pequeño rebote ("pop"). No toca ninguna otra lógica.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class POP_Animacion : MonoBehaviour
{
    [Tooltip("Tarjeta que hace el pop (ej. Panel_Code). Su Pivot debe ser (0.5, 0.5).")]
    public RectTransform tarjeta;

    [Min(0f)] public float duracion = 0.25f;
    [Range(0.5f, 1f)] public float escalaInicial = 0.8f;

    CanvasGroup cg;
    Vector3 escalaOriginal = Vector3.one;
    Coroutine rutina;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (tarjeta != null) escalaOriginal = tarjeta.localScale;
    }

    void OnEnable()
    {
        if (duracion <= 0f) return;
        rutina = StartCoroutine(Abrir());
    }

    void OnDisable()
    {
        // Si se apaga a media animación, lo deja limpio para la próxima vez
        if (rutina != null) { StopCoroutine(rutina); rutina = null; }
        if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; }
        if (tarjeta != null) tarjeta.localScale = escalaOriginal;
    }

    IEnumerator Abrir()
    {
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        Aplicar(escalaInicial);

        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            float n = t / duracion;
            cg.alpha = Mathf.SmoothStep(0f, 1f, n);
            Aplicar(Mathf.LerpUnclamped(escalaInicial, 1f, EaseOutBack(n)));
            yield return null;
        }

        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        Aplicar(1f);
        rutina = null;
    }

    void Aplicar(float factor)
    {
        if (tarjeta != null) tarjeta.localScale = escalaOriginal * factor;
    }

    float EaseOutBack(float x)
    {
        x = Mathf.Clamp01(x);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
    }
}