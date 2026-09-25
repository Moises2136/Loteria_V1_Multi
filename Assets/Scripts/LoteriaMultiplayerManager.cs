// LoteriaMultiplayerManager.cs
// GameObject vacío en escena - maneja TODO lo de red y mazo
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Este script es el "cerebro" de la partida multijugador: decide qué
/// carta se canta y cuándo, valida quién gana, y mantiene sincronizados
/// a todos los jugadores de la sala usando RPCs de Photon (mensajes que
/// se mandan entre los dispositivos conectados). Los paneles de UI (como
/// <c>PanelJuegaLoteriaMultiUI</c>) solo escuchan sus eventos y le reportan
/// aciertos; nunca deciden nada de la partida por su cuenta. Es un
/// singleton, así que solo existe una instancia y se puede acceder desde
/// cualquier parte usando <c>LoteriaMultiplayerManager.Instance</c>.
/// </summary>
/// <remarks>
/// Solo el <b>MasterClient</b> (el anfitrión de la sala, según Photon)
/// controla el mazo y decide qué carta se canta; los demás jugadores solo
/// reciben esa información mediante RPCs y reaccionan a ella. Si el
/// MasterClient se desconecta, Photon asigna uno nuevo automáticamente, y
/// este script está preparado para que ese nuevo MasterClient retome el
/// juego donde se quedó (ver <see cref="RetomarComoMaster"/>).
/// </remarks>
public class LoteriaMultiplayerManager : MonoBehaviourPunCallbacks
{
    /// <summary>
    /// La única instancia de este script que existe en el juego.
    /// </summary>
    public static LoteriaMultiplayerManager Instance { get; private set; }

    /// <summary>Cuántos segundos se muestra cada carta antes de pasar a la siguiente.</summary>
    [Header("Configuración")]
    public float tiempoEntreCartas = 15f;

    // =========================
    // REFERENCIAS DE UI
    // =========================

    /// <summary>Panel de sala de espera que ve el anfitrión, y que se oculta al arrancar la partida.</summary>
    [Header("Referencias de UI")]
    public GameObject panelSalaEsperaAnfitrion;

    /// <summary>Panel de sala de espera que ve un jugador normal, y que se oculta al arrancar la partida.</summary>
    public GameObject panelSalaEsperaPlayer;

    /// <summary>Panel de cuenta regresiva que se muestra justo antes de empezar a jugar.</summary>
    public GameObject panelCuentaRegresiva;

    /// <summary>
    /// Cuántos aciertos lleva cada jugador, indexado por su número de
    /// actor de Photon (<c>ActorNumber</c>). Solo lo usa y mantiene el MasterClient.
    /// </summary>
    [Header("Reporte RPC")]
    private Dictionary<int, int> aciertosPorJugador = new Dictionary<int, int>();

    /// <summary>Cuántas cartas tiene el tablero de cada jugador (usado para validar si alguien ya completó el suyo).</summary>
    public int totalCartasTablero = 9;

    // =========================
    // EVENTOS (los escucha la UI)
    // =========================

    /// <summary>Se dispara cuando se canta una nueva carta. Manda el ID de esa carta.</summary>
    public event System.Action<int> OnCartaCantada;

    /// <summary>Se dispara cuando alguien gana la partida. Manda el nombre del ganador y si el ganador es el jugador local.</summary>
    public event System.Action<string, bool> OnLoteriaGanada;   // nombre, ¿soy yo?

    /// <summary>Se dispara cuando el jugador local pide Lotería pero es rechazado por no tener el tablero completo. Manda aciertos actuales y necesarios.</summary>
    public event System.Action<int, int> OnLoteriaRechazada;

    /// <summary>Se dispara cuando ya no quedan más cartas por cantar, sin que nadie haya ganado.</summary>
    public event System.Action OnCartasAgotadas;

    /// <summary>Lista de IDs de cartas que aún faltan por cantar, en el orden en que se van a mostrar.</summary>
    private List<int> mazoCantado = new List<int>();

    /// <summary>Indica si la partida ya terminó (por victoria o porque se acabaron las cartas).</summary>
    private bool juegoTerminado = false;

    /// <summary>ID de la carta que se está cantando actualmente (-1 si no hay ninguna).</summary>
    private int cartaActualId = -1;

    /// <summary>Permite que la UI consulte cuál es la carta actual, para comparar contra lo que el jugador toca.</summary>
    public int CartaActualId => cartaActualId;

    private Coroutine rutinaFlujoJuego;
    private bool partidaIniciada = false;

