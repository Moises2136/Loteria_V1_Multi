// PanelJuegaLoteriaMultiUI.cs
// Solo dibuja lo que el Manager le dice. Cero PhotonView aquí.
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla la pantalla del juego en modo Multijugador. A diferencia
/// de la versión Solo, aquí NO se decide nada por cuenta propia: solo escucha
/// los avisos que le manda <see cref="LoteriaMultiplayerManager"/> (qué carta se
/// cantó, quién ganó, si se acabaron las cartas) y dibuja el tablero acorde a eso.
/// Cuando el jugador toca una casilla, este script solo reporta el acierto al
/// manager; no decide si la partida terminó ni sincroniza nada con la red.
/// </summary>
public class PanelJuegaLoteriaMultiUI : MonoBehaviour
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
    // PANELES DE FLUJO
    // =========================

    /// <summary>Panel del juego actualmente visible.</summary>
    [Header("Paneles de flujo")]
    public GameObject panelJuegoActual;

    /// <summary>Panel de resultados al que se pasa cuando termina la partida.</summary>
    public GameObject panelResultados;

    /// <summary>Panel/overlay que aparece cuando se completa el tablero.</summary>
    public GameObject panelLoteriaCompletada;

    /// <summary>Cuántos segundos se espera antes de mostrar la pantalla de resultados tras terminar.</summary>
    public float tiempoAntesDeResultados = 2f;

    // =========================
    // AUDIO / TTS
    // =========================

    /// <summary>Referencia al sistema de texto a voz, usado para "cantar" cada carta en voz alta.</summary>
    [Header("Audio / TTS")]
    public ElevenLabsTTS ttsManager;

    // =========================
    // TEXTOS
    // =========================

    /// <summary>Texto con el título/nombre de la lotería que se está jugando.</summary>
    [Header("Textos")]
    public TMP_Text textoTituloLoteria;

    /// <summary>Texto que muestra cuántos jugadores hay conectados en la sala.</summary>
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
    [Header("Historial")]
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

    /// <summary>Las casillas del tablero del jugador.</summary>
    [Header("Tablero")]
    public CasillaTablero[] casillas;

    /// <summary>Cuántas cartas tiene el tablero del jugador (por defecto 9).</summary>
    public int totalCartasTablero = 9;


    // =========================
    // BOTÓN LOTERÍA
    // =========================

    /// <summary>Botón para declarar "¡Lotería!" cuando se completó el tablero.</summary>
    [Header("Botón Lotería")]
    public Button botonLoteria;

    /// <summary>Botón para salir de la partida y volver al menú.</summary>
    public Button botonSalir;

