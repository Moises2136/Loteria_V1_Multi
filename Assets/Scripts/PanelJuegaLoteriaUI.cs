using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla toda la partida de lotería en modo Solo: genera el
/// tablero con cartas al azar, va "cantando" cartas una por una (mostrándolas
/// y leyéndolas en voz alta), revisa cuando el jugador marca una casilla
/// correcta o incorrecta, y detecta cuándo se completa el tablero para
/// mostrar la pantalla de resultados.
/// </summary>
public class PanelJuegaLoteriaSoloUI : MonoBehaviour
{
    /// <summary>
    /// Representa una casilla del tablero de 9 cartas: su botón, su imagen,
    /// y los overlays visuales que indican si fue marcada correcta o incorrectamente.
    /// </summary>
    [System.Serializable]
    public class CasillaTablero
    {
        /// <summary>Botón que el jugador toca para marcar esta casilla.</summary>
        public Button boton;

        /// <summary>Imagen que muestra la carta asignada a esta casilla.</summary>
        public Image imagenCarta;

        /// <summary>Overlay visual que se muestra cuando la casilla se marcó correctamente.</summary>
        public GameObject overlayCorrecta;

        /// <summary>Overlay visual que se muestra cuando la casilla se marcó por error.</summary>
        public GameObject overlayIncorrecta;

        /// <summary>ID de la carta que le tocó a esta casilla.</summary>
        [HideInInspector] public int idCarta;

        /// <summary>Indica si esta casilla ya fue marcada correctamente.</summary>
        [HideInInspector] public bool marcada;

        /// <summary>Indica si esta casilla está temporalmente bloqueada (por ejemplo, mostrando el error).</summary>
        [HideInInspector] public bool bloqueada;
    }

    // =========================
    // FLUJO FINAL
    // =========================

    /// <summary>Panel del juego actualmente visible.</summary>
    [Header("Flujo final")]
    public GameObject panelJuegoActual;

    /// <summary>Panel de resultados al que se pasa al ganar.</summary>
    public GameObject panelResultados;

    /// <summary>Panel/overlay que aparece cuando se completa el tablero.</summary>
    public GameObject panelLoteriaCompletada;

    /// <summary>Cuántos segundos se espera antes de mostrar la pantalla de resultados tras ganar.</summary>
    public float tiempoAntesDeResultados = 2f;

    // =========================
    // CARGA REMOTA
    // =========================

    /// <summary>Referencia al cargador de imágenes remotas (usado por otras partes del sistema de cartas).</summary>
    [Header("Carga remota")]
    public RemoteImageLoader remoteImageLoader;

    // =========================
    // AUDIO / TTS
    // =========================

    /// <summary>Referencia al sistema de texto a voz, usado para "cantar" cada carta en voz alta.</summary>
    [Header("Audio / TTS")]
    public ElevenLabsTTS ttsManager;

    // =========================
    // TEXTOS SUPERIORES
    // =========================

    /// <summary>Texto con el título/nombre de la lotería que se está jugando.</summary>
    [Header("Textos superiores")]
    public TMP_Text textoTituloLoteria;

    /// <summary>Texto que indica cuántos jugadores hay (en modo Solo, siempre dice "1 jugador").</summary>
    public TMP_Text textoJugadores;

    /// <summary>Texto que muestra el nombre de la carta que se está cantando en este momento.</summary>
    public TMP_Text textoNombreCartaActual;

    /// <summary>Texto que muestra el conteo de aciertos (ej. "3/9").</summary>
    public TMP_Text textoContador;

    /// <summary>Texto usado para mostrar mensajes de estado generales (errores, avisos, etc).</summary>
    public TMP_Text textoEstadoGeneral;

    // =========================
    // HISTORIAL
    // =========================

    /// <summary>
    /// Los 3 espacios fijos que muestran las últimas cartas cantadas,
    /// de la más antigua a la más reciente.
    /// </summary>
    [Header("Historial fijo (3 slots)")]
    public CardsHistorial[] historialSlots;

