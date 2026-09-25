using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

/// <summary>
/// Este script controla todo el flujo de selección de modo de juego:
/// elegir entre jugar Solo o Multijugador, crear o unirse a una sala,
/// y mostrar las pantallas correctas según lo que vaya pasando
/// (esperando jugadores, cuenta regresiva, etc). Es el "director de tráfico"
/// entre todos los paneles relacionados con iniciar una partida.
/// </summary>
public class SalaFlowManager : MonoBehaviour
{
    // =========================
    // BOTONES DE MODO
    // =========================

    /// <summary>Botón para elegir el modo Solo.</summary>
    [Header("Botones modo")]
    public Button botonJugarSolo;

    /// <summary>Botón para elegir el modo Multijugador.</summary>
    public Button botonJugarMultijugador;

    /// <summary>Botón para comenzar la partida (usado en modo Solo).</summary>
    public Button botonComenzar;

    /// <summary>Botón para regresar a la pantalla anterior.</summary>
    public Button botonRegresar;

    /// <summary>Botón para jugar en modo solo</summary>
    public Button botonPlaySolo;

    // =========================
    // PANELES PRINCIPALES
    // =========================

    /// <summary>Panel principal donde el jugador elige la lotería a jugar.</summary>
    [Header("Paneles principales")]
    public GameObject panelJuegaLoteria;

    /// <summary>Panel con las opciones de sala multijugador (crear/unirse).</summary>
    public GameObject panelMultiSala;

    /// <summary>Fondo oscuro que aparece detrás del panel de meter código.</summary>
    public GameObject panelFondo;

    /// <summary>Panel donde el jugador escribe el código de una sala para unirse.</summary>
    public GameObject panelMeterCode;

    // =========================
    // BLOQUES VISUALES POR MODO
    // =========================

    /// <summary>Bloque visual que muestra la modalidad de juego (arrastrar el objeto "Mod_game").</summary>
    [Header("Bloques visuales por modo")]
    public GameObject panelModalidadJuego;

    /// <summary>Bloque opcional para agrupar elementos exclusivos del modo Solo.</summary>
    public GameObject bloqueSolo;

    // =========================
    // BOTONES PANEL MULTIJUGADOR
    // =========================

    /// <summary>Botón para crear una sala nueva.</summary>
    [Header("Botones panel multijugador")]
    public Button buttonCrearSala;

    /// <summary>Botón para abrir el panel de unirse a una sala por código.</summary>
    public Button buttonUnirseSala;

    // =========================
    // PANEL UNIRSE POR CÓDIGO
    // =========================

    /// <summary>Campo de texto donde se escribe el código de la sala.</summary>
    [Header("Panel unirse por código")]
    public TMP_InputField codeJoinInput;

    /// <summary>Botón para confirmar e intentar unirse con el código escrito.</summary>
    public Button buttonIngresar;

    // =========================
    // CUENTA REGRESIVA MODO SOLO
    // =========================

    /// <summary>Cuánto dura la cuenta regresiva antes de iniciar el modo Solo.</summary>
    [Header("Cuenta regresiva modo solo")]
    public float duracionCuentaRegresivaSolo = 5f;

    /// <summary>Referencia a la coroutine de cuenta regresiva, para poder detenerla si hace falta.</summary>
    private Coroutine rutinaInicioSolo;

    // =========================
    // PANELES SIGUIENTES
    // =========================

    /// <summary>Panel de cuenta regresiva antes de empezar a jugar.</summary>
    [Header("Paneles siguientes")]
    public GameObject panelCuentaRegresiva;

    /// <summary>Panel de espera que ve el anfitrión mientras se unen otros jugadores.</summary>
    public GameObject panelSalaEsperaAnfitrion;

    /// <summary>Panel de espera que ve un jugador normal antes de que el anfitrión inicie.</summary>
    public GameObject panelSalaEsperaPlayer;

    /// <summary>Panel donde se juega la lotería en modo Solo.</summary>
    public GameObject panelJuegaLoteriaSolo;

    /// <summary>Panel donde se juega la lotería en modo Multijugador.</summary>
    public GameObject panelJuegaLoteriaMultiplayer;

    /// <summary>Panel del menú principal.</summary>
    public GameObject panelMenu;

    // =========================
    // TEXTO DE ESTADO
    // =========================