/// <summary>Panel del menú principal, al que se vuelve al salir.</summary>
    public GameObject panelMenu;

    // =========================
    // FEEDBACK INCORRECTO y CORRECTO
    // =========================

    /// <summary>Cuántos segundos se muestra el aviso de "incorrecta" antes de quitarlo.</summary>
    [Header("Feedback incorrecto y correcto")]
    public float tiempoFeedbackIncorrecto = 0.7f;

    /// <summary>Cuántas casillas ha marcado correctamente el jugador local hasta ahora.</summary>
    private int aciertos = 0;

    /// <summary>Indica si la partida ya terminó (por victoria propia, de otro jugador, o porque se acabó el mazo).</summary>
    private bool juegoTerminado = false;

    /// <summary>Da retroalimentación sonora cuando el jugador marca una casilla correctamente.</summary>
    public AudioSource audioCorrecto;
    public AudioClip sonidoCorrecto;

    /// <summary>Da retroalimentación sonora cuando el jugador marca una casilla incorrectamente.</summary>
    public AudioSource audioIncorrecto;
    public AudioClip sonidoIncorrecto;



    // ── Lifecycle ─────────────────────────────────────────────────

    /// <summary>
    /// Cada vez que se activa este panel, se suscribe a los avisos del
    /// <see cref="LoteriaMultiplayerManager"/> (carta cantada, lotería ganada,
    /// cartas agotadas), y prepara la interfaz y el tablero.
    /// </summary>
    void OnEnable()
    {
        // Suscribirse a eventos del Manager
        if (LoteriaMultiplayerManager.Instance != null)
        {
            LoteriaMultiplayerManager.Instance.OnCartaCantada += MostrarCartaActual;
            LoteriaMultiplayerManager.Instance.OnLoteriaGanada += ManejarLoteria;
            LoteriaMultiplayerManager.Instance.OnCartasAgotadas += ManejarFinCartas;
        }

        InicializarUI();
        ConfigurarBotones();
        GenerarTablero();
    }

    /// <summary>
    /// Al desactivarse este panel, detiene cualquier coroutine en curso y se
    /// desconecta de los avisos del manager, para evitar errores o fugas de memoria.
    /// </summary>
    void OnDisable()
    {
        StopAllCoroutines();

        // Desuscribirse para evitar memory leaks
        if (LoteriaMultiplayerManager.Instance != null)
        {
            LoteriaMultiplayerManager.Instance.OnCartaCantada -= MostrarCartaActual;
            LoteriaMultiplayerManager.Instance.OnLoteriaGanada -= ManejarLoteria;
            LoteriaMultiplayerManager.Instance.OnCartasAgotadas -= ManejarFinCartas;
        }
    }

    // ── Tablero ───────────────────────────────────────────────────

    /// <summary>
    /// Elige al azar las cartas que le tocarán al tablero de este jugador
    /// (una por cada casilla), usando las cartas ya disponibles en el caché local.
    /// </summary>
    /// <remarks>
    /// Cada jugador genera su propio tablero de forma local e independiente;
    /// el manager solo sincroniza qué carta se está cantando, no el tablero de nadie.
    /// </remarks>
    void GenerarTablero()
    {
        var cache = LoteriaDataCache.Instance;
        if (cache == null || !cache.Listo) return;

        if (cache.TotalCartas < totalCartasTablero)
        {
            Debug.LogWarning("Cartas insuficientes: " + cache.TotalCartas);
            return;
        }

        var ids = new System.Collections.Generic.List<int>(cache.Cartas.Keys);
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

    // ── Recibe carta del Manager ──────────────────────────────────

    /// <summary>
    /// Se llama automáticamente cuando el manager avisa que se cantó una nueva carta.
    /// Actualiza la imagen y el nombre en pantalla, agrega la carta al historial,
    /// y la lee en voz alta.
    /// </summary>
    /// <param name="idCarta">ID de la carta que se acaba de cantar.</param>
    void MostrarCartaActual(int idCarta)
    {
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
    /// Se llama automáticamente cuando el manager avisa que alguien ganó la partida.
    /// Muestra el mensaje correspondiente (si ganó el jugador local o alguien más)
    /// y arranca el flujo de cierre de partida.
    /// </summary>
    /// <param name="nombreGanador">Nombre del jugador que ganó.</param>
    /// <param name="soyYo">Verdadero si el ganador es el jugador local.</param>
    void ManejarLoteria(string nombreGanador, bool soyYo)
    {
        juegoTerminado = true;
        SetEstado(soyYo ? "¡Ganaste! ¡LOTERÍA!" : $"¡{nombreGanador} ganó!");

        StartCoroutine(FlujoFinPartida(nombreGanador));
    }

    /// <summary>
    /// Se llama automáticamente cuando el manager avisa que ya no quedan más
    /// cartas por cantar, sin que nadie haya ganado.
    /// </summary>
    void ManejarFinCartas()
    {
        juegoTerminado = true;
        SetEstado("Se acabaron las cartas.");
        if (textoNombreCartaActual != null)
            textoNombreCartaActual.text = "Fin de la partida";
    }

    // ── Interacción ───────────────────────────────────────────────

    /// <summary>
    /// Conecta cada casilla del tablero con su función de "tocar", y también
    /// conecta el botón de declarar lotería.
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
    /// Se ejecuta cuando el jugador toca una casilla del tablero. Compara la
    /// carta de esa casilla contra la carta actual reportada por el manager
    /// (no una variable local, ya que la carta "actual" la controla el manager).
    /// </summary>
    /// <param name="index">Índice de la casilla que se tocó.</param>
    void TocarCasilla(int index)
    {
        if (juegoTerminado) return;
        var casilla = casillas[index];
        if (casilla.bloqueada || casilla.marcada) return;

        int cartaActual = LoteriaMultiplayerManager.Instance?.CartaActualId ?? -1;

        if (casilla.idCarta == cartaActual)
            MarcarCorrecta(index);
        else
            StartCoroutine(MostrarIncorrecta(index));
    }

    /// <summary>
    /// Marca una casilla como acertada: activa el overlay correspondiente,
    /// suma un acierto, le avisa al manager del acierto (para que sincronice
    /// el progreso si hace falta), y revisa si con esto se completó el tablero.
    /// </summary>
    /// <param name="index">Índice de la casilla acertada.</param>
    void MarcarCorrecta(int index)
    {
        var casilla = casillas[index];
        casilla.marcada = casilla.bloqueada = true;

        if (casilla.overlayIncorrecta != null) casilla.overlayIncorrecta.SetActive(false);
        if (casilla.overlayCorrecta != null) casilla.overlayCorrecta.SetActive(true);

        if (audioCorrecto != null && sonidoCorrecto != null)
            audioCorrecto.PlayOneShot(sonidoCorrecto);

        aciertos++;
        ActualizarContador();
        SetEstado("¡Correcta!");

        LoteriaMultiplayerManager.Instance?.ReportarAcierto();

        if (aciertos >= totalCartasTablero)
        {
            if (botonLoteria != null) botonLoteria.interactable = true;
            if (panelLoteriaCompletada != null) panelLoteriaCompletada.SetActive(true);
            SetEstado("¡Tablero completo! Presiona ¡Lotería!");
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

        if (audioIncorrecto != null && sonidoIncorrecto != null)
            audioIncorrecto.PlayOneShot(sonidoIncorrecto);

        yield return new WaitForSeconds(tiempoFeedbackIncorrecto);

        if (!casilla.marcada && casilla.overlayIncorrecta != null)
            casilla.overlayIncorrecta.SetActive(false);

        casilla.bloqueada = false;
        if (!juegoTerminado) SetEstado("");
    }

    /// <summary>
    /// Se ejecuta cuando el jugador presiona el botón de declarar "¡Lotería!".
    /// Si ya completó el tablero, le pide al manager que valide y anuncie
    /// la victoria a toda la sala (no se declara ganador localmente).
    /// </summary>
    void ValidarLoteria()
    {
        if (aciertos >= totalCartasTablero)
            LoteriaMultiplayerManager.Instance?.PedirLoteria();
        else
            SetEstado("Aún faltan cartas.");
    }

    /// <summary>
    /// Anuncia el fin de la partida en voz alta, espera un momento, y cambia
    /// del panel de juego al panel de resultados, indicándole el nombre del ganador.
    /// </summary>
    /// <param name="nombreGanador">Nombre del jugador que ganó la partida.</param>
    IEnumerator FlujoFinPartida(string nombreGanador)
    {
        if (ttsManager != null) ttsManager.SpeakText("¡Lotería!");
        yield return new WaitForSeconds(tiempoAntesDeResultados);

        if (panelJuegoActual != null) panelJuegoActual.SetActive(false);
        if (panelResultados != null)
        {
            panelResultados.SetActive(true);
            var resultadosUI = panelResultados.GetComponent<PanelResultadosPartidaUI>();
            if (resultadosUI != null)
                resultadosUI.MostrarGanador(nombreGanador);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Deja la interfaz en su estado inicial antes de empezar: limpia textos,
    /// oculta overlays, borra el historial, resetea el tablero, y muestra
    /// cuántos jugadores hay conectados en la sala.
    /// </summary>
    void InicializarUI()
    {
        aciertos = 0;
        juegoTerminado = false;
        if (panelLoteriaCompletada != null) panelLoteriaCompletada.SetActive(false);
        if (textoNombreCartaActual != null) textoNombreCartaActual.text = "Preparando...";
        if (textoEstadoGeneral != null) textoEstadoGeneral.text = "";
        if (imagenCartaActual != null) { imagenCartaActual.sprite = null; imagenCartaActual.color = Color.clear; }
        if (botonLoteria != null) botonLoteria.interactable = false;

        if (textoJugadores != null && Photon.Pun.PhotonNetwork.InRoom)
            textoJugadores.text = Photon.Pun.PhotonNetwork.CurrentRoom.PlayerCount + " jugadores";
        
        if (textoTituloLoteria != null)
        {
            string nombre = PlayerPrefs.GetString("loteria_nombre", "");
            
            var room = Photon.Pun.PhotonNetwork.CurrentRoom;
            if (room != null && room.CustomProperties.TryGetValue("loteriaNombre", out object n))
            nombre = n as string;
            
            if (!string.IsNullOrWhiteSpace(nombre))
            textoTituloLoteria.text = nombre;
        }

        foreach (var c in casillas)
        {
            if (c.overlayCorrecta != null) c.overlayCorrecta.SetActive(false);
            if (c.overlayIncorrecta != null) c.overlayIncorrecta.SetActive(false);
            if (c.imagenCarta != null) { c.imagenCarta.sprite = null; c.imagenCarta.color = Color.clear; }
            c.marcada = false; c.bloqueada = false; c.idCarta = -1;
        }

        if (historialSlots != null)
            foreach (var s in historialSlots)
                if (s != null) s.ClearData();

        ActualizarContador();
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
        string txt = string.IsNullOrWhiteSpace(descripcion) ? "" : descripcion.Trim();
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            if (!string.IsNullOrWhiteSpace(txt)) txt += ". ";
            txt += nombre.Trim();
        }
        ttsManager.SpeakText(txt);
    }

    void SalirAlMenu()
    {
    StopAllCoroutines();
    juegoTerminado = true;

    if (Photon.Pun.PhotonNetwork.InRoom)
        Photon.Pun.PhotonNetwork.LeaveRoom();

    SalaSessionManager.Instance?.Reiniciar();

    if (panelJuegoActual != null) panelJuegoActual.SetActive(false);
    if (panelMenu != null) panelMenu.SetActive(true);
    }

    /// <summary>
    /// Actualiza el texto que muestra el conteo de aciertos (ej. "3/9").
    /// </summary>
    void ActualizarContador() =>
        textoContador?.SetText(aciertos + "/" + totalCartasTablero);

    /// <summary>
    /// Muestra un mensaje en el texto de estado general.
    /// </summary>
    /// <param name="msg">El mensaje a mostrar.</param>
    void SetEstado(string msg) =>
        textoEstadoGeneral?.SetText(msg);

    /// <summary>
    /// Revuelve una lista de números al azar (algoritmo de Fisher-Yates),
    /// usada para elegir qué cartas le tocan al tablero de este jugador.
    /// </summary>
    /// <param name="list">La lista que se va a revolver, modificada directamente.</param>
    void Shuffle(System.Collections.Generic.List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
}
