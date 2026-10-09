using Photon.Pun;
using Photon.Realtime;
using Firebase.Auth;
using UnityEngine;


/// <summary>
/// Este script se encarga de conectar el juego al servidor de Photon
/// (el servicio que permite el multijugador). Es un singleton, así que
/// solo existe una instancia y se puede acceder desde cualquier parte
/// usando <c>PhotonLauncher.Instance</c>.
/// </summary>
public class PhotonLauncher : MonoBehaviourPunCallbacks
{
    /// <summary>
    /// Instancia de este script que existe en el juego.
    /// </summary>
    public static PhotonLauncher Instance;

    /// <summary>
    /// La variable que indica si el juego debe reconectarse automáticamente a Photon
    /// después de una desconexión. Se puede activar desde cualquier parte del juego
    /// </summary
    private bool reconectarDespuesDeDesconexion = false;

    /// <summary>
    /// Si está activado, el juego intenta conectarse a Photon automáticamente
    /// apenas arranca la escena.
    /// </summary>
    [Header("Debug")]
    public bool autoConnectOnStart = true;

    /// <summary>
    /// Se ejecuta al cargar el script. Aquí se asegura de que solo exista
    /// una instancia de este manager en todo el juego (patrón singleton),
    /// y evita que se destruya al cambiar de escena.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Al iniciar el juego, si "autoConnectOnStart" está activado,
    /// se conecta automáticamente a Photon.
    /// </summary>
    private void Start()
    {
        if (autoConnectOnStart)
        {
            Connect();
        }
    }

    /// <summary>
    /// Intenta conectar el juego al servidor de Photon.
    /// Si ya está conectado, no hace nada.
    /// </summary>
    public void Connect()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

        if (user != null)
        {
            PhotonNetwork.AuthValues = new AuthenticationValues(user.UserId);
        }

        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Photon ya está conectado.");
            return;
        }

        // Evita que Photon cambie de escena automáticamente al conectar
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("Conectando a Photon...");
    }

    /// <summary>
    /// Llamar después del login: hace que Photon use el UID de Firebase
    /// (necesario para amigos) y el nombre real del jugador.
    /// Si ya estaba conectado con otro UserId, reconecta.
    /// </summary>
    public void ConfigurarUsuario(string uid, string nombre)
    {
        if (!string.IsNullOrWhiteSpace(nombre))
        PhotonNetwork.NickName = nombre;
        
        if (PhotonNetwork.AuthValues != null && PhotonNetwork.AuthValues.UserId == uid)
        return;
        
        PhotonNetwork.AuthValues = new AuthenticationValues(uid);
        
        if (PhotonNetwork.IsConnected)
        {
        if (PhotonNetwork.InRoom) return; // no cortar una partida
        reconectarDespuesDeDesconexion = true;
        PhotonNetwork.Disconnect();
        }
        else
        {
        Connect();
        }
    }

    /// <summary>
    /// Se llama automáticamente cuando la conexión con el servidor principal
    /// de Photon (Master) fue exitosa. Si el jugador no tiene nombre asignado,
    /// le pone uno aleatorio.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado a Photon Master.");
        if (string.IsNullOrWhiteSpace(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Jugador_" + Random.Range(1000, 9999);
        }
    }

    /// <summary>
    /// Se llama automáticamente cuando el juego se desconecta de Photon.
    /// </summary>
    /// <param name="cause">Motivo de la desconexión.</param>
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Photon desconectado: " + cause);

        if (reconectarDespuesDeDesconexion)
        {
            Debug.Log("Intentando reconectar a Photon...");
            Connect();
        }
    }
}