    /// <summary>Texto usado para mostrar mensajes de estado o error al jugador.</summary>
    [Header("Texto opcional de error/estado")]
    public TMP_Text textoEstado;

    // =========================
    // MODO ACTUAL
    // =========================

    /// <summary>Indica si el modo seleccionado actualmente es Multijugador (true) o Solo (false).</summary>
    [Header("Modo actual")]
    public bool modoMultijugador = false;

    /// <summary>Llave usada para guardar/leer en PlayerPrefs el ID de la lotería elegida.</summary>
    private const string KEY_LOTERIA_DOC_ID = "loteria_doc_id";

    /// <summary>
    /// Al iniciar, deja todos los paneles secundarios ocultos, conecta los botones
    /// con sus funciones, se suscribe a los avisos de <see cref="PhotonRoomManager"/>,
    /// y deja el modo Solo seleccionado por defecto.
    /// </summary>
    private void Start()
    {
        if (panelMultiSala != null) panelMultiSala.SetActive(false);
        if (panelFondo != null) panelFondo.SetActive(false);
        if (panelMeterCode != null) panelMeterCode.SetActive(false);

        if (panelSalaEsperaAnfitrion != null) panelSalaEsperaAnfitrion.SetActive(false);
        if (panelSalaEsperaPlayer != null) panelSalaEsperaPlayer.SetActive(false);
        if (panelCuentaRegresiva != null) panelCuentaRegresiva.SetActive(false);
        if (panelJuegaLoteriaSolo != null) panelJuegaLoteriaSolo.SetActive(false);
        if (panelJuegaLoteriaMultiplayer != null) panelJuegaLoteriaMultiplayer.SetActive(false);
        if (panelMenu != null) panelMenu.SetActive(true);
        if (textoEstado != null) textoEstado.text = "";

        if (botonRegresar != null)
        {
            botonRegresar.onClick.RemoveAllListeners();
            botonRegresar.onClick.AddListener(RegresarAMenu);
        }

        if (botonJugarSolo != null)
        {
            botonJugarSolo.onClick.RemoveAllListeners();
            botonJugarSolo.onClick.AddListener(SeleccionarModoSolo);
        }

        if (botonPlaySolo != null)
        {
            botonPlaySolo.onClick.RemoveAllListeners();
            botonPlaySolo.onClick.AddListener(ComenzarFlujo);
            botonPlaySolo.gameObject.SetActive(false);
        }

        if (botonJugarMultijugador != null)
        {
            botonJugarMultijugador.onClick.RemoveAllListeners();
            botonJugarMultijugador.onClick.AddListener(SeleccionarModoMultijugador);
        }

        if (buttonCrearSala != null)
        {
            buttonCrearSala.onClick.RemoveAllListeners();
            buttonCrearSala.onClick.AddListener(CrearSala);
        }

        if (buttonUnirseSala != null)
        {
            buttonUnirseSala.onClick.RemoveAllListeners();
            buttonUnirseSala.onClick.AddListener(AbrirPanelMeterCode);
        }

        if (buttonIngresar != null)
        {
            buttonIngresar.onClick.RemoveAllListeners();
            buttonIngresar.onClick.AddListener(IngresarASala);
        }

        if (botonComenzar != null)
        {
            botonComenzar.onClick.RemoveAllListeners();
            botonComenzar.onClick.AddListener(ComenzarFlujo);
        }

        // El fondo oscuro también actúa como botón: si le dan clic, cierra el panel de código
        if (panelFondo != null)
        {
            Button fondoButton = panelFondo.GetComponent<Button>();
            if (fondoButton != null)
            {
                fondoButton.onClick.RemoveAllListeners();
                fondoButton.onClick.AddListener(CerrarPanelMeterCode);
            }
        }

        // Nos suscribimos a los avisos del manager de salas de Photon
        if (PhotonRoomManager.Instance != null)
        {
            PhotonRoomManager.Instance.OnRoomJoined -= ManejarSalaUnida;
            PhotonRoomManager.Instance.OnRoomJoined += ManejarSalaUnida;

            PhotonRoomManager.Instance.OnPlayerCountChanged -= ActualizarConteoJugadores;
            PhotonRoomManager.Instance.OnPlayerCountChanged += ActualizarConteoJugadores;

            PhotonRoomManager.Instance.OnJoinFailed -= ManejarErrorUnirse;
            PhotonRoomManager.Instance.OnJoinFailed += ManejarErrorUnirse;

            PhotonRoomManager.Instance.OnCreateFailed -= ManejarErrorCrear;
            PhotonRoomManager.Instance.OnCreateFailed += ManejarErrorCrear;
        }

        modoMultijugador = false;
        ActualizarVistaModo();
    }

