using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animación de la pantalla de ganador.
/// <para>
/// Al activarse el objeto: la corona entra con un "pop" (crece con rebote) y, al
/// terminar, lanza destellos en forma de estrella de 4 puntas sobre los laureles.
/// Cada destello aparece en un punto aleatorio del arco, crece, gira, sube un poco
/// y se apaga; luego reaparece en otro lugar tras una espera aleatoria. Al inicio
/// sale una ráfaga y después continúan de forma constante. Mientras tanto el número
/// pulsa suavemente y las estrellas parpadean desfasadas.
/// </para>
/// <para>
/// No necesita sprites extra: si el destello no tiene imagen, genera una estrella
/// brillante por código. Usa <c>unscaledDeltaTime</c>, así que funciona aunque el
/// juego esté en pausa (<c>Time.timeScale = 0</c>).
/// </para>
/// </summary>
public class Winner_animacion : MonoBehaviour
{
    // =========================
    // ELEMENTOS
    // =========================

    /// <summary>Grupo completo de la corona (laureles, número, estrellas). Pivot en (0.5, 0.5).</summary>
    [Header("Elementos")]
    [Tooltip("Grupo completo (laureles + número + estrellas). Pivot en (0.5, 0.5).")]
    public RectTransform corona;

    /// <summary>Número que pulsa (opcional).</summary>
    public RectTransform numero;

    /// <summary>Estrellas que parpadean (opcional).</summary>
    public RectTransform[] estrellas;

    /// <summary>Imagen base del destello, hija de la corona y centrada. Sin sprite se genera uno.</summary>
    [Tooltip("Imagen base del destello, hija de la corona y centrada. Sin sprite se genera uno.")]
    public Image destello;

    // =========================
    // ENTRADA
    // =========================

    /// <summary>Duración del pop de entrada de la corona (0 = sin animación).</summary>
    [Header("Entrada")]
    [Min(0f)] public float duracionEntrada = 0.5f;

    /// <summary>Tamaño relativo desde el que arranca la corona (0.3 = 30 % del tamaño final).</summary>
    [Range(0f, 1f)] public float escalaInicial = 0.3f;

    // =========================
    // DESTELLOS
    // =========================

    /// <summary>Radio del arco donde aparecen, en píxeles del canvas, hasta el centro de los laureles.</summary>
    [Header("Destellos")]
    [Tooltip("Radio en píxeles del canvas, mide hasta el centro de los laureles.")]
    public float radio = 120f;

    /// <summary>Qué tanto se aleja del arco al aparecer (± píxeles), para que no salgan en línea.</summary>
    [Min(0f)] public float variacionRadio = 25f;

    /// <summary>Tamaño del arco donde pueden aparecer, en grados (180 = media vuelta).</summary>
    [Range(30f, 360f)] public float arco = 200f;

    /// <summary>Si es verdadero el arco pasa por abajo (laureles); si no, por arriba.</summary>
    public bool arcoInferior = true;

    /// <summary>Cantidad de destellos activos al mismo tiempo.</summary>
    [Min(1)] public int cantidad = 10;

    /// <summary>Tamaño máximo de un destello en píxeles (el mínimo es 50 % de este).</summary>
    public float tamano = 45f;

    /// <summary>Duración de vida de cada destello, en segundos (mínimo y máximo).</summary>
    public Vector2 vida = new Vector2(0.6f, 1.2f);

    /// <summary>Espera antes de reaparecer, en segundos (mínimo y máximo).</summary>
    public Vector2 espera = new Vector2(0.1f, 0.9f);

    /// <summary>Retraso máximo de la ráfaga inicial, en segundos.</summary>
    [Min(0f)] public float retrasoInicial = 0.4f;

    /// <summary>Píxeles que sube cada destello durante su vida.</summary>
    public float deriva = 20f;

    /// <summary>Velocidad máxima de giro, en grados por segundo (hacia cualquier lado).</summary>
    public float giro = 90f;

    /// <summary>Color del destello (el alpha se ajusta solo).</summary>
    public Color colorBrillo = new Color(1f, 0.88f, 0.45f, 1f);

    // =========================
    // PULSOS
    // =========================

    /// <summary>Cuánto crece y se encoge el número al pulsar (0.05 = ±5 %).</summary>
    [Header("Pulsos")]
    public float pulsoNumero = 0.05f;

    /// <summary>Velocidad del pulso del número.</summary>
    public float velocidadPulso = 2f;

    /// <summary>Cuánto varía el tamaño de las estrellas al parpadear.</summary>
    public float parpadeoEstrellas = 0.25f;

    // =========================
    // ESTADO INTERNO
    // =========================

