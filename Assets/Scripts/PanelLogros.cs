using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla el panel de logros del juego: muestra cuáles logros
/// ya están desbloqueados y cuáles siguen bloqueados, usando los datos
/// guardados en PlayerPrefs. También ofrece funciones para desbloquear
/// cada logro desde cualquier parte del juego.
/// </summary>
public class PanelLogros : MonoBehaviour
{
    // =========================
    // LOGROS
    // =========================

    /// <summary>Objeto visual (ícono) del logro de completar una lotería.</summary>
    [Header("Logros")]
    public GameObject logroLoteria;

    /// <summary>Objeto visual (ícono) del logro botánico.</summary>
    public GameObject logroBotanico;

    // =========================
    // TEXTO ESTADO
    // =========================

    /// <summary>Texto que indica si el logro de lotería está desbloqueado o bloqueado.</summary>
    [Header("Texto estado (opcional)")]
    public TMP_Text textoEstadoLoteria;

    /// <summary>Texto que indica si el logro botánico está desbloqueado o bloqueado.</summary>
    public TMP_Text textoEstadoBotanico;

    // =========================
    // NAVEGACIÓN
    // =========================

    /// <summary>Botón para regresar al panel anterior.</summary>
    [Header("Botón regresar")]
    public Button botonRegresar;

    /// <summary>Este mismo panel de logros.</summary>
    public GameObject panelActual;

    /// <summary>Panel al que se regresa al presionar el botón.</summary>
    public GameObject panelAnterior;

    // =========================
    // CLAVES PLAYERPREFS
    // =========================

    /// <summary>Llave para guardar si el logro de lotería está desbloqueado.</summary>
    private const string KEY_LOGRO_LOTERIA = "logro_loteria";

    /// <summary>Llave para guardar si el logro botánico está desbloqueado.</summary>
    private const string KEY_LOGRO_BOTANICO = "logro_botanico";

    // =========================
    // INICIALIZACIÓN
    // =========================

    /// <summary>
    /// Al iniciar, conecta el botón de regresar con su función correspondiente.
    /// </summary>
    void Start()
    {
        ConfigurarBoton();
    }

    /// <summary>
    /// Cada vez que se activa este panel, recarga el estado de los logros.
    /// </summary>
    void OnEnable()
    {
        CargarLogros();
    }

    /// <summary>
    /// Conecta el botón de regresar con su función.
    /// </summary>
    void ConfigurarBoton()
    {
        if (botonRegresar != null)
        {
            botonRegresar.onClick.RemoveAllListeners();
            botonRegresar.onClick.AddListener(Regresar);
        }
    }

    // =========================
    // CARGA DE LOGROS
    // =========================

    /// <summary>
    /// Lee de PlayerPrefs si cada logro está desbloqueado o no,
    /// y actualiza cómo se ve cada uno en pantalla.
    /// </summary>
    void CargarLogros()
    {
        bool logroLoteriaActivo = PlayerPrefs.GetInt(KEY_LOGRO_LOTERIA, 0) == 1;
        bool logroBotanicoActivo = PlayerPrefs.GetInt(KEY_LOGRO_BOTANICO, 0) == 1;

        ActualizarLogro(logroLoteria, textoEstadoLoteria, logroLoteriaActivo, "Logro desbloqueado");
        ActualizarLogro(logroBotanico, textoEstadoBotanico, logroBotanicoActivo, "Logro desbloqueado");
    }

    /// <summary>
    /// Actualiza cómo se ve un logro en pantalla según si ya está desbloqueado.
    /// Si está desbloqueado, se ve a color; si está bloqueado, se ve gris.
    /// </summary>
    /// <param name="logro">Objeto visual del logro (su ícono).</param>
    /// <param name="texto">Texto que muestra el estado del logro.</param>
    /// <param name="activo">Verdadero si el logro ya está desbloqueado.</param>
    /// <param name="mensaje">Mensaje a mostrar cuando el logro está desbloqueado.</param>
    void ActualizarLogro(GameObject logro, TMP_Text texto, bool activo, string mensaje)
    {
        if (logro != null)
        {
            Image img = logro.GetComponent<Image>();

            if (img != null)
            {
                // Blanco = desbloqueado, gris = bloqueado
                if (activo)
                    img.color = Color.white;
                else
                    img.color = new Color(0.7f, 0.7f, 0.7f);
            }
        }

        if (texto != null)
        {
            texto.text = activo ? mensaje : "Bloqueado";
        }
    }

    // =========================
    // NAVEGACIÓN
    // =========================

    /// <summary>
    /// Cierra este panel y regresa al panel anterior.
    /// </summary>
    public void Regresar()
    {
        if (panelActual != null)
            panelActual.SetActive(false);

        if (panelAnterior != null)
            panelAnterior.SetActive(true);
    }

    // =========================
    // DESBLOQUEO DE LOGROS
    // =========================

    /// <summary>
    /// Marca el logro de lotería como desbloqueado y lo guarda de forma permanente.
    /// </summary>
    /// <remarks>
    /// Es un método <c>static</c>, así que se puede llamar desde cualquier parte
    /// del juego (por ejemplo, desde el script que detecta cuándo se completa una
    /// partida) sin necesitar tener este panel abierto en pantalla — igual que
    /// <c>RegistrarVictoria</c> en <c>PanelResultados</c>.
    /// </remarks>
    public static void DesbloquearLogroLoteria()
    {
        PlayerPrefs.SetInt(KEY_LOGRO_LOTERIA, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Marca el logro botánico como desbloqueado y lo guarda de forma permanente.
    /// </summary>
    /// <remarks>
    /// Igual que <see cref="DesbloquearLogroLoteria"/>, es <c>static</c> y se
    /// puede llamar desde cualquier parte del juego.
    /// </remarks>
    public static void DesbloquearLogroBotanico()
    {
        PlayerPrefs.SetInt(KEY_LOGRO_BOTANICO, 1);
        PlayerPrefs.Save();
    }
}