    /// <summary>
    /// Se ejecuta cuando este objeto se destruye. Se desconecta de todos los
    /// avisos de <see cref="PhotonRoomManager"/> para evitar errores o fugas de memoria.
    /// </summary>
    private void OnDestroy()
    {
        if (PhotonRoomManager.Instance != null)
        {
            PhotonRoomManager.Instance.OnRoomJoined -= ManejarSalaUnida;
            PhotonRoomManager.Instance.OnPlayerCountChanged -= ActualizarConteoJugadores;

            PhotonRoomManager.Instance.OnJoinFailed -= ManejarErrorUnirse;
            PhotonRoomManager.Instance.OnCreateFailed -= ManejarErrorCrear;
        }
    }

    /// <summary>
    /// Se llama cuando falla el intento de unirse a una sala. Muestra un mensaje genérico en pantalla.
    /// </summary>
    /// <param name="mensaje">Detalle del error (no se muestra directamente al jugador).</param>
    void ManejarErrorUnirse(string mensaje)
    {
        if (textoEstado != null)
            textoEstado.text = "No se pudo unir: código inválido o sala llena.";
    }

    /// <summary>
    /// Se llama cuando falla el intento de crear una sala. Muestra un mensaje genérico en pantalla.
    /// </summary>
    /// <param name="mensaje">Detalle del error (no se muestra directamente al jugador).</param>
    void ManejarErrorCrear(string mensaje)
    {
        if (textoEstado != null)
            textoEstado.text = "No se pudo crear la sala. Intenta de nuevo.";
    }

    // =========================
    // SELECCIÓN DE MODO
    // =========================

    /// <summary>
    /// Selecciona el modo Solo y actualiza los paneles en pantalla acorde a eso.
    /// </summary>
    public void SeleccionarModoSolo()
    {
        modoMultijugador = false;
        ActualizarVistaModo();

        if (botonPlaySolo != null) botonPlaySolo.gameObject.SetActive(true);

        if (textoEstado != null)
            textoEstado.text = "Modo Solo seleccionado.";
    }

    /// <summary>
    /// Ajusta qué paneles y botones se muestran según el modo actual
    /// (Solo o Multijugador), y cierra cualquier panel secundario abierto.
    /// </summary>
    private void ActualizarVistaModo()
    {
        bool esMulti = modoMultijugador;

        if (panelMultiSala != null)
            panelMultiSala.SetActive(esMulti);

        if (panelModalidadJuego != null)
            panelModalidadJuego.SetActive(esMulti);

        if (botonComenzar != null)
            botonComenzar.gameObject.SetActive(!esMulti);

        if (bloqueSolo != null)
            bloqueSolo.SetActive(!esMulti);

        CerrarPanelMeterCode();

        if (panelCuentaRegresiva != null)
            panelCuentaRegresiva.SetActive(false);

        if (panelJuegaLoteriaSolo != null)
            panelJuegaLoteriaSolo.SetActive(false);
    }

    /// <summary>
    /// Selecciona el modo Multijugador y actualiza los paneles en pantalla acorde a eso.
    /// </summary>
    public void SeleccionarModoMultijugador()
    {
        modoMultijugador = true;
        ActualizarVistaModo();

        if (botonPlaySolo != null) botonPlaySolo.gameObject.SetActive(false);

        if (textoEstado != null)
            textoEstado.text = "Modo Multijugador seleccionado.";
    }

    /// <summary>Oculta el panel de selección y abre el panel de juego en modo Solo.</summary>
    public void IrAJugarSolo()
    {
    if (modoMultijugador) return;

    if (panelJuegaLoteria != null) panelJuegaLoteria.SetActive(false);
    if (panelJuegaLoteriaSolo != null) panelJuegaLoteriaSolo.SetActive(true);
    }

