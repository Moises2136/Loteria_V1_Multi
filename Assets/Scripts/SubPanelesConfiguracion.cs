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

    void Awake()
    {
        animSombras = new Coroutine[pestanas.Length];

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
                pestanas[i].panel.SetActive(activa);

            if (pestanas[i].etiqueta != null)
            {
                pestanas[i].etiqueta.color = activa ? colorActiva : colorInactiva;
                pestanas[i].etiqueta.fontStyle = activa ? FontStyles.Bold : FontStyles.Normal;
            }

            AnimarSombra(i, activa ? sombraElevada : Vector2.zero, instantaneo);
        }

        MoverIndicador(pestanas[indice].boton, instantaneo);
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