    // =========================
    // CARTA ACTUAL
    // =========================

    /// <summary>Imagen grande que muestra la carta que se está cantando en este momento.</summary>
    [Header("Carta actual")]
    public Image imagenCartaActual;

    // =========================
    // TABLERO
    // =========================

    /// <summary>Las 9 casillas del tablero del jugador.</summary>
    [Header("Tablero")]
    public CasillaTablero[] casillas;

    // =========================
    // BOTÓN FINAL
    // =========================

    /// <summary>Botón para declarar "¡Lotería!" cuando se completó el tablero.</summary>
    [Header("Botón final")]
    public Button botonLoteria;

    /// <summary>Botón para salir de la partida y volver al menú.</summary>
    public Button botonSalir;

    /// <summary>Panel del menú principal, al que se vuelve al salir.</summary>
    public GameObject panelMenu;

    // =========================
    // FX Sound
    // =========================

    /// <summary>Da retroalimentación sonora cuando el jugador marca una casilla correctamente.</summary>
    public AudioSource audioCorrecto;
    public AudioClip sonidoCorrecto;

    /// <summary>Da retroalimentación sonora cuando el jugador marca una casilla incorrectamente.</summary>
    public AudioSource audioIncorrecto;
    public AudioClip sonidoIncorrecto;


    // =========================
    // CONFIGURACIÓN
    // =========================

    /// <summary>Cuántas cartas tiene el tablero del jugador (por defecto 9).</summary>
    [Header("Configuración")]
    public int totalCartasTablero = 9;

    /// <summary>Cuántos segundos se muestra cada carta antes de pasar a la siguiente.</summary>
    public float tiempoEntreCartas = 15f;

    /// <summary>Cuántos segundos se muestra el aviso de "incorrecta" antes de quitarlo.</summary>
    public float tiempoFeedbackIncorrecto = 0.7f;

    /// <summary>Llave usada para leer el ID de la lotería guardado en PlayerPrefs.</summary>
    private const string KEY_LOTERIA_DOC_ID = "loteria_doc_id";

    /// <summary>ID de la carta que se está mostrando/cantando en este momento (-1 si no hay ninguna).</summary>
    private int cartaActualId = -1;

    /// <summary>Cuántas casillas ha marcado correctamente el jugador hasta ahora.</summary>
    private int aciertos = 0;

    /// <summary>Indica si la partida ya terminó (por victoria, por salir, o porque se acabó el mazo).</summary>
    private bool juegoTerminado = false;

    /// <summary>
    /// Lista de IDs de cartas que aún faltan por "cantarse" en esta partida,
    /// en el orden en que se van a mostrar.
    /// </summary>
    private List<int> mazoCantado = new List<int>();

    // ── Ciclo de vida ────────────────────────────────────────────

    /// <summary>Cada vez que se activa este panel, se reinicia la partida desde cero.</summary>
    void OnEnable() => ReiniciarPartida();

    /// <summary>Al desactivarse este panel, se detienen todas las coroutines en curso (cuenta de cartas, etc).</summary>
    void OnDisable() => StopAllCoroutines();

    // ── Reinicio ─────────────────────────────────────────────────

    /// <summary>
    /// Reinicia todo el estado de la partida (aciertos, mazo, tablero),
    /// prepara la interfaz, y empieza a cargar las cartas de la lotería
    /// seleccionada para poder arrancar el juego.
    /// </summary>
    void ReiniciarPartida()
    {
        StopAllCoroutines();
        cartaActualId = -1;
        aciertos = 0;
        juegoTerminado = false;
        mazoCantado.Clear();

        InicializarUI();
        ConfigurarBotones();

        string loteriaDocId = PlayerPrefs.GetString(KEY_LOTERIA_DOC_ID, "");

        if (string.IsNullOrWhiteSpace(loteriaDocId))
        {
            SetEstado("No hay lotería seleccionada.");
            return;
        }

        // Si el cache ya está listo (por ejemplo, volviste a jugar)
        // úsalo directamente. Si no, cárgalo ahora.
        if (LoteriaDataCache.Instance != null && LoteriaDataCache.Instance.Listo)
        {
            IniciarJuegoDesdeCache();
        }
        else
        {
            SetEstado("Cargando cartas...");
            StartCoroutine(CargarCacheYJugar(loteriaDocId));
        }
    }

