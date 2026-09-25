using UnityEngine;

/// <summary>
/// Este script se encarga de la navegación general entre los paneles del menú
/// (perfil, configuración, resultados, amigos, logros, créditos, etc). Solo
/// deja un panel activo a la vez: al mostrar uno, automáticamente oculta
/// todos los demás. Es un singleton, así que solo existe una instancia y se
/// puede acceder desde cualquier parte usando <c>ScreensManage.Instance</c>.
/// </summary>
public class ScreensManage : MonoBehaviour
{
    /// <summary>
    /// La única instancia de este script que existe en el juego.
    /// </summary>
    public static ScreensManage Instance { get; private set; }

    // =========================
    // PANELES DE MENÚ
    // =========================

    /// <summary>Panel del menú principal.</summary>
    [Header("Paneles de menú (navegación general)")]
    public GameObject panelMenu;

    /// <summary>Panel de perfil del usuario.</summary>
    public GameObject panelPerfil;

    /// <summary>Panel de configuración.</summary>
    public GameObject panelConfig;

    /// <summary>Panel de resultados/estadísticas del jugador.</summary>
    public GameObject panelResultadosRatio;

    /// <summary>Panel de amigos.</summary>
    public GameObject panelAmigos;

    /// <summary>Panel de logros.</summary>
    public GameObject panelLogros;

    /// <summary>Panel de créditos.</summary>
    public GameObject panelCreditos;

    /// <summary>Botones de navegación general.</summary>
    [Header("Botones navegación general")]

    /// <summary>Botón para ir al menú principal.</summary>
    public GameObject btnMenu;

    /// <summary>Botón para ir al panel de perfil.</summary>   
    public GameObject btnPerfil;

    /// <summary> Botón para ir al panel de configuración </summary>
    public GameObject btnConfig;

    /// <summary> Botón para ir al panel de resultados/estadísticas </summary>
    public GameObject btnResultadosRatio;

    /// <summary> Botón para ir al panel de amigos </summary>
    public GameObject btnAmigos;

    /// <summary> Botón para ir al panel de logros </summary>
    public GameObject btnLogros;

    /// <summary> Botón para ir al panel de créditos </summary>
    public GameObject btnCreditos;

    /// <summary> Botón para cerrar sesión </summary>
    public GameObject btnCerrarSesion;

    /// <summary> Botón para salir del juego </summary>
    public GameObject btnSalir;
    

    /// <summary>
    /// Lista con todos los paneles de menú, usada internamente para
    /// poder ocultarlos todos de golpe y luego mostrar solo el que corresponde
    /// </summary>
    private GameObject[] menuPanels;

    /// <summary>
    /// Lista con todos los botones de flujo de navegación, usada internamente para
    /// poder desactivarlos todos de golpe y luego activar solo el que corresponde
    /// </summary>
    private GameObject[] botonesFlujo;

    /// <summary>
    /// Se ejecuta al cargar el script. Aquí se asegura de que solo exista
    /// una instancia de este manager en todo el juego (patrón singleton),
    /// y arma la lista interna con todos los paneles de menú.
    /// </summary>
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        menuPanels = new GameObject[]
        {
            panelMenu,
            panelPerfil,
            panelConfig,
            panelResultadosRatio,
            panelAmigos,
            panelLogros,
            panelCreditos,
        };
    }

    /// <summary>
    /// Muestra el panel indicado y oculta automáticamente todos los demás
    /// paneles de menú registrados.
    /// </summary>
    /// <param name="panelToShow">El panel que se quiere mostrar.</param>
    public void ShowPanel(GameObject panelToShow)
    {
        foreach (var panel in menuPanels)
        {
            if (panel != null)
                panel.SetActive(panel == panelToShow);
        }
    }

    /// <summary>
    /// Muestra directamente el panel del menú principal
    /// (equivale a llamar <see cref="ShowPanel"/> con <see cref="panelMenu"/>).
    /// </summary>
    public void ShowMenu()
    {
        ShowPanel(panelMenu);
    }
}