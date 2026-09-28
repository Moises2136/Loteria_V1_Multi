using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Controla el panel de Amigos:
/// - Muestra el código de amigo del jugador (y permite copiarlo).
/// - Envía solicitudes con el código escrito.
/// - Llena el Scroll View con solicitudes pendientes (Aceptar/Rechazar) y amigos.
/// - Consulta a Photon cada cierto tiempo si los amigos están conectados o en partida.
/// </summary>
/// <remarks>
/// Hereda de MonoBehaviourPunCallbacks para recibir OnFriendListUpdate.
/// Para que Photon reconozca a los amigos, al conectar a Photon debe asignarse
/// PhotonNetwork.AuthValues.UserId con el UID de Firebase del jugador.
/// </remarks>
public class Panel_Amigos_UI : MonoBehaviourPunCallbacks
{
    // =========================
    // MI CÓDIGO
    // =========================

    [Header("Mi código")]
    public TMP_Text textoMiCodigo;

    /// <summary>Opcional: botón para copiar el código al portapapeles.</summary>
    public Button botonCopiar;

    // =========================
    // AGREGAR
    // =========================

    [Header("Agregar amigo")]
    public TMP_InputField inputCodigo;
    public Button botonAgregar;

    /// <summary>Texto para avisos ("Solicitud enviada", errores, etc.).</summary>
    public TMP_Text textoMensaje;

    // =========================
    // LISTA
    // =========================

    [Header("Lista (dentro de Viewport > Content)")]
    public Transform contenedor;
    public AmigoPrefab prefabItem;

    /// <summary>Encabezado "Solicitudes (n)", hijo de Content. Se oculta si no hay.</summary>
    public TMP_Text encabezadoSolicitudes;

    /// <summary>Encabezado "Amigos", hijo de Content.</summary>
    public TMP_Text encabezadoAmigos;

    /// <summary>Opcional: texto que aparece cuando no hay amigos todavía.</summary>
    public TMP_Text textoSinAmigos;

    // =========================
    // ESTADO EN LÍNEA
    // =========================

    [Header("Estado en línea (Photon)")]
    public float intervaloEstado = 15f;

    // =========================
    // INTERNOS
    // =========================

    readonly List<AmigoPrefab> itemsCreados = new List<AmigoPrefab>();
    readonly Dictionary<string, AmigoPrefab> itemsAmigos = new Dictionary<string, AmigoPrefab>();

    int cargaActual = 0;
    Coroutine rutinaEstado;

    // =========================
    // CICLO DE VIDA
    // =========================

    void Start()
    {
        if (botonAgregar != null)
        {
            botonAgregar.onClick.RemoveAllListeners();
            botonAgregar.onClick.AddListener(AlPresionarAgregar);
        }

        if (botonCopiar != null)
        {
            botonCopiar.onClick.RemoveAllListeners();
            botonCopiar.onClick.AddListener(CopiarCodigo);
        }

        if (inputCodigo != null)
            inputCodigo.characterLimit = 6;
    }

    // IMPORTANTE: llamar a base para que Photon registre/quite los callbacks
    public override void OnEnable()
    {
        base.OnEnable();
        SetMensaje("");
        CargarTodo();
        rutinaEstado = StartCoroutine(ActualizarEstadoPeriodicamente());
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (rutinaEstado != null) StopCoroutine(rutinaEstado);
        rutinaEstado = null;
    }

    // =========================
    // CARGA
    // =========================

    /// <summary>Carga el código, las solicitudes y los amigos, y redibuja la lista.</summary>
    public async void CargarTodo()
    {
        int idCarga = ++cargaActual;

        if (textoMiCodigo != null) textoMiCodigo.text = "...";

        string codigo = await AmigosService.ObtenerOCrearMiCodigo();
        if (!SigueVigente(idCarga)) return;
        if (textoMiCodigo != null) textoMiCodigo.text = codigo ?? "---";

        List<AmigoInfo> solicitudes = await AmigosService.ObtenerSolicitudes();
        List<AmigoInfo> amigos = await AmigosService.ObtenerAmigos();
        if (!SigueVigente(idCarga)) return;

        DibujarLista(solicitudes, amigos);
        PedirEstados();
    }

    bool SigueVigente(int idCarga)
    {
        return this != null && isActiveAndEnabled && idCarga == cargaActual;
    }

    // =========================
    // DIBUJAR LISTA
    // =========================

    void DibujarLista(List<AmigoInfo> solicitudes, List<AmigoInfo> amigos)
    {
        LimpiarLista();

        // Solicitudes arriba
        if (encabezadoSolicitudes != null)
        {
            encabezadoSolicitudes.gameObject.SetActive(solicitudes.Count > 0);
            encabezadoSolicitudes.text = $"Solicitudes ({solicitudes.Count})";
            encabezadoSolicitudes.transform.SetAsLastSibling();
        }

        foreach (AmigoInfo s in solicitudes)
        {
            AmigoPrefab item = CrearItem();
            item.ConfigurarComoSolicitud(s, AlAceptar, AlRechazar);
        }

        // Amigos abajo
        if (encabezadoAmigos != null)
        {
            encabezadoAmigos.text = "Amigos";
            encabezadoAmigos.transform.SetAsLastSibling();
        }

        foreach (AmigoInfo a in amigos)
        {
            AmigoPrefab item = CrearItem();
            item.ConfigurarComoAmigo(a);
            itemsAmigos[a.uid] = item;
        }

        if (textoSinAmigos != null)
        {
            textoSinAmigos.gameObject.SetActive(amigos.Count == 0);
            textoSinAmigos.transform.SetAsLastSibling();
        }
    }

