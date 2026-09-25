using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using System;

/// <summary>
/// Este script se encarga de todo lo relacionado con las salas multijugador (usando Photon):
/// crear una sala, unirse a una con un código, y avisar cuando entran o salen jugadores.
/// Es un "singleton", o sea que solo existe una instancia de este script en todo el juego,
/// y se puede acceder a él desde cualquier parte usando <c>PhotonRoomManager.Instance</c>.
/// </summary>
public class PhotonRoomManager : MonoBehaviourPunCallbacks
{
    /// <summary>
    /// La única instancia de este script que existe en el juego.
    /// Así cualquier otro script puede usarlo sin necesidad de buscarlo.
    /// </summary>
    public static PhotonRoomManager Instance;

    // =========================
    // ESTADO ACTUAL
    // =========================

    /// <summary>Código de la sala en la que estamos actualmente.</summary>
    [Header("Estado actual")]
    public string currentRoomCode = "";

    /// <summary>Indica si este jugador es el que creó la sala (el host).</summary>
    public bool isHost = false;

    // =========================
    // EVENTOS
    // =========================
    // Estos "Action" son avisos que otros scripts pueden escuchar
    // para enterarse cuando pasa algo (por ejemplo, cuando alguien entra a la sala).

    /// <summary>Se dispara cuando el jugador entra exitosamente a una sala.</summary>
    public Action OnRoomJoined;

    /// <summary>Se dispara cuando cambia la cantidad de jugadores en la sala. Manda el nuevo total.</summary>
    public Action<int> OnPlayerCountChanged;

    /// <summary>Se dispara cuando falla el intento de unirse a una sala. Manda el motivo del error.</summary>
    public Action<string> OnJoinFailed;

    /// <summary>Se dispara cuando falla el intento de crear una sala. Manda el motivo del error.</summary>
    public Action<string> OnCreateFailed;

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
            // Si ya existe una instancia, destruimos esta copia para no duplicar el manager
            Destroy(gameObject);
        }
    }

    // =========================
    // CONFIGURACIÓN DEL JUGADOR
    // =========================

    /// <summary>
    /// Define el nombre con el que este jugador va a aparecer ante los demás.
    /// </summary>
    /// <param name="playerName">Nombre que se le va a mostrar a los otros jugadores.</param>
    public void SetPlayerName(string playerName)
    {
        PhotonNetwork.NickName = playerName;
    }

    // =========================
    // CREAR Y UNIRSE A SALAS
    // =========================

    /// <summary>
    /// Crea una nueva sala multijugador con un código específico,
    /// asociada a una lotería (juego) en particular.
    /// </summary>
    /// <param name="roomCode">Código único que identifica esta sala.</param>
    /// <param name="loteriaDocId">ID del documento de la lotería que se va a jugar en esta sala.</param>
    public void CreateRoom(string roomCode, string loteriaDocId)
    {
        // Si Photon todavía no está listo, no podemos crear la sala
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError("Photon no está listo para crear sala.");
            return;
        }

        // Configuramos las opciones de la sala: cuántos jugadores caben,
        // si aparece en listas públicas, y datos extra que queremos guardar
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 8,
            IsVisible = false,
            IsOpen = true,
            CustomRoomProperties = new Hashtable
            {
                { "roomCode", roomCode },
                { "loteriaDocId", loteriaDocId },
                { "loteriaNombre", PlayerPrefs.GetString("loteria_nombre", "") },
                { "started", false },
                { "currentCardId", -1 }
            },
            // Estas propiedades son las que se pueden consultar desde fuera de la sala (en el lobby)
            CustomRoomPropertiesForLobby = new string[] { "roomCode", "loteriaDocId", "started", "currentCardId" }
        };

        currentRoomCode = roomCode;
        PhotonNetwork.CreateRoom(roomCode, options, TypedLobby.Default);
    }

    /// <summary>
    /// Intenta unir al jugador a una sala existente usando su código.
    /// </summary>
    /// <param name="roomCode">Código de la sala a la que se quiere entrar.</param>
    public void JoinRoomByCode(string roomCode)
    {
        // Si Photon todavía no está listo, no podemos intentar unirnos
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError("Photon no está listo para unirse.");
            return;
        }

        currentRoomCode = roomCode;
        PhotonNetwork.JoinRoom(roomCode);
    }

    public override void OnLeftRoom()
    {
    isHost = false;
    currentRoomCode = "";
    }

    // =========================
    // EVENTOS DE PHOTON (CALLBACKS)
    // =========================
    // Estas funciones las llama Photon automáticamente cuando pasa algo
    // relacionado con la sala (se crea, se entra, entra/sale un jugador, etc).

    /// <summary>Se llama automáticamente cuando la sala se creó exitosamente.</summary>
    public override void OnCreatedRoom()
    {
        isHost = true;
        Debug.Log("Sala creada: " + PhotonNetwork.CurrentRoom.Name);
    }

    /// <summary>
    /// Se llama automáticamente cuando el jugador entró exitosamente a una sala
    /// (ya sea porque la creó o porque se unió a una existente).
    /// </summary>
    public override void OnJoinedRoom()
    {
        // Si este jugador es el "master client" de Photon, entonces es el host
        isHost = PhotonNetwork.IsMasterClient;
        Debug.Log("Entraste a la sala: " + PhotonNetwork.CurrentRoom.Name);

        // Avisamos a quien esté escuchando que ya entramos, y cuántos jugadores hay
        OnRoomJoined?.Invoke();
        OnPlayerCountChanged?.Invoke(PhotonNetwork.CurrentRoom.PlayerCount);
    }

    /// <summary>Se llama automáticamente cuando otro jugador entra a la sala.</summary>
    /// <param name="newPlayer">El jugador que acaba de entrar.</param>
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log("Jugador entró: " + newPlayer.NickName);
        OnPlayerCountChanged?.Invoke(PhotonNetwork.CurrentRoom.PlayerCount);
    }

    /// <summary>Se llama automáticamente cuando otro jugador sale de la sala.</summary>
    /// <param name="otherPlayer">El jugador que acaba de salir.</param>
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log("Jugador salió: " + otherPlayer.NickName);
        OnPlayerCountChanged?.Invoke(PhotonNetwork.CurrentRoom.PlayerCount);
    }

    /// <summary>Se llama automáticamente cuando falla el intento de unirse a una sala.</summary>
    /// <param name="returnCode">Código de error que da Photon.</param>
    /// <param name="message">Mensaje explicando qué salió mal.</param>
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("No se pudo entrar a la sala: " + returnCode + " | " + message);
        OnJoinFailed?.Invoke(message);
    }

    /// <summary>Se llama automáticamente cuando falla el intento de crear una sala.</summary>
    /// <param name="returnCode">Código de error que da Photon.</param>
    /// <param name="message">Mensaje explicando qué salió mal.</param>
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("No se pudo crear la sala: " + returnCode + " | " + message);
        OnCreateFailed?.Invoke(message);
    }
}