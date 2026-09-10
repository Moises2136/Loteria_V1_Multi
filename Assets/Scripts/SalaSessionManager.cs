using UnityEngine;

/// <summary>
/// Este script guarda información sobre la sala multijugador actual:
/// si este jugador es el anfitrión o solo un jugador normal, el código
/// de la sala, y cuántos jugadores hay. Es un singleton, así que solo
/// existe una instancia y se puede acceder desde cualquier parte usando
/// <c>SalaSessionManager.Instance</c>.
/// </summary>
public class SalaSessionManager : MonoBehaviour
{
    /// <summary>
    /// La única instancia de este script que existe en el juego.
    /// </summary>
    public static SalaSessionManager Instance;

    /// <summary>
    /// Los distintos roles que puede tener este jugador dentro de una sala.
    /// </summary>
    public enum RolSala
    {
        /// <summary>Todavía no está en ninguna sala.</summary>
        Ninguno,

        /// <summary>Es el jugador que creó la sala.</summary>
        Anfitrion,

        /// <summary>Es un jugador que se unió a una sala existente.</summary>
        Player
    }

    /// <summary>El rol actual de este jugador dentro de la sala.</summary>
    public RolSala rolActual = RolSala.Ninguno;

    /// <summary>Código de la sala en la que está este jugador.</summary>
    public string codigoSala = "";

    /// <summary>Cantidad de jugadores actualmente en la sala.</summary>
    public int jugadoresEnSala = 1;

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
    /// Configura a este jugador como el anfitrión de una nueva sala.
    /// </summary>
    /// <param name="codigo">Código de la sala que se acaba de crear.</param>
    public void ConfigurarComoAnfitrion(string codigo)
    {
        rolActual = RolSala.Anfitrion;
        codigoSala = codigo;
        jugadoresEnSala = 1;
    }

    /// <summary>
    /// Configura a este jugador como un jugador normal que se unió a una sala existente.
    /// </summary>
    /// <param name="codigo">Código de la sala a la que se unió.</param>
    public void ConfigurarComoPlayer(string codigo)
    {
        rolActual = RolSala.Player;
        codigoSala = codigo;
    }
}