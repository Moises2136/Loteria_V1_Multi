using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;

/// <summary>
/// Este script controla la pantalla de resultados al terminar una partida.
/// Muestra el nombre del ganador y permite regresar al menú principal.
/// Por defecto asume que el jugador local es el ganador (modo Solo),
/// pero en modo Multijugador se puede sobrescribir con el nombre real
/// del ganador usando <see cref="MostrarGanador"/>.
/// </summary>
public class PanelResultadosPartidaUI : MonoBehaviour
{
    /// <summary>Texto que muestra el nombre del ganador.</summary>
    [Header("Textos")]
    public TMP_Text txtWinnerName;

    /// <summary>Botón para continuar y regresar al menú principal.</summary>
    [Header("Botón")]
    public Button btnContinuar;

    /// <summary>Este mismo panel de resultados.</summary>
    [Header("Paneles")]
    public GameObject panelResultadosPartida;

    /// <summary>Panel del menú principal, al que se regresa al presionar "Continuar".</summary>
    public GameObject panelMenu;

    /// <summary>
    /// Al iniciar, conecta el botón de continuar con su función correspondiente.
    /// </summary>
    private void Start()
    {
        if (btnContinuar != null)
        {
            btnContinuar.onClick.RemoveAllListeners();
            btnContinuar.onClick.AddListener(ContinuarAlMenu);
        }
    }

    /// <summary>
    /// Cada vez que se activa este panel, carga un nombre por defecto
    /// (el del jugador local). Es correcto para el modo Solo; en modo
    /// Multijugador, este valor se sobrescribe después con el nombre
    /// real del ganador.
    /// </summary>
    private void OnEnable()
    {
        CargarNombreGanador();
    }

    /// <summary>
    /// Busca el nombre del jugador local para mostrarlo como ganador por defecto.
    /// Primero revisa los datos guardados localmente (PlayerPrefs); si no
    /// encuentra nada, intenta usar el nombre o correo de Firebase; y si
    /// tampoco hay nada, usa "Jugador" como último recurso.
    /// </summary>
    private void CargarNombreGanador()
    {
        string nombre = PlayerPrefs.GetString("usuario", "");

        if (string.IsNullOrWhiteSpace(nombre))
            nombre = PlayerPrefs.GetString("nombreCompleto", "");

        if (string.IsNullOrWhiteSpace(nombre))
        {
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

            if (user != null)
            {
                if (!string.IsNullOrWhiteSpace(user.DisplayName))
                    nombre = user.DisplayName;
                else if (!string.IsNullOrWhiteSpace(user.Email))
                    nombre = user.Email;
            }
        }

        if (string.IsNullOrWhiteSpace(nombre))
            nombre = "Jugador";

        if (txtWinnerName != null)
            txtWinnerName.text = nombre;
    }

    /// <summary>
    /// Muestra un nombre de ganador específico, sobrescribiendo el valor
    /// por defecto. Se usa en modo Multijugador, donde el ganador real
    /// puede ser un jugador distinto al local.
    /// </summary>
    /// <param name="nombreGanador">Nombre del jugador que ganó la partida.</param>
    public void MostrarGanador(string nombreGanador)
    {
    if (txtWinnerName != null)
        txtWinnerName.text = nombreGanador;
        }

    /// <summary>
    /// Cierra el panel de resultados y regresa al menú principal.
    /// </summary>
    public void ContinuarAlMenu()
    {
        if (panelResultadosPartida != null)
            panelResultadosPartida.SetActive(false);

        if (panelMenu != null)
            panelMenu.SetActive(true);
    }
}