    /// <summary>Datos de un destello individual.</summary>
    private class Chispa
    {
        public Image imagen;
        public RectTransform rt;
        public float espera;     // segundos que faltan para aparecer
        public float tiempo;     // segundos que lleva vivo
        public float duracion;   // segundos totales de vida
        public Vector2 origen;   // posición donde nació
        public float tam;        // tamaño máximo de este destello
        public float rotacion;   // ángulo actual
        public float velGiro;    // velocidad de giro
    }

    /// <summary>Todos los destellos (la imagen base es el primero).</summary>
    private Chispa[] chispas;

    /// <summary>Escalas originales, para respetarlas al animar y al limpiar.</summary>
    private Vector3 escalaCorona = Vector3.one;
    private Vector3 escalaNumero = Vector3.one;
    private Vector3[] escalaEstrellas;

    /// <summary>Tiempo transcurrido desde que terminó la entrada (para los pulsos).</summary>
    private float tiempo;

    /// <summary>Verdadero cuando terminó la entrada y pueden animarse los destellos.</summary>
    private bool listo;

    /// <summary>Corrutina de entrada en curso.</summary>
    private Coroutine entrada;

    // =========================
    // CICLO DE VIDA
    // =========================

    /// <summary>Guarda las escalas originales y crea los destellos.</summary>
    void Awake()
    {
        if (corona != null) escalaCorona = corona.localScale;
        if (numero != null) escalaNumero = numero.localScale;

        escalaEstrellas = new Vector3[estrellas.Length];
        for (int i = 0; i < estrellas.Length; i++)
            if (estrellas[i] != null) escalaEstrellas[i] = estrellas[i].localScale;

        CrearChispas();
    }

    /// <summary>Cada vez que se activa: reinicia el estado y lanza la entrada.</summary>
    void OnEnable()
    {
        listo = false;
        tiempo = 0f;

        foreach (Chispa c in chispas) Ocultar(c);

        entrada = StartCoroutine(Entrada());
    }

    /// <summary>Al apagarse: corta la entrada y deja la corona en su tamaño original.</summary>
    void OnDisable()
    {
        if (entrada != null) { StopCoroutine(entrada); entrada = null; }
        if (corona != null) corona.localScale = escalaCorona;
    }

    /// <summary>Cada frame, una vez lista la entrada, anima los destellos y los pulsos.</summary>
    void Update()
    {
        if (!listo) return;

        float dt = Time.unscaledDeltaTime;
        tiempo += dt;

        foreach (Chispa c in chispas) AnimarChispa(c, dt);
        Pulsar();
    }

    // =========================
    // ENTRADA
    // =========================

    /// <summary>La corona crece con rebote y al terminar lanza la ráfaga inicial de destellos.</summary>
    IEnumerator Entrada()
    {
        if (duracionEntrada > 0f && corona != null)
        {
            for (float t = 0f; t < duracionEntrada; t += Time.unscaledDeltaTime)
            {
                float f = Mathf.LerpUnclamped(escalaInicial, 1f, EaseOutBack(t / duracionEntrada));
                corona.localScale = escalaCorona * f;
                yield return null;
            }
        }

        if (corona != null) corona.localScale = escalaCorona;

        // Ráfaga inicial: todos arrancan con un pequeño retraso aleatorio
        foreach (Chispa c in chispas)
        {
            Reiniciar(c);
            c.espera = Random.Range(0f, retrasoInicial);
        }

        listo = true;
        entrada = null;
    }

    // =========================
    // DESTELLOS
    // =========================

    /// <summary>
    /// Avanza un destello. Mientras espera está oculto; al nacer crece hasta la
    /// mitad de su vida y luego se encoge, a la vez que sube, gira y se desvanece.
    /// Al terminar se reinicia en otro lugar con una nueva espera.
    /// </summary>
    /// <param name="c">Destello a animar.</param>
    /// <param name="dt">Segundos desde el frame anterior.</param>
    void AnimarChispa(Chispa c, float dt)
    {
        if (c.espera > 0f)
        {
            c.espera -= dt;
            return;
        }

        c.tiempo += dt;
        float p = c.tiempo / c.duracion;

        if (p >= 1f)
        {
            Reiniciar(c);
            return;
        }

        float forma = Mathf.Sin(p * Mathf.PI);   // 0 -> 1 -> 0 durante la vida

        c.imagen.enabled = true;
        c.rotacion += c.velGiro * dt;

        c.rt.anchoredPosition = c.origen + Vector2.up * (deriva * p);
        c.rt.sizeDelta = Vector2.one * c.tam * forma;
        c.rt.localRotation = Quaternion.Euler(0f, 0f, c.rotacion);

        Color col = colorBrillo;
        col.a = forma;
        c.imagen.color = col;
    }