    AmigoPrefab CrearItem()
    {
        AmigoPrefab item = Instantiate(prefabItem, contenedor);
        item.transform.SetAsLastSibling();
        itemsCreados.Add(item);
        return item;
    }

    void LimpiarLista()
    {
        foreach (AmigoPrefab item in itemsCreados)
            if (item != null) Destroy(item.gameObject);

        itemsCreados.Clear();
        itemsAmigos.Clear();
    }

    // =========================
    // AGREGAR
    // =========================

    async void AlPresionarAgregar()
    {
        string codigo = inputCodigo != null ? inputCodigo.text : "";
        if (string.IsNullOrWhiteSpace(codigo))
        {
            SetMensaje("Escribe el código de tu amigo.");
            return;
        }

        if (botonAgregar != null) botonAgregar.interactable = false;
        SetMensaje("Enviando...");

        ResultadoSolicitud resultado = await AmigosService.EnviarSolicitud(codigo);
        if (this == null) return;

        if (botonAgregar != null) botonAgregar.interactable = true;

        switch (resultado)
        {
            case ResultadoSolicitud.Enviada:
                SetMensaje("Solicitud enviada.");
                if (inputCodigo != null) inputCodigo.text = "";
                break;
            case ResultadoSolicitud.AceptadaAutomaticamente:
                SetMensaje("¡Ahora son amigos!");
                if (inputCodigo != null) inputCodigo.text = "";
                CargarTodo();
                break;
            case ResultadoSolicitud.NoEncontrado:
                SetMensaje("No existe ningún jugador con ese código.");
                break;
            case ResultadoSolicitud.EsTuCodigo:
                SetMensaje("Ese es tu propio código.");
                break;
            case ResultadoSolicitud.YaSonAmigos:
                SetMensaje("Ya son amigos.");
                break;
            case ResultadoSolicitud.SinSesion:
                SetMensaje("Inicia sesión para agregar amigos.");
                break;
            default:
                SetMensaje("Ocurrió un error. Intenta de nuevo.");
                break;
        }
    }

    // =========================
    // ACEPTAR / RECHAZAR
    // =========================

    async void AlAceptar(AmigoPrefab item)
    {
        item.SetInteractuable(false);

        bool ok = await AmigosService.AceptarSolicitud(item.Info.uid, item.Info.nombre);
        if (this == null) return;

        if (ok)
        {
            SetMensaje($"Ahora eres amigo de {item.Info.nombre}.");
            CargarTodo();
        }
        else
        {
            if (item != null) item.SetInteractuable(true);
            SetMensaje("No se pudo aceptar. Intenta de nuevo.");
        }
    }

    async void AlRechazar(AmigoPrefab item)
    {
        item.SetInteractuable(false);

        bool ok = await AmigosService.RechazarSolicitud(item.Info.uid);
        if (this == null) return;

        if (ok)
        {
            CargarTodo();
        }
        else
        {
            if (item != null) item.SetInteractuable(true);
            SetMensaje("No se pudo rechazar. Intenta de nuevo.");
        }
    }

    // =========================
    // ESTADO EN LÍNEA (PHOTON)
    // =========================

    IEnumerator ActualizarEstadoPeriodicamente()
    {
        var espera = new WaitForSeconds(intervaloEstado);
        while (true)
        {
            yield return espera;
            PedirEstados();
        }
    }

    /// <summary>
    /// Le pregunta a Photon por los amigos. Solo funciona conectado al servidor
    /// principal (no dentro de una sala); si no se puede, los estados quedan vacíos.
    /// </summary>
    void PedirEstados()
    {
        if (itemsAmigos.Count == 0) return;
        if (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom) return;

        string[] uids = new string[itemsAmigos.Count];
        itemsAmigos.Keys.CopyTo(uids, 0);

        PhotonNetwork.FindFriends(uids);
    }

    /// <summary>Respuesta de Photon con el estado de cada amigo.</summary>
    public override void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        foreach (FriendInfo f in friendList)
        {
            if (!itemsAmigos.TryGetValue(f.UserId, out AmigoPrefab item) || item == null)
                continue;

            if (f.IsInRoom) item.SetEstado(EstadoAmigo.EnPartida);
            else if (f.IsOnline) item.SetEstado(EstadoAmigo.Conectado);
            else item.SetEstado(EstadoAmigo.Desconectado);
        }
    }

    // =========================
    // AUXILIARES
    // =========================

    void CopiarCodigo()
    {
        if (textoMiCodigo == null) return;
        string codigo = textoMiCodigo.text;
        if (string.IsNullOrEmpty(codigo) || codigo == "..." || codigo == "---") return;

        GUIUtility.systemCopyBuffer = codigo;
        SetMensaje("Código copiado.");
    }

    void SetMensaje(string msg)
    {
        if (textoMensaje != null) textoMensaje.text = msg;
    }
}