    /// <summary>
    /// Se ejecuta al cargar el script. Aquí se asegura de que solo exista
    /// una instancia de este manager en la escena (patrón singleton).
    /// </summary>
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Llamado desde PanelSalaEsperaAnfitrion cuando arranca ────

    /// <summary>
    /// Arranca la partida: prepara el mazo de cartas y comienza a cantarlas
    /// una por una. Solo funciona si quien lo llama es el MasterClient.
    /// </summary>
    public void IniciarJuego()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        juegoTerminado = false;
        cartaActualId = -1;
        PrepararMazo();
        rutinaFlujoJuego = StartCoroutine(FlujoJuego());
    }

    /// <summary>
    /// Reinicia el conteo de aciertos de todos los jugadores, limpia el
    /// registro de cartas ya cantadas guardado en las propiedades de la
    /// sala, y arma un mazo nuevo con todas las cartas de la lotería en
    /// orden aleatorio.
    /// </summary>
    void PrepararMazo()
    {
        aciertosPorJugador.Clear();

        var propsReset = new ExitGames.Client.Photon.Hashtable { { "cartasCantadas", "" } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(propsReset);

        var cache = LoteriaDataCache.Instance;
        mazoCantado.Clear();
        foreach (var id in cache.Cartas.Keys)
            mazoCantado.Add(id);
        Shuffle(mazoCantado);
    }

    /// <summary>
    /// Ciclo principal del juego (solo lo ejecuta el MasterClient): va
    /// sacando una carta a la vez del mazo, la registra en las propiedades
    /// de la sala (por si hay que retomar después), la manda a todos los
    /// jugadores vía RPC, y espera antes de pasar a la siguiente. Si se
    /// acaban las cartas sin que nadie gane, avisa a todos.
    /// </summary>
    IEnumerator FlujoJuego()
    {
        yield return new WaitForSeconds(1f);

        while (!juegoTerminado && mazoCantado.Count > 0)
        {
            int id = mazoCantado[0];
            mazoCantado.RemoveAt(0);

            GuardarCartaCantadaEnRoom(id);
            photonView.RPC("RPC_CartaCantada", RpcTarget.All, id);

            float t = tiempoEntreCartas;
            while (t > 0f && !juegoTerminado)
            {
                t -= Time.deltaTime;
                yield return null;
            }
        }

        if (!juegoTerminado)
            photonView.RPC("RPC_CartasAgotadas", RpcTarget.All);
    }

    /// <summary>
    /// Agrega el ID de la carta recién cantada a la lista de "cartas ya
    /// cantadas" guardada en las propiedades de la sala (separadas por
    /// comas). Esto sirve para que, si el MasterClient cambia a mitad de
    /// partida, el nuevo sepa exactamente qué cartas ya se cantaron.
    /// </summary>
    /// <param name="id">ID de la carta que se acaba de cantar.</param>
    void GuardarCartaCantadaEnRoom(int id)
    {
        string actual = "";
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("cartasCantadas", out object valor))
            actual = (string)valor;

        actual = string.IsNullOrEmpty(actual) ? id.ToString() : actual + "," + id;

        var props = new ExitGames.Client.Photon.Hashtable { { "cartasCantadas", actual } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    /// <summary>
    /// Le avisa al MasterClient que el jugador local acaba de acertar una
    /// casilla de su tablero. Se llama desde la UI cada vez que el jugador
    /// marca correctamente una carta.
    /// </summary>
    public void ReportarAcierto()
    {
        photonView.RPC(nameof(RPC_ReportarAcierto), RpcTarget.MasterClient,
                       PhotonNetwork.LocalPlayer.ActorNumber);
    }

    // ── RPCs ──────────────────────────────────────────────────────
    // Los RPCs son mensajes que se mandan entre los jugadores conectados
    // vía Photon, para mantener sincronizado lo que pasa en la partida.

    /// <summary>
    /// Se ejecuta en TODOS los jugadores cuando el anfitrión presiona
    /// "Iniciar Juego". Oculta las pantallas de espera, muestra la cuenta
    /// regresiva configurada en modo multijugador, y si quien lo recibe
    /// es el MasterClient, arranca el flujo real del mazo (los demás
    /// jugadores solo se quedan escuchando <see cref="OnCartaCantada"/>).
    /// </summary>
    [PunRPC]
    public void RPC_IniciarPartidaVisual()
    {
        partidaIniciada = true;
        juegoTerminado = false;
        cartaActualId = -1;

        if (panelSalaEsperaAnfitrion != null) panelSalaEsperaAnfitrion.SetActive(false);
        if (panelSalaEsperaPlayer != null) panelSalaEsperaPlayer.SetActive(false);

        if (panelCuentaRegresiva != null)
        {
            var countdown = panelCuentaRegresiva.GetComponent<CuentaRegresivaVideo>();

            if (countdown != null)
                countdown.ConfigurarModo(true);

            panelCuentaRegresiva.SetActive(true);
        }

        // Solo el Master arranca el mazo real; los demás solo se quedan escuchando OnCartaCantada
        if (PhotonNetwork.IsMasterClient)
            IniciarJuego();
    }

    /// <summary>
    /// Se ejecuta solo en el MasterClient, cuando algún jugador reporta un
    /// acierto. Suma uno al conteo de aciertos de ese jugador.
    /// </summary>
    /// <param name="actorNumber">Número de actor de Photon del jugador que acertó.</param>
    [PunRPC]
    void RPC_ReportarAcierto(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (!aciertosPorJugador.ContainsKey(actorNumber))
            aciertosPorJugador[actorNumber] = 0;

        aciertosPorJugador[actorNumber]++;

        GuardarAciertosEnRoom();
    }

    // Ahora guarda el diccionario completo de aciertos como texto en las propiedades de la sala
    void GuardarAciertosEnRoom()
    {
        var partes = new List<string>();
        foreach (var kvp in aciertosPorJugador)
        {
            partes.Add(kvp.Key + ":" + kvp.Value);
        }
        
        string serializado = string.Join(",", partes);
        
        var props = new ExitGames.Client.Photon.Hashtable { { "aciertosPorJugador", serializado } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    /// <summary>
    /// Se ejecuta solo en el MasterClient, cuando un jugador declara
    /// "¡Lotería!". Valida que ese jugador de verdad tenga todas las
    /// casillas de su tablero acertadas (según lo que el MasterClient
    /// llevaba registrado); si es así, anuncia la victoria a todos.
    /// </summary>
    /// <param name="actorNumber">Número de actor de Photon del jugador que declaró lotería.</param>
    /// <param name="nombreJugador">Nombre del jugador que declaró lotería.</param>
    /// <remarks>
    /// Si la validación falla, por ahora solo se registra una advertencia
    /// en consola del MasterClient; el jugador que hizo trampa (o tuvo un
    /// error de sincronización) no recibe ningún aviso de vuelta.
    /// </remarks>
    [PunRPC]
void RPC_PedirLoteria(int actorNumber, string nombreJugador)
    {
    if (!PhotonNetwork.IsMasterClient) return;

    int aciertos = aciertosPorJugador.TryGetValue(actorNumber, out int a) ? a : 0;

    if (aciertos < totalCartasTablero)
        {
        Debug.LogWarning($"Jugador {nombreJugador} pidió Lotería con solo {aciertos} aciertos. Rechazado.");

        // NUEVO: avisamos al jugador que lo pidió, no solo a la consola del MasterClient
        Photon.Realtime.Player jugador = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (jugador != null)
            {
            photonView.RPC(nameof(RPC_LoteriaRechazada), jugador, aciertos, totalCartasTablero);
            }

        return;
        }

    photonView.RPC("RPC_LoteriaCantada", RpcTarget.All, actorNumber, nombreJugador);
    }

    // Se ejecuta solo en el jugador que pidió Lotería sin tener el tablero completo.
    [PunRPC]
    void RPC_LoteriaRechazada(int aciertosActuales, int aciertosNecesarios)
    {
    Debug.Log($"Tu Lotería fue rechazada: tienes {aciertosActuales}/{aciertosNecesarios} aciertos.");
    OnLoteriaRechazada?.Invoke(aciertosActuales, aciertosNecesarios);
    }



    /// <summary>
    /// Se ejecuta en TODOS los jugadores cuando el MasterClient canta una
    /// nueva carta. Actualiza cuál es la carta actual y avisa a la UI
    /// mediante el evento <see cref="OnCartaCantada"/>.
    /// </summary>
    /// <param name="idCarta">ID de la carta que se acaba de cantar.</param>
    [PunRPC]
    void RPC_CartaCantada(int idCarta)
    {
        cartaActualId = idCarta;
        OnCartaCantada?.Invoke(idCarta);        // la UI se entera por evento
    }

    /// <summary>
    /// Se ejecuta en TODOS los jugadores cuando se acaban las cartas sin
    /// que nadie haya ganado. Marca la partida como terminada y avisa a
    /// la UI mediante el evento <see cref="OnCartasAgotadas"/>.
    /// </summary>
    [PunRPC]
    void RPC_CartasAgotadas()
    {
        juegoTerminado = true;
        OnCartasAgotadas?.Invoke();
    }

    /// <summary>
    /// Le pide al MasterClient que valide si el jugador local ya completó
    /// su tablero y, de ser así, lo declare ganador. Se llama desde la UI
    /// cuando el jugador presiona el botón de "¡Lotería!".
    /// </summary>
    public void PedirLoteria()
    {
        photonView.RPC("RPC_PedirLoteria", RpcTarget.MasterClient,
                       PhotonNetwork.LocalPlayer.ActorNumber,
                       PhotonNetwork.LocalPlayer.NickName);
    }

    /// <summary>
    /// Se ejecuta en TODOS los jugadores cuando el MasterClient confirma
    /// que alguien ganó. Marca la partida como terminada y avisa a la UI
    /// mediante el evento <see cref="OnLoteriaGanada"/>, indicando si el
    /// ganador es el jugador local y ahí se puede mostrar la pantalla de resultados correspondiente
    /// contador de partidas y victorias/perdidas
    /// </summary>
    /// <param name="actorNumber">Número de actor de Photon del jugador ganador.</param>
    /// <param name="nombreJugador">Nombre del jugador ganador.</param>
    [PunRPC]
    void RPC_LoteriaCantada(int actorNumber, string nombreJugador)
    {
        if (juegoTerminado) return;  // ya salimos de la sala

        juegoTerminado = true;
        bool soyYo = actorNumber == PhotonNetwork.LocalPlayer.ActorNumber;

        if (soyYo) PanelResultados.RegistrarVictoria();
        else PanelResultados.RegistrarDerrota();

        OnLoteriaGanada?.Invoke(nombreJugador, soyYo);
    }

    // ── Photon callbacks ──────────────────────────────────────────

    /// <summary>
    /// Se llama automáticamente cuando un jugador sale de la sala. Si,
    /// como resultado, este cliente se vuelve el nuevo MasterClient (por
    /// ejemplo porque el anfitrión original se desconectó) y la partida
    /// no había terminado, retoma el juego donde se quedó.
    /// </summary>
    /// <param name="otherPlayer">El jugador que salió de la sala.</param>
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        // Si el nuevo MasterClient es este cliente, retoma el flujo
        if (PhotonNetwork.IsMasterClient && partidaIniciada && !juegoTerminado && rutinaFlujoJuego == null)
        {
        Debug.Log("Retomando como nuevo MasterClient");
        RetomarComoMaster();
        }
    }

    /// <summary>Al salir de la sala, detiene el mazo y limpia el estado local.</summary>
    public override void OnLeftRoom()
    {
    StopAllCoroutines();
    rutinaFlujoJuego = null;
    partidaIniciada = false;
    juegoTerminado = true;
    cartaActualId = -1;
    mazoCantado.Clear();
    aciertosPorJugador.Clear();
    }

    /// <summary>
    /// Reconstruye el mazo restante a partir de las cartas que ya se
    /// habían cantado (guardadas en las propiedades de la sala), armando
    /// un nuevo mazo solo con las cartas que faltan, y continúa el flujo
    /// del juego desde ahí como el nuevo MasterClient.
    /// </summary>
    void RetomarComoMaster()
    {
        var cache = LoteriaDataCache.Instance;
        var yaCantadas = new HashSet<int>();

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("cartasCantadas", out object valor))
        {
            string str = (string)valor;
            if (!string.IsNullOrEmpty(str))
                foreach (var s in str.Split(','))
                    yaCantadas.Add(int.Parse(s));
        }

        // Reconstruimos los aciertos de cada jugador para evitar el bug de que no gano el jugador.
        aciertosPorJugador.Clear();
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("aciertosPorJugador", out object aciertosValor))
        {
            string str = (string)aciertosValor;
            if (!string.IsNullOrEmpty(str))
            {
                foreach (var par in str.Split(','))
                {
                    var partes = par.Split(':');
                    if (partes.Length == 2)
                    aciertosPorJugador[int.Parse(partes[0])] = int.Parse(partes[1]);
                }
            }
        }

        mazoCantado.Clear();
        foreach (var id in cache.Cartas.Keys)
            if (!yaCantadas.Contains(id))
                mazoCantado.Add(id);

        Shuffle(mazoCantado);
        rutinaFlujoJuego = StartCoroutine(FlujoJuego());
    }

    /// <summary>
    /// Revuelve una lista de números al azar (algoritmo de Fisher-Yates),
    /// usada para definir el orden en que se cantan las cartas.
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
}
