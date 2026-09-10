using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

/// <summary>
/// Este script controla la pantalla de espera que ve un jugador normal
/// (no el anfitrión) mientras está en una sala multijugador. Muestra el
/// código de la sala, cuántos jugadores hay, y el progreso de la carga
/// de las cartas de la lotería. También permite salir de la sala.
/// </summary>
public class PanelSalaEsperaPlayerUI : MonoBehaviour
{
    // =========================
    // UI
    // =========================

    /// <summary>Texto que muestra el código (PIN) de la sala.</summary>
    [Header("UI")]
    public TMP_Text textoPIN;

    /// <summary>Texto que muestra cuántos jugadores hay en la sala.</summary>
    public TMP_Text textoJugadores;

    /// <summary>Texto que muestra el estado actual (cargando, listo, error, etc).</summary>
    public TMP_Text textoEstado;

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

    /// <summary>Panel de cuenta regresiva antes de empezar a jugar.</summary>
    public GameObject panelCuentaRegresiva;

    /// <summary>Llave usada para leer el ID de la lotería guardada en las propiedades de la sala.</summary>
    private const string KEY_LOTERIA_DOC_ID = "loteria_doc_id";

    /// <summary>
    /// Cada vez que se activa este panel, carga los datos de la sala,
    /// conecta el botón de salir, y empieza a cargar las cartas de la lotería.
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
        if (LoteriaDataCache.Instance != null)
        {
            LoteriaDataCache.Instance.OnCacheListoOK -= AlCacheListoOK;
            LoteriaDataCache.Instance.OnCacheError -= AlCacheError;
        }
    }

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
    /// Conecta el botón de salir con su función correspondiente.
    /// </summary>
    void ConfigurarBotones()
    {
        if (botonSalir != null)
        {
            botonSalir.onClick.RemoveAllListeners();
            botonSalir.onClick.AddListener(SalirSala);
        }
    }

    /// <summary>
    /// Busca el ID de la lotería guardado en las propiedades de la sala de Photon,
    /// y pide que se carguen sus cartas (usando <see cref="LoteriaDataCache"/>).
    /// Si las cartas ya estaban cargadas de antes, avisa de inmediato que todo está listo.
    /// </summary>
    void IniciarCargaCache()
    {
        string loteriaDocId = "";
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("loteriaDocId", out object valor))
        {
            loteriaDocId = (string)valor;
        }

        if (string.IsNullOrWhiteSpace(loteriaDocId))
        {
            SetTextoEstado("Sin lotería seleccionada.");
            return;
        }

        if (LoteriaDataCache.Instance == null) return;

        // Si el caché ya estaba listo de antes, no hace falta esperar ningún aviso
        if (LoteriaDataCache.Instance.Listo)
        {
            AlCacheListoOK();
            return;
        }

        SetTextoEstado("Cargando cartas...");

        LoteriaDataCache.Instance.OnCacheListoOK -= AlCacheListoOK;
        LoteriaDataCache.Instance.OnCacheListoOK += AlCacheListoOK;

        LoteriaDataCache.Instance.OnCacheError -= AlCacheError;
        LoteriaDataCache.Instance.OnCacheError += AlCacheError;

        LoteriaDataCache.Instance.CargarSiNecesario(loteriaDocId);
    }

    /// <summary>
    /// Se llama automáticamente cuando las cartas de la lotería ya están listas.
    /// Muestra un mensaje de confirmación y deja de escuchar los avisos del caché.
    /// </summary>
    void AlCacheListoOK()
    {
        LoteriaDataCache.Instance.OnCacheListoOK -= AlCacheListoOK;
        LoteriaDataCache.Instance.OnCacheError -= AlCacheError;

        SetTextoEstado("✓ Cartas listas. Esperando al anfitrión...");
    }

    /// <summary>
    /// Se llama automáticamente si algo falla al cargar las cartas de la lotería.
    /// Muestra el error en pantalla y deja de escuchar los avisos del caché.
    /// </summary>
    /// <param name="error">Descripción del error ocurrido.</param>
    void AlCacheError(string error)
    {
        LoteriaDataCache.Instance.OnCacheListoOK -= AlCacheListoOK;
        LoteriaDataCache.Instance.OnCacheError -= AlCacheError;

        SetTextoEstado("Error cargando cartas: " + error);
    }

    /// <summary>
    /// Muestra un mensaje en el texto de estado.
    /// </summary>
    /// <param name="msg">El mensaje a mostrar.</param>
    void SetTextoEstado(string msg)
    {
        if (textoEstado != null)
            textoEstado.text = msg;
    }

    /// <summary>
    /// Saca al jugador de la sala de Photon (si estaba en una) y regresa
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
    /// Actualiza el texto que muestra cuántos jugadores hay en la sala.
    /// </summary>
    /// <param name="cantidad">Cantidad actual de jugadores.</param>
    public void ActualizarJugadores(int cantidad)
    {
        if (textoJugadores != null)
            textoJugadores.text = "Jugadores: " + cantidad;
    }
}