    // ── Carga cache en modo solo ──────────────────────────────────

    /// <summary>
    /// Pide que se carguen las cartas de la lotería (si aún no estaban en caché)
    /// y espera a que termine antes de arrancar el juego.
    /// </summary>
    /// <param name="loteriaDocId">ID de la lotería cuyas cartas se van a cargar.</param>
    IEnumerator CargarCacheYJugar(string loteriaDocId)
    {
        // Si no existe el singleton lo buscamos en escena
        if (LoteriaDataCache.Instance == null)
        {
            SetEstado("LoteriaDataCache no encontrado en escena.");
            yield break;
        }

        bool terminado = false;
        string errorCache = null;

        LoteriaDataCache.Instance.OnCacheListoOK += () => terminado = true;
        LoteriaDataCache.Instance.OnCacheError += e => { errorCache = e; terminado = true; };

        LoteriaDataCache.Instance.CargarSiNecesario(loteriaDocId);

        yield return new WaitUntil(() => terminado);

        if (errorCache != null)
        {
            SetEstado("Error: " + errorCache);
            yield break;
        }

        IniciarJuegoDesdeCache();
    }

    /// <summary>
    /// Una vez que las cartas ya están en caché, arma el título, genera el
    /// tablero, prepara el mazo de cartas a cantar, y arranca el flujo del juego.
    /// </summary>
    void IniciarJuegoDesdeCache()
    {
        var cache = LoteriaDataCache.Instance;

        string nombre = PlayerPrefs.GetString("loteria_nombre", "");
        if (!string.IsNullOrWhiteSpace(nombre) && textoTituloLoteria != null)
            textoTituloLoteria.text = nombre;

        if (textoJugadores != null)
            textoJugadores.text = "1 jugador";

        GenerarTablero(cache);
        PrepararMazo(cache);
        StartCoroutine(FlujoJuego());
        SetEstado("");
    }

    // ── Tablero ───────────────────────────────────────────────────

    /// <summary>
    /// Elige al azar las cartas que le tocarán al tablero del jugador
    /// (una por cada casilla) y las asigna a cada una.
    /// </summary>
    /// <param name="cache">El caché con todas las cartas disponibles de la lotería.</param>
    void GenerarTablero(LoteriaDataCache cache)
    {
        if (cache.TotalCartas < totalCartasTablero)
        {
            Debug.LogWarning("Cartas insuficientes: " + cache.TotalCartas);
            return;
        }

        List<int> ids = new List<int>(cache.Cartas.Keys);
        Shuffle(ids);

        for (int i = 0; i < totalCartasTablero; i++)
        {
            var carta = cache.ObtenerCarta(ids[i]);

            casillas[i].idCarta = carta.id;
            casillas[i].marcada = false;
            casillas[i].bloqueada = false;

            if (casillas[i].overlayCorrecta != null) casillas[i].overlayCorrecta.SetActive(false);
            if (casillas[i].overlayIncorrecta != null) casillas[i].overlayIncorrecta.SetActive(false);

            if (casillas[i].imagenCarta != null)
            {
                casillas[i].imagenCarta.sprite = carta.sprite;
                casillas[i].imagenCarta.color = carta.sprite != null ? Color.white : Color.clear;
                casillas[i].imagenCarta.preserveAspect = true;
            }
        }
    }