    public void RegresarAMenu()
    {
    if (panelJuegaLoteria != null) panelJuegaLoteria.SetActive(false);
    if (panelMenu != null) panelMenu.SetActive(true); 
    }

    // =========================
    // PANEL METER CÓDIGO
    // =========================

    /// <summary>
    /// Abre el panel donde el jugador escribe el código de una sala para unirse.
    /// Solo funciona si el modo Multijugador está seleccionado.
    /// </summary>
    public void AbrirPanelMeterCode()
    {
        if (!modoMultijugador)
            return;

        if (panelFondo != null) panelFondo.SetActive(true);
        if (panelMeterCode != null) panelMeterCode.SetActive(true);

        if (textoEstado != null)
            textoEstado.text = "Ingresa el código de sala.";
    }

    /// <summary>
    /// Cierra el panel de meter código, oculta el fondo, y limpia lo escrito en el campo.
    /// </summary>
    public void CerrarPanelMeterCode()
    {
        if (panelFondo != null) panelFondo.SetActive(false);
        if (panelMeterCode != null) panelMeterCode.SetActive(false);

        if (codeJoinInput != null)
            codeJoinInput.text = "";
    }

    // =========================
    // CREAR SALA
    // =========================

    /// <summary>
    /// Crea una nueva sala multijugador para la lotería actualmente seleccionada.
    /// </summary>
    /// <remarks>
    /// Requiere que ya se haya elegido una lotería previamente (guardada en PlayerPrefs),
    /// y que exista un <see cref="PhotonRoomManager"/> activo en la escena.
    /// </remarks>
    public void CrearSala()
    {
        if (!modoMultijugador)
            return;

        if (PhotonRoomManager.Instance == null)
        {
            if (textoEstado != null) textoEstado.text = "PhotonRoomManager no existe.";
            return;
        }

        string loteriaDocId = PlayerPrefs.GetString(KEY_LOTERIA_DOC_ID, "");
        if (string.IsNullOrWhiteSpace(loteriaDocId))
        {
            if (textoEstado != null) textoEstado.text = "Primero selecciona o crea una lotería.";
            return;
        }

        string codigoSala = GenerarCodigoSala();

        if (SalaSessionManager.Instance != null)
            SalaSessionManager.Instance.ConfigurarComoAnfitrion(codigoSala);

        // Si el jugador no tiene nombre guardado, le asignamos uno aleatorio
        string playerName = PlayerPrefs.GetString("usuario", "");
        if (string.IsNullOrWhiteSpace(playerName))
            playerName = "Jugador_" + Random.Range(1000, 9999);

        PhotonRoomManager.Instance.SetPlayerName(playerName);
        PhotonRoomManager.Instance.CreateRoom(codigoSala, loteriaDocId);

        if (textoEstado != null)
            textoEstado.text = "Creando sala: " + codigoSala;
    }

    // =========================
    // UNIRSE A SALA
    // =========================

    /// <summary>
    /// Intenta unir al jugador a una sala existente usando el código escrito
    /// en <see cref="codeJoinInput"/>.
    /// </summary>
    public void IngresarASala()
    {
        if (!modoMultijugador)
            return;

        if (codeJoinInput == null)
        {
            Debug.LogWarning("No hay input de código asignado.");
            return;
        }

        string codigo = codeJoinInput.text.Trim();

        if (string.IsNullOrEmpty(codigo))
        {
            if (textoEstado != null)
                textoEstado.text = "Ingresa un código.";
            return;
        }

        if (codigo.Length != 6)
        {
            if (textoEstado != null)
                textoEstado.text = "El código debe tener 6 dígitos.";
            return;
        }

        if (PhotonRoomManager.Instance == null)
        {
            if (textoEstado != null) textoEstado.text = "PhotonRoomManager no existe.";
            return;
        }

        if (SalaSessionManager.Instance != null)
            SalaSessionManager.Instance.ConfigurarComoPlayer(codigo);

        // Si el jugador no tiene nombre guardado, le asignamos uno aleatorio
        string playerName = PlayerPrefs.GetString("usuario", "");
        if (string.IsNullOrWhiteSpace(playerName))
            playerName = "Jugador_" + Random.Range(1000, 9999);

        PhotonRoomManager.Instance.SetPlayerName(playerName);
        PhotonRoomManager.Instance.JoinRoomByCode(codigo);

        if (textoEstado != null)
            textoEstado.text = "Uniéndose a sala: " + codigo;

        Debug.Log("Jugador intentando unirse a sala con código: " + codigo);
    }