    /// <summary>Elige una nueva posición, tamaño y duración, y programa su próxima aparición.</summary>
    /// <param name="c">Destello a reiniciar.</param>
    void Reiniciar(Chispa c)
    {
        Ocultar(c);

        c.espera = Random.Range(espera.x, espera.y);
        c.tiempo = 0f;
        c.duracion = Random.Range(vida.x, vida.y);
        c.tam = tamano * Random.Range(0.5f, 1f);
        c.rotacion = Random.Range(0f, 90f);
        c.velGiro = Random.Range(-giro, giro);

        // Punto aleatorio sobre el arco, con un poco de variación en el radio
        float pos = Random.Range(0f, arco);
        float a = AnguloEnArco(pos) * Mathf.Deg2Rad;
        float r = radio + Random.Range(-variacionRadio, variacionRadio);
        c.origen = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * r;
        c.rt.anchoredPosition = c.origen;
    }

    /// <summary>Oculta un destello sin destruirlo.</summary>
    /// <param name="c">Destello a ocultar.</param>
    void Ocultar(Chispa c)
    {
        if (c.imagen != null) c.imagen.enabled = false;
    }

    /// <summary>
    /// Convierte una posición dentro del arco (0 = extremo izquierdo, arco = extremo
    /// derecho) en un ángulo medido desde arriba en sentido del reloj, que es el que
    /// usa <c>(sen, cos)</c> para colocar el punto.
    /// </summary>
    /// <param name="pos">Grados recorridos desde el extremo izquierdo.</param>
    /// <returns>Ángulo en grados.</returns>
    float AnguloEnArco(float pos)
    {
        float desdeCentro = pos - arco * 0.5f;   // negativo = izquierda, positivo = derecha
        return arcoInferior ? 180f - desdeCentro : desdeCentro;
    }

    // =========================
    // PULSOS
    // =========================

    /// <summary>Número que respira y estrellas que parpadean desfasadas.</summary>
    void Pulsar()
    {
        if (numero != null)
            numero.localScale = escalaNumero * (1f + pulsoNumero * Mathf.Sin(tiempo * velocidadPulso));

        for (int i = 0; i < estrellas.Length; i++)
        {
            if (estrellas[i] == null) continue;
            float s = 1f + parpadeoEstrellas * Mathf.Sin(tiempo * 3f + i * 1.3f);
            estrellas[i].localScale = escalaEstrellas[i] * s;
        }
    }

    // =========================
    // CREACIÓN
    // =========================

    /// <summary>Clona la imagen base para tener la cantidad de destellos pedida.</summary>
    void CrearChispas()
    {
        if (destello == null) { chispas = new Chispa[0]; return; }

        if (destello.sprite == null)
            destello.sprite = CrearSpriteEstrella();

        destello.raycastTarget = false;
        chispas = new Chispa[Mathf.Max(1, cantidad)];

        for (int i = 0; i < chispas.Length; i++)
        {
            Image img = i == 0 ? destello : Instantiate(destello, destello.transform.parent);
            img.name = i == 0 ? destello.name : "Chispa_" + i;

            chispas[i] = new Chispa { imagen = img, rt = (RectTransform)img.transform };
        }
    }

    /// <summary>
    /// Genera por código una estrella de 4 puntas con un núcleo brillante
    /// (blanca con alpha, se tiñe con <see cref="colorBrillo"/>).
    /// </summary>
    /// <returns>Sprite de estrella.</returns>
    Sprite CrearSpriteEstrella()
    {
        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x / (n - 1f)) * 2f - 1f;   // -1..1
                float v = (y / (n - 1f)) * 2f - 1f;

                float brazoH = Mathf.Pow(1f - Mathf.Abs(u), 3f) * Mathf.Clamp01(1f - Mathf.Abs(v) * 9f);
                float brazoV = Mathf.Pow(1f - Mathf.Abs(v), 3f) * Mathf.Clamp01(1f - Mathf.Abs(u) * 9f);
                float nucleo = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v) * 2.2f), 2f);

                float a = Mathf.Clamp01(Mathf.Max(brazoH, brazoV) + nucleo);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    // =========================
    // AUXILIARES
    // =========================

    /// <summary>Curva "ease out back": llega al valor final con un pequeño rebote.</summary>
    /// <param name="x">Progreso de 0 a 1.</param>
    /// <returns>Valor suavizado, que puede pasarse un poco de 1.</returns>
    float EaseOutBack(float x)
    {
        x = Mathf.Clamp01(x);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
    }
}