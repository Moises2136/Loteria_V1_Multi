using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla el panel de resultados generales del jugador:
/// cuántas partidas ha jugado, ganado, perdido, su porcentaje de victorias,
/// y cuál fue el resultado de la última partida. También se encarga de
/// guardar esas estadísticas cada vez que el jugador gana o pierde.
/// </summary>
public class PanelResultados : MonoBehaviour
{
    // =========================
    // TEXTOS
    // =========================

    /// <summary>Texto que muestra el total de partidas jugadas.</summary>
    [Header("Textos")]
    public TMP_Text txtPartidasJugadas;

    /// <summary>Texto que muestra el número de partidas ganadas.</summary>
    public TMP_Text txtGanadas;

    /// <summary>Texto que muestra el número de partidas perdidas.</summary>
    public TMP_Text txtPerdidas;

    /// <summary>Texto que muestra el porcentaje de victorias.</summary>
    public TMP_Text txtRatio;

    /// <summary>Texto que muestra el resultado de la última partida jugada.</summary>
    public TMP_Text txtUltimoResultado;

    // =========================
    // 🔙 NAVEGACIÓN
    // =========================

    /// <summary>Botón para regresar al panel anterior.</summary>
    [Header("Boton regresar")]
    public Button btnRegresar;

    /// <summary>Este mismo panel de resultados.</summary>
    public GameObject panelResultados;

    /// <summary>Panel al que se regresa al presionar el botón.</summary>
    public GameObject panelAnterior;

    // =========================
    // CLAVES PLAYERPREFS
    // =========================
    // Estas llaves se usan para guardar y leer las estadísticas en PlayerPrefs.

    /// <summary>Llave para guardar el número de partidas ganadas.</summary>
    private const string KEY_GANADAS = "stats_ganadas";

    /// <summary>Llave para guardar el número de partidas perdidas.</summary>
    private const string KEY_PERDIDAS = "stats_perdidas";

    /// <summary>Llave para guardar el resultado de la última partida.</summary>
    private const string KEY_ULTIMO = "stats_ultimo_resultado";

    // =========================
    // INICIALIZACIÓN
    // =========================

    /// <summary>
    /// Al iniciar, conecta el botón de regresar con su función correspondiente.
    /// </summary>
    void Start()
    {
        if (btnRegresar != null)
        {
            btnRegresar.onClick.RemoveAllListeners();
            btnRegresar.onClick.AddListener(Regresar);
        }
    }

    /// <summary>
    /// Cada vez que se activa este panel, recarga las estadísticas más recientes.
    /// </summary>
    void OnEnable()
    {
        CargarResultados();
    }

    // =========================
    // CARGA DE DATOS
    // =========================

    /// <summary>
    /// Lee las estadísticas guardadas en PlayerPrefs (partidas ganadas y perdidas),
    /// calcula el porcentaje de victorias, y actualiza todos los textos en pantalla.
    /// </summary>
    public void CargarResultados()
    {
        int ganadas = PlayerPrefs.GetInt(KEY_GANADAS, 0);
        int perdidas = PlayerPrefs.GetInt(KEY_PERDIDAS, 0);
        int jugadas = ganadas + perdidas;

        // Cálculo de porcentaje de victorias
        float ratio = 0f;
        if (jugadas > 0)
            ratio = ((float)ganadas / jugadas) * 100f;

        if (txtPartidasJugadas != null)
            txtPartidasJugadas.text = jugadas.ToString();

        if (txtGanadas != null)
            txtGanadas.text = ganadas.ToString();

        if (txtPerdidas != null)
            txtPerdidas.text = perdidas.ToString();

        if (txtRatio != null)
            txtRatio.text = ratio.ToString("F1") + "%";

        if (txtUltimoResultado != null)
            txtUltimoResultado.text = "Último resultado: " +
                PlayerPrefs.GetString(KEY_ULTIMO, "Sin partidas");
    }

    // =========================
    // REGISTRO DE RESULTADOS
    // =========================

    /// <summary>
    /// Suma una victoria más a las estadísticas guardadas y actualiza
    /// el último resultado a "Ganaste".
    /// </summary>
    /// <remarks>
    /// Es un método <c>static</c>, así que se puede llamar desde cualquier
    /// parte del juego (por ejemplo, desde el script que detecta cuándo
    /// termina una partida) sin necesidad de tener este panel abierto en pantalla.
    /// </remarks>
    public static void RegistrarVictoria()
    {
        int ganadas = PlayerPrefs.GetInt(KEY_GANADAS, 0);
        ganadas++;

        PlayerPrefs.SetInt(KEY_GANADAS, ganadas);
        PlayerPrefs.SetString(KEY_ULTIMO, "Ganaste");
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Suma una derrota más a las estadísticas guardadas y actualiza
    /// el último resultado a "Perdiste".
    /// </summary>
    /// <remarks>
    /// Igual que <see cref="RegistrarVictoria"/>, es <c>static</c> y se
    /// puede llamar desde cualquier parte del juego.
    /// </remarks>
    public static void RegistrarDerrota()
    {
        int perdidas = PlayerPrefs.GetInt(KEY_PERDIDAS, 0);
        perdidas++;

        PlayerPrefs.SetInt(KEY_PERDIDAS, perdidas);
        PlayerPrefs.SetString(KEY_ULTIMO, "Perdiste");
        PlayerPrefs.Save();
    }

    // =========================
    // NAVEGACIÓN
    // =========================

    /// <summary>
    /// Cierra el panel de resultados y regresa al panel anterior.
    /// </summary>
    public void Regresar()
    {
        if (panelResultados != null)
            panelResultados.SetActive(false);

        if (panelAnterior != null)
            panelAnterior.SetActive(true);
    }
}