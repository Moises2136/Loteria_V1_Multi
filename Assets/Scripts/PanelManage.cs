using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Navegación de los paneles del menú lateral (perfil, configuración,
/// clasificación, amigos, idioma, logros, créditos). Solo deja un panel
/// activo a la vez. Aquí viven los botones que abren cada panel y los
/// botones de regresar (casitas), así cada panel solo tiene su propia lógica.
/// Es un singleton: <c>ScreensManage.Instance</c>.
/// </summary>
public class ScreensManage : MonoBehaviour
{
    public static ScreensManage Instance { get; private set; }

    // =========================
    // PANELES
    // =========================

    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelPerfil;
    public GameObject panelConfig;
    public GameObject panelResultadosRatio;
    public GameObject panelAmigos;
    public GameObject panelIdioma;
    public GameObject panelLogros;
    public GameObject panelCreditos;

    // =========================
    // BOTONES DEL MENÚ LATERAL
    // =========================

    [Header("Botones del menú lateral")]
    public Button btnPerfil;
    public Button btnConfig;
    public Button btnClasificacion;
    public Button btnAmigos;
    public Button btnIdioma;
    public Button btnLogros;
    public Button btnCreditos;

    // =========================
    // BOTONES DE REGRESAR
    // =========================

    /// <summary>Casitas de cada panel. Todas regresan al menú principal.</summary>
    [Header("Botones de regresar (todos vuelven al menú)")]
    public Button[] botonesRegresar;

    /// <summary>Todos los paneles, para ocultarlos de golpe.</summary>
    private GameObject[] menuPanels;

    // =========================
    // INICIALIZACIÓN
    // =========================

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        menuPanels = new GameObject[]
        {
            panelMenu,
            panelPerfil,
            panelConfig,
            panelResultadosRatio,
            panelAmigos,
            panelIdioma,
            panelLogros,
            panelCreditos
        };
    }

    /// <summary>Conecta cada botón con el panel que debe abrir.</summary>
    void Start()
    {
        Conectar(btnPerfil, panelPerfil);
        Conectar(btnConfig, panelConfig);
        Conectar(btnClasificacion, panelResultadosRatio);
        Conectar(btnAmigos, panelAmigos);
        Conectar(btnIdioma, panelIdioma);
        Conectar(btnLogros, panelLogros);
        Conectar(btnCreditos, panelCreditos);

        if (botonesRegresar != null)
        {
            foreach (Button b in botonesRegresar)
                Conectar(b, panelMenu);
        }
    }

    /// <summary>Hace que un botón muestre un panel al presionarlo.</summary>
    /// <param name="boton">Botón a conectar.</param>
    /// <param name="destino">Panel que se muestra.</param>
    void Conectar(Button boton, GameObject destino)
    {
        if (boton == null) return;

        if (destino == null)
        {
            Debug.LogWarning($"ScreensManage: el botón '{boton.name}' no tiene panel destino asignado.");
            return;
        }

        boton.onClick.AddListener(() => ShowPanel(destino));
    }

    // =========================
    // NAVEGACIÓN
    // =========================

    /// <summary>
    /// Muestra el panel indicado y oculta todos los demás.
    /// Si el panel es null o no está registrado, no cambia nada y avisa en consola
    /// (así nunca se queda la pantalla en blanco).
    /// </summary>
    /// <param name="panelToShow">Panel a mostrar.</param>
    public void ShowPanel(GameObject panelToShow)
    {
        if (panelToShow == null)
        {
            Debug.LogWarning("ScreensManage.ShowPanel: panel nulo. No se cambió nada.");
            return;
        }

        if (System.Array.IndexOf(menuPanels, panelToShow) < 0)
        {
            Debug.LogWarning($"ScreensManage.ShowPanel: '{panelToShow.name}' no está en la lista de paneles. No se cambió nada.");
            return;
        }

        AplicarVisibilidad(panelToShow);
    }

    /// <summary>Muestra el menú principal.</summary>
    public void ShowMenu() => ShowPanel(panelMenu);

    /// <summary>Oculta todos los paneles (por ejemplo, al cerrar sesión).</summary>
    public void OcultarTodos() => AplicarVisibilidad(null);

    /// <summary>Prende solo el panel indicado y apaga los demás (null = apagar todos).</summary>
    void AplicarVisibilidad(GameObject panelToShow)
    {
        foreach (GameObject panel in menuPanels)
        {
            if (panel != null)
                panel.SetActive(panel == panelToShow);
        }
    }
}