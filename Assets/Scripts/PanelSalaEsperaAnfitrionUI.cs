using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

/// <summary>
/// Este script controla la pantalla de espera que ve el anfitrión mientras
/// otros jugadores se unen a la sala. Muestra el código de la sala, cuántos
/// jugadores hay, el progreso de la carga de las cartas de la lotería, y
/// permite copiar el código, salir de la sala, o iniciar la partida cuando
/// todo esté listo.
/// </summary>
public class PanelSalaEsperaAnfitrionUI : MonoBehaviour
{
    // =========================
    // UI
    // =========================

    /// <summary>Texto que muestra el código (PIN) de la sala.</summary>
    [Header("UI")]
    public TMP_Text textoPIN;

    /// <summary>Texto que muestra cuántos jugadores hay en la sala.</summary>
    public TMP_Text textoJugadores;

    /// <summary>Texto que muestra el estado de carga de las cartas (arrastrar un TMP_Text aquí).</summary>
    public TMP_Text textoEstadoCarga;

    /// <summary>Botón para copiar el código de la sala al portapapeles.</summary>
    public Button botonCopiarPIN;

    /// <summary>Botón para iniciar la partida (solo disponible cuando las cartas ya cargaron).</summary>
    public Button botonIniciarJuego;

    /// <summary>Botón para salir de la sala.</summary>
    public Button botonSalir;

    // =========================
    // PANELES
    // =========================

    /// <summary>Este mismo panel de sala de espera.</summary>
    [Header("Paneles")]
    public GameObject panelActual;

    /// <summary>Panel al que se regresa al salir de la sala.</summary>
    public GameObject panelJuegaLoteria;

    /// <summary>Llave usada para leer el ID de la lotería guardado en PlayerPrefs.</summary>
    private const string KEY_LOTERIA_DOC_ID = "loteria_doc_id";

    /// <summary>
    /// Cada vez que se activa este panel, carga los datos de la sala,
    /// conecta los botones, y empieza a cargar las cartas de la lotería.
    /// </summary>
    private void OnEnable()
    {
        CargarDatos();
        ConfigurarBotones();
        IniciarCargaCache();
    }

    /// <summary>
    /// Al desactivarse este panel, se desconecta de los avisos de
    /// <see cref="LoteriaDataCache"/> para evitar errores o fugas de memoria.
    /// </summary>
    private void OnDisable()
    {
        // Desuscribir para evitar memory leaks
        if (LoteriaDataCache.Instance != null)
        {
            LoteriaDataCache.Instance.OnCacheListoOK -= AlCacheListoOK;
            LoteriaDataCache.Instance.OnCacheError -= AlCacheError;
        }
    }

    // ── Carga de datos de sesión ─────────────────────────────────

    /// <summary>
    /// Muestra el código de la sala (separado letra por letra) y la
    /// cantidad actual de jugadores, usando los datos guardados en
    /// <see cref="SalaSessionManager"/>.
    /// </summary>
    void CargarDatos()
    {
        if (SalaSessionManager.Instance == null) return;

        string codigo = SalaSessionManager.Instance.codigoSala;
        int jugadores = SalaSessionManager.Instance.jugadoresEnSala;

        if (textoPIN != null)
            textoPIN.text = string.Join(" ", codigo.ToCharArray());

        if (textoJugadores != null)
            textoJugadores.text = "Jugadores: " + jugadores;
    }

    /// <summary>
    /// Conecta los tres botones de esta pantalla con sus funciones.
    /// El botón de iniciar juego empieza deshabilitado, ya que solo
    /// se activa cuando las cartas terminan de cargarse.
    /// </summary>
    void ConfigurarBotones()
    {
        if (botonCopiarPIN != null)
        {
            botonCopiarPIN.onClick.RemoveAllListeners();
            botonCopiarPIN.onClick.AddListener(CopiarPIN);
        }

        if (botonIniciarJuego != null)
        {
            botonIniciarJuego.onClick.RemoveAllListeners();
            botonIniciarJuego.onClick.AddListener(IniciarJuego);
            botonIniciarJuego.interactable = false; // se habilita cuando cache esté listo
        }

        if (botonSalir != null)
        {
            botonSalir.onClick.RemoveAllListeners();
            botonSalir.onClick.AddListener(SalirSala);
        }
    }

    // ── Cache ────────────────────────────────────────────────────

    /// <summary>
    /// Busca el ID de la lotería guardado en PlayerPrefs y pide que se
    /// carguen sus cartas (usando <see cref="LoteriaDataCache"/>). Si las
    /// cartas ya estaban cargadas de antes, avisa de inmediato que todo está listo.
    /// </summary>
    void IniciarCargaCache()
    {
        string loteriaDocId = PlayerPrefs.GetString(KEY_LOTERIA_DOC_ID, "");

        if (string.IsNullOrWhiteSpace(loteriaDocId))
        {
            SetTextoEstado("No hay lotería seleccionada.");
            return;
        }

        if (LoteriaDataCache.Instance == null)
        {
            SetTextoEstado("Cache no disponible.");
            return;
        }

        // Si ya estaba listo, habilitar de inmediato
        if (LoteriaDataCache.Instance.Listo)
        {
            AlCacheListoOK();
            return;
        }

        SetTextoEstado("Cargando cartas...");
        botonIniciarJuego.interactable = false;

        LoteriaDataCache.Instance.OnCacheListoOK -= AlCacheListoOK;
        LoteriaDataCache.Instance.OnCacheListoOK += AlCacheListoOK;

        LoteriaDataCache.Instance.OnCacheError -= AlCacheError;
        LoteriaDataCache.Instance.OnCacheError += AlCacheError;

        LoteriaDataCache.Instance.CargarSiNecesario(loteriaDocId);
    }

    /// <summary>
    /// Se llama automáticamente cuando las cartas de la lotería ya están listas.
    /// Muestra cuántas cartas se cargaron y, si este jugador es el anfitrión,
    /// habilita el botón de iniciar juego.
    /// </summary>
    void AlCacheListoOK()
    {
        LoteriaDataCache.Instance.OnCacheListoOK -= AlCacheListoOK;
        LoteriaDataCache.Instance.OnCacheError -= AlCacheError;

        int total = LoteriaDataCache.Instance.TotalCartas;
        SetTextoEstado("✓ " + total + " cartas listas");

        // Solo el anfitrión puede iniciar
        if (botonIniciarJuego != null)
            botonIniciarJuego.interactable = PhotonNetwork.IsMasterClient;
    }

    /// <summary>
    /// Se llama automáticamente si algo falla al cargar las cartas de la lotería.
    /// Muestra el error en pantalla.
    /// </summary>
    /// <param name="error">Descripción del error ocurrido.</param>
    void AlCacheError(string error)
    {
        LoteriaDataCache.Instance.OnCacheListoOK -= AlCacheListoOK;
        LoteriaDataCache.Instance.OnCacheError -= AlCacheError;

        SetTextoEstado("Error: " + error);
    }

    /// <summary>
    /// Muestra un mensaje en el texto de estado y también lo registra en la consola.
    /// </summary>
    /// <param name="msg">El mensaje a mostrar.</param>
    void SetTextoEstado(string msg)
    {
        if (textoEstadoCarga != null)
            textoEstadoCarga.text = msg;

        Debug.Log("[SalaEsperaAnfitrion] " + msg);
    }

    // ── Acciones de botones ──────────────────────────────────────

    /// <summary>
    /// Copia el código de la sala al portapapeles del dispositivo,
    /// para que el anfitrión pueda compartirlo fácilmente.
    /// </summary>
    void CopiarPIN()
    {
        if (SalaSessionManager.Instance == null) return;
        GUIUtility.systemCopyBuffer = SalaSessionManager.Instance.codigoSala;
        Debug.Log("PIN copiado: " + SalaSessionManager.Instance.codigoSala);
    }

    /// <summary>
    /// Inicia la partida para todos los jugadores en la sala. Solo funciona
    /// si las cartas ya terminaron de cargar y si quien lo llama es el anfitrión.
    /// </summary>
    /// <remarks>
    /// Usa un RPC de Photon (<c>RPC_IniciarPartidaVisual</c>) para avisarle
    /// a todos los jugadores conectados que la partida ya comenzó, no solo
    /// al anfitrión.
    /// </remarks>
    void IniciarJuego()
    {
        if (!LoteriaDataCache.Instance.Listo)
        {
            SetTextoEstado("Espera, aún cargando cartas...");
            return;
        }

        if (!PhotonNetwork.IsMasterClient) return;

        LoteriaMultiplayerManager.Instance.photonView.RPC(
            nameof(LoteriaMultiplayerManager.RPC_IniciarPartidaVisual),
            RpcTarget.All
        );
    }

    /// <summary>
    /// Saca al anfitrión de la sala de Photon (si estaba en una) y regresa
    /// a la pantalla de selección de lotería.
    /// </summary>
    void SalirSala()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        if (panelActual != null) panelActual.SetActive(false);
        if (panelJuegaLoteria != null) panelJuegaLoteria.SetActive(true);
    }

    /// <summary>
    /// Vuelve a cargar los datos de la sala (código y cantidad de jugadores).
    /// Se llama desde fuera (por ejemplo desde <c>SalaFlowManager</c>) cuando
    /// cambia algo en la sesión y hay que refrescar lo que se ve en pantalla.
    /// </summary>
    public void RefrescarDesdeSession()
    {
        CargarDatos();
    }
}