    // =========================
    // EVENTOS DE SALA
    // =========================

    /// <summary>
    /// Se llama automáticamente cuando el jugador entra exitosamente a una sala.
    /// Cierra los paneles de selección y muestra la pantalla de espera correcta
    /// según si este jugador es el anfitrión o no.
    /// </summary>
    private void ManejarSalaUnida()
    {
        CerrarPanelMeterCode();

        if (panelJuegaLoteria != null) panelJuegaLoteria.SetActive(false);
        if (panelMultiSala != null) panelMultiSala.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            if (panelSalaEsperaAnfitrion != null) panelSalaEsperaAnfitrion.SetActive(true);
            if (panelSalaEsperaPlayer != null) panelSalaEsperaPlayer.SetActive(false);
        }
        else
        {
            if (panelSalaEsperaPlayer != null) panelSalaEsperaPlayer.SetActive(true);
            if (panelSalaEsperaAnfitrion != null) panelSalaEsperaAnfitrion.SetActive(false);
        }

        ActualizarConteoJugadores(PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 1);

        if (textoEstado != null)
            textoEstado.text = "Sala unida correctamente.";
    }

    /// <summary>
    /// Se llama automáticamente cuando cambia la cantidad de jugadores en la sala.
    /// Actualiza el conteo guardado y avisa a las pantallas de espera (anfitrión y jugador)
    /// para que refresquen lo que muestran.
    /// </summary>
    /// <param name="cantidad">Cantidad actual de jugadores en la sala.</param>
    private void ActualizarConteoJugadores(int cantidad)
    {
        if (SalaSessionManager.Instance != null)
            SalaSessionManager.Instance.jugadoresEnSala = cantidad;

        PanelSalaEsperaAnfitrionUI uiHost = FindFirstObjectByType<PanelSalaEsperaAnfitrionUI>();
        if (uiHost != null)
            uiHost.RefrescarDesdeSession();

        PanelSalaEsperaPlayerUI uiPlayer = FindFirstObjectByType<PanelSalaEsperaPlayerUI>();
        if (uiPlayer != null)
            uiPlayer.ActualizarJugadores(cantidad);
    }

    // =========================
    // COMENZAR FLUJO
    // =========================

    /// <summary>
    /// Inicia el flujo de comenzar a jugar. En modo Multijugador solo pide que
    /// el jugador elija Crear Sala o Unirse Sala primero. En modo Solo, configura
    /// y muestra la cuenta regresiva antes de empezar la partida.
    /// </summary>
    public void ComenzarFlujo()
    {
        if (modoMultijugador)
        {
            if (textoEstado != null)
                textoEstado.text = "Selecciona Crear Sala o Unirse Sala.";
            return;
        }

        // Configurar countdown en modo SOLO
        if (panelCuentaRegresiva != null)
        {
            CuentaRegresivaVideo cuenta = panelCuentaRegresiva.GetComponent<CuentaRegresivaVideo>();

            if (cuenta == null)
                cuenta = panelCuentaRegresiva.GetComponentInChildren<CuentaRegresivaVideo>(true);

            if (cuenta != null)
                cuenta.ConfigurarModo(false);
        }

        if (string.IsNullOrWhiteSpace(PlayerPrefs.GetString(KEY_LOTERIA_DOC_ID, "")))
        {
            if (textoEstado != null) textoEstado.text = "Primero selecciona una lotería.";
            return;
        }

        if (panelJuegaLoteria != null)
            panelJuegaLoteria.SetActive(false);

        if (panelCuentaRegresiva != null)
            panelCuentaRegresiva.SetActive(true);

        if (textoEstado != null)
            textoEstado.text = "Iniciando modo solo...";

        Debug.Log("Inicia flujo SOLO.");
    }

    /// <summary>
    /// Genera un código aleatorio de 6 dígitos para identificar una sala nueva.
    /// </summary>
    /// <returns>Código de sala como texto, por ejemplo "482913".</returns>
    private string GenerarCodigoSala()
    {
        return Random.Range(100000, 999999).ToString();
    }
}