    /// <summary>
    /// Arma la lista de todas las cartas que se van a "cantar" durante la
    /// partida, en un orden aleatorio.
    /// </summary>
    /// <param name="cache">El caché con todas las cartas disponibles de la lotería.</param>
    void PrepararMazo(LoteriaDataCache cache)
    {
        mazoCantado.Clear();
        foreach (var id in cache.Cartas.Keys)
            mazoCantado.Add(id);
        Shuffle(mazoCantado);
    }

    // ── Flujo de juego ────────────────────────────────────────────

    /// <summary>
    /// Ciclo principal del juego: va sacando una carta a la vez del mazo,
    /// mostrándola, y esperando un tiempo antes de pasar a la siguiente.
    /// Se detiene cuando el jugador gana, sale de la partida, o se acaban las cartas.
    /// </summary>
    IEnumerator FlujoJuego()
    {
        yield return new WaitForSeconds(1f);

        while (!juegoTerminado && mazoCantado.Count > 0)
        {
            int id = mazoCantado[0];
            mazoCantado.RemoveAt(0);

            MostrarCartaActual(id);

            float t = tiempoEntreCartas;
            while (t > 0f && !juegoTerminado)
            {
                t -= Time.deltaTime;
                yield return null;
            }
        }

        if (!juegoTerminado)
        {
            juegoTerminado = true;
            SetEstado("Se acabaron las cartas.");
            if (textoNombreCartaActual != null)
                textoNombreCartaActual.text = "Fin de la partida";
        }
    }

    /// <summary>
    /// Muestra en pantalla la carta que se acaba de cantar (imagen y nombre),
    /// la agrega al historial reciente, y la lee en voz alta.
    /// </summary>
    /// <param name="idCarta">ID de la carta que se va a mostrar.</param>
    void MostrarCartaActual(int idCarta)
    {
        cartaActualId = idCarta;

        var carta = LoteriaDataCache.Instance?.ObtenerCarta(idCarta);
        if (carta == null) return;

        if (imagenCartaActual != null)
        {
            imagenCartaActual.sprite = carta.sprite;
            imagenCartaActual.color = carta.sprite != null ? Color.white : Color.clear;
            imagenCartaActual.preserveAspect = true;
        }

        if (textoNombreCartaActual != null)
            textoNombreCartaActual.text = carta.nombre;

        AgregarHistorial(idCarta);
        LeerCarta(carta.descripcion, carta.nombre);
    }

    /// <summary>
    /// Recorre los 3 espacios del historial "corriendo" cada carta un lugar
    /// hacia atrás, para dejar espacio a la carta recién cantada en el último slot.
    /// </summary>
    /// <param name="idCarta">ID de la carta recién cantada que se agrega al historial.</param>
    void AgregarHistorial(int idCarta)
    {
        if (historialSlots == null || historialSlots.Length < 3) return;

        var carta = LoteriaDataCache.Instance?.ObtenerCarta(idCarta);
        if (carta == null) return;

        if (historialSlots[0] != null && historialSlots[1] != null)
            historialSlots[0].CopyFrom(historialSlots[1]);

        if (historialSlots[1] != null && historialSlots[2] != null)
            historialSlots[1].CopyFrom(historialSlots[2]);

        if (historialSlots[2] != null)
            historialSlots[2].SetData(carta.sprite, carta.nombre);
    }

