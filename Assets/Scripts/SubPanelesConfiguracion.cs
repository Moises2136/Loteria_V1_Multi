using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla pestañas dentro de un panel (por ejemplo, Configuración):
/// al presionar una pestaña muestra solo su subpanel, resalta su texto
/// y desliza la línea indicadora debajo de ella.
/// </summary>
public class SubPanelesConfiguracion : MonoBehaviour
{
    [Serializable]
    public class Pestana
    {
        public Button boton;
        public TMP_Text etiqueta;
        public GameObject panel;

        /// <summary>Opcional: sombra del botón, se "eleva" cuando la pestaña está activa.</summary>
        public Shadow sombra;
    }

    [Header("Pestañas (en orden)")]
    public Pestana[] pestanas;

    /// <summary>Pestaña que se abre al entrar al panel.</summary>
    public int pestanaInicial = 0;

    [Header("Indicador (línea debajo de la pestaña activa)")]
    public RectTransform indicador;
    public float duracionAnimacion = 0.15f;

    [Header("Estilo del texto")]
    public Color colorActiva = Color.black;
    public Color colorInactiva = new Color(0.55f, 0.55f, 0.55f);

    [Header("Sombra (la pestaña activa se eleva)")]
    public Vector2 sombraElevada = new Vector2(4, -4);

    Coroutine animacion;
    Coroutine[] animSombras;

    [Header("Animaciones (Sobre los subpaneles)")]
    public float duracionPanel = 0.25f;
    Coroutine[] animPaneles;

    void Awake()
    {
        animSombras = new Coroutine[pestanas.Length];
        animPaneles = new Coroutine[pestanas.Length];

        for (int i = 0; i < pestanas.Length; i++)
        {
            int indice = i;
            if (pestanas[i].boton != null)
            {
                pestanas[i].boton.onClick.RemoveAllListeners();
                pestanas[i].boton.onClick.AddListener(() => Seleccionar(indice));
            }
        }
    }

    void OnEnable()
    {
        // Al abrir el panel se regresa a la pestaña inicial, sin animación
        Seleccionar(pestanaInicial, true);
    }

    /// <summary>Selecciona una pestaña con animación (para usarse desde botones).</summary>
    public void Seleccionar(int indice) => Seleccionar(indice, false);

    void Seleccionar(int indice, bool instantaneo)
    {
        if (pestanas == null || indice < 0 || indice >= pestanas.Length) return;

        for (int i = 0; i < pestanas.Length; i++)
        {
            bool activa = i == indice;

            if (pestanas[i].panel != null)
                MostrarPanel(i, activa, instantaneo);

            if (pestanas[i].etiqueta != null)
            {
                pestanas[i].etiqueta.color = activa ? colorActiva : colorInactiva;
                pestanas[i].etiqueta.fontStyle = activa ? FontStyles.Bold : FontStyles.Normal;
            }

            AnimarSombra(i, activa ? sombraElevada : Vector2.zero, instantaneo);
        }

        MoverIndicador(pestanas[indice].boton, instantaneo);
    }

    void MostrarPanel(int i, bool activa, bool instantaneo)
    {
    GameObject panel = pestanas[i].panel;

    if (animPaneles[i] != null) { StopCoroutine(animPaneles[i]); animPaneles[i] = null; }

    RectMask2D mascara = panel.GetComponent<RectMask2D>();
    if (mascara == null) mascara = panel.AddComponent<RectMask2D>();

    if (instantaneo || !isActiveAndEnabled)
    {
        mascara.padding = Vector4.zero;
        panel.SetActive(activa);
        return;
    }

    if (activa)
    {
        mascara.padding = new Vector4(0, 5000, 0, 0); // todo tapado antes de mostrarlo
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();           // la cortina baja por encima del anterior
        animPaneles[i] = StartCoroutine(Cortina(i, mascara, (RectTransform)panel.transform));
    }
    else
    {
        // El anterior se queda debajo hasta que termine la cortina
        animPaneles[i] = StartCoroutine(OcultarDespues(i, panel));
    }
    }

    IEnumerator Cortina(int i, RectMask2D m, RectTransform rt)
    {
    float alto = rt.rect.height;
    for (float t = 0; t < duracionPanel; t += Time.unscaledDeltaTime)
    {
        float p = Mathf.SmoothStep(0f, 1f, t / duracionPanel);
        m.padding = new Vector4(0, alto * (1f - p), 0, 0); // x=izq, y=abajo, z=der, w=arriba
        yield return null;
    }
    m.padding = Vector4.zero;
    animPaneles[i] = null;
    }

    IEnumerator OcultarDespues(int i, GameObject panel)
    {
    yield return new WaitForSecondsRealtime(duracionPanel);
    panel.SetActive(false);
    animPaneles[i] = null;
    }

    void MoverIndicador(Button boton, bool instantaneo)
    {
        if (indicador == null || boton == null) return;

        // Asegura que los layouts ya calcularon posiciones y tamaños
        Canvas.ForceUpdateCanvases();

        RectTransform destino = (RectTransform)boton.transform;
        float xDestino = destino.position.x;
        float anchoDestino = destino.rect.width;

        if (animacion != null) StopCoroutine(animacion);

        if (instantaneo || !isActiveAndEnabled)
        {
            AplicarIndicador(xDestino, anchoDestino);
            return;
        }

        animacion = StartCoroutine(AnimarIndicador(xDestino, anchoDestino));
    }

    IEnumerator AnimarIndicador(float xDestino, float anchoDestino)
    {
        float xInicial = indicador.position.x;
        float anchoInicial = indicador.sizeDelta.x;
        float t = 0f;

        while (t < duracionAnimacion)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / duracionAnimacion);
            AplicarIndicador(Mathf.Lerp(xInicial, xDestino, p),
                             Mathf.Lerp(anchoInicial, anchoDestino, p));
            yield return null;
        }

        AplicarIndicador(xDestino, anchoDestino);
        animacion = null;
    }

    void AnimarSombra(int i, Vector2 destino, bool instantaneo)
    {
        Shadow s = pestanas[i].sombra;
        if (s == null) return;

        // Si ya había una animación en esta sombra, la cortamos para que no se encimen
        if (animSombras[i] != null) StopCoroutine(animSombras[i]);
        animSombras[i] = null;

        if (instantaneo || !isActiveAndEnabled)
        {
            s.effectDistance = destino;
            return;
        }

        animSombras[i] = StartCoroutine(AnimarSombraRutina(i, s, destino));
    }

    IEnumerator AnimarSombraRutina(int i, Shadow s, Vector2 destino)
    {
        Vector2 inicio = s.effectDistance;
        for (float t = 0; t < duracionAnimacion; t += Time.unscaledDeltaTime)
        {
            s.effectDistance = Vector2.Lerp(inicio, destino, t / duracionAnimacion);
            yield return null;
        }
        s.effectDistance = destino;
        animSombras[i] = null;
    }

    void AplicarIndicador(float x, float ancho)
    {
        Vector3 pos = indicador.position;
        pos.x = x;
        indicador.position = pos;
        indicador.sizeDelta = new Vector2(ancho, indicador.sizeDelta.y);
    }
}