    /// <summary>
    /// Arma el texto a leer en voz alta (descripción seguida del nombre de la carta)
    /// y se lo manda al sistema de texto a voz.
    /// </summary>
    /// <param name="descripcion">Descripción de la carta.</param>
    /// <param name="nombre">Nombre de la carta.</param>
    void LeerCarta(string descripcion, string nombre)
    {
        if (ttsManager == null) return;
        string txt = "";
        if (!string.IsNullOrWhiteSpace(descripcion)) txt = descripcion.Trim();
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            if (!string.IsNullOrWhiteSpace(txt)) txt += ". ";
            txt += nombre.Trim();
        }
        ttsManager.SpeakText(txt);
    }

    // ── Interacción ───────────────────────────────────────────────

    /// <summary>
    /// Conecta cada casilla del tablero con su función de "tocar", y también
    /// conecta los botones de declarar lotería y de salir.
    /// </summary>
    void ConfigurarBotones()
    {
        for (int i = 0; i < casillas.Length; i++)
        {
            int idx = i;
            if (casillas[i].boton != null)
            {
                casillas[i].boton.onClick.RemoveAllListeners();
                casillas[i].boton.onClick.AddListener(() => TocarCasilla(idx));
            }
        }

        if (botonLoteria != null)
        {
            botonLoteria.onClick.RemoveAllListeners();
            botonLoteria.onClick.AddListener(ValidarLoteria);
        }

        if (botonSalir != null)
        {
            botonSalir.onClick.RemoveAllListeners();
            botonSalir.onClick.AddListener(SalirAlMenu);
        }
    }

    /// <summary>
    /// Se ejecuta cuando el jugador toca una casilla del tablero.
    /// Revisa si la carta de esa casilla es la que se está cantando actualmente,
    /// y marca la casilla como correcta o muestra el aviso de error.
    /// </summary>
    /// <param name="index">Índice de la casilla que se tocó.</param>
    void TocarCasilla(int index)
    {
        if (juegoTerminado) return;

        var casilla = casillas[index];
        if (casilla.bloqueada || casilla.marcada) return;

        if (casilla.idCarta == cartaActualId)
            MarcarCorrecta(index);
        else
            StartCoroutine(MostrarIncorrecta(index));
    }

    /// <summary>
    /// Marca una casilla como acertada: activa el overlay correspondiente,
    /// suma un acierto, y revisa si con esto se completó todo el tablero
    /// (en cuyo caso muestra el aviso de "Lotería completada").
    /// </summary>
    /// <param name="index">Índice de la casilla acertada.</param>
    void MarcarCorrecta(int index)
    {
        var casilla = casillas[index];
        casilla.marcada = true;
        casilla.bloqueada = true;

        if (casilla.overlayIncorrecta != null) casilla.overlayIncorrecta.SetActive(false);
        if (casilla.overlayCorrecta != null) casilla.overlayCorrecta.SetActive(true);

        aciertos++;
        ActualizarContador();
        SetEstado("¡Correcta!");

        if (aciertos >= totalCartasTablero)
        {
            juegoTerminado = true;
            cartaActualId = -1;

            if (textoNombreCartaActual != null)
                textoNombreCartaActual.text = "Tablero completo";

            SetEstado("¡Lotería completada!");

            if (botonLoteria != null)
                botonLoteria.interactable = true;

            if (panelLoteriaCompletada != null)
                panelLoteriaCompletada.SetActive(true);

            if (ttsManager != null)
                ttsManager.SpeakText("Lotería completada");
        }
    }

    /// <summary>
    /// Muestra brevemente el aviso de "casilla incorrecta" y lo quita después
    /// de <see cref="tiempoFeedbackIncorrecto"/> segundos, siempre y cuando
    /// la casilla no haya sido marcada correctamente mientras tanto.
    /// </summary>
    /// <param name="index">Índice de la casilla marcada por error.</param>
    IEnumerator MostrarIncorrecta(int index)
    {
        var casilla = casillas[index];
        if (casilla.bloqueada) yield break;

        casilla.bloqueada = true;
        if (casilla.overlayIncorrecta != null) casilla.overlayIncorrecta.SetActive(true);
        SetEstado("Carta incorrecta");

        yield return new WaitForSeconds(tiempoFeedbackIncorrecto);

        if (!casilla.marcada && casilla.overlayIncorrecta != null)
            casilla.overlayIncorrecta.SetActive(false);

        casilla.bloqueada = false;
        if (!juegoTerminado) SetEstado("");
    }

    /// <summary>
    /// Se ejecuta cuando el jugador presiona el botón de declarar "¡Lotería!".
    /// Solo se considera válido si ya se completaron todas las casillas del tablero.
    /// </summary>
    void ValidarLoteria()
    {
        if (aciertos >= totalCartasTablero)
        {
            SetEstado("¡Lotería correcta!");
            StartCoroutine(FlujoVictoria());
        }
        else
        {
            SetEstado("Aún faltan cartas.");
        }
    }

    /// <summary>
    /// Anuncia la victoria en voz alta, espera un momento, y cambia del panel
    /// de juego al panel de resultados.
    /// </summary>
    IEnumerator FlujoVictoria()
    {
        juegoTerminado = true;
        if (ttsManager != null) ttsManager.SpeakText("Lotería");

        yield return new WaitForSeconds(tiempoAntesDeResultados);

        if (panelJuegoActual != null) panelJuegoActual.SetActive(false);
        if (panelResultados != null) panelResultados.SetActive(true);
    }

    // ── Helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Deja la interfaz en su estado inicial antes de empezar una partida:
    /// limpia textos, oculta overlays, borra el historial, y resetea el
    /// tablero y el contador de aciertos.
    /// </summary>
    void InicializarUI()
    {
        if (panelLoteriaCompletada != null) panelLoteriaCompletada.SetActive(false);
        if (textoNombreCartaActual != null) textoNombreCartaActual.text = "Preparando...";
        if (textoEstadoGeneral != null) textoEstadoGeneral.text = "";
        if (imagenCartaActual != null) { imagenCartaActual.sprite = null; imagenCartaActual.color = Color.clear; }
        if (botonLoteria != null) botonLoteria.interactable = false;

        if (casillas != null)
        {
            foreach (var c in casillas)
            {
                if (c.overlayCorrecta != null) c.overlayCorrecta.SetActive(false);
                if (c.overlayIncorrecta != null) c.overlayIncorrecta.SetActive(false);
                if (c.imagenCarta != null) { c.imagenCarta.sprite = null; c.imagenCarta.color = Color.clear; }
                c.marcada = false; c.bloqueada = false; c.idCarta = -1;
            }
        }

        if (historialSlots != null)
            foreach (var s in historialSlots)
                if (s != null) s.ClearData();

        aciertos = 0;
        juegoTerminado = false;
        ActualizarContador();
    }

    /// <summary>
    /// Actualiza el texto que muestra el conteo de aciertos (ej. "3/9").
    /// </summary>
    void ActualizarContador()
    {
        if (textoContador != null)
            textoContador.text = aciertos + "/" + totalCartasTablero;
    }

    /// <summary>
    /// Muestra un mensaje en el texto de estado general.
    /// </summary>
    /// <param name="msg">El mensaje a mostrar.</param>
    void SetEstado(string msg)
    {
        if (textoEstadoGeneral != null)
            textoEstadoGeneral.text = msg;
    }

    /// <summary>
    /// Revuelve una lista de números al azar (algoritmo de Fisher-Yates).
    /// Se usa tanto para elegir las cartas del tablero como para el orden del mazo.
    /// </summary>
    /// <param name="list">La lista que se va a revolver, modificada directamente.</param>
    void Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            int t = list[i]; list[i] = list[r]; list[r] = t;
        }
    }

    /// <summary>
    /// Detiene la partida, limpia su estado, y regresa al menú principal.
    /// </summary>
    void SalirAlMenu()
    {
        StopAllCoroutines();
        juegoTerminado = true;

        cartaActualId = -1;
        aciertos = 0;
        mazoCantado.Clear();

        if (panelJuegoActual != null) panelJuegoActual.SetActive(false);
        if (panelMenu != null) panelMenu.SetActive(true);
    }
}
