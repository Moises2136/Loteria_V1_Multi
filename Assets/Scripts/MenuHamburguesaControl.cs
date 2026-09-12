using UnityEngine;
using Firebase.Auth;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Este script controla el menú tipo "hamburguesa" (el panel lateral que se
/// abre y cierra deslizándose): maneja su animación de apertura/cierre,
/// la navegación hacia los distintos paneles de contenido (perfil,
/// configuración, historial, etc), el fondo oscuro que aparece detrás,
/// y el cierre de sesión visual.
/// </summary>
public class MenuHamburguesaControl : MonoBehaviour
{
    // =========================
    // BOTÓN PRINCIPAL
    // =========================

    /// <summary>Botón que abre o cierra el menú lateral.</summary>
    public Button btnAbrirMenu;

    // =========================
    // BOTONES DEL MENÚ
    // =========================

    /// <summary>Botón para abrir el panel de perfil.</summary>
    public Button btnPerfil;

    /// <summary>Botón para abrir el panel de configuración.</summary>
    public Button btnConfiguracion;

    /// <summary>Botón para abrir el panel de historial.</summary>
    public Button btnHistorial;

    /// <summary>Botón para abrir el panel de clasificación (ranking).</summary>
    public Button btnClasificacion;

    /// <summary>Botón para abrir el panel de idioma.</summary>
    public Button btnIdioma;

    /// <summary>Botón para abrir el panel de créditos.</summary>
    public Button btnCreditos;

    /// <summary>Botón para abrir un panel de créditos alterno/final.</summary>
    public Button btnCreditosFinal;

    /// <summary>Botón para cerrar sesión.</summary>
    public Button btnCerrarSesion;

    // =========================
    // PANELES DE CONTENIDO
    // =========================

    /// <summary>Panel de perfil del usuario.</summary>
    public GameObject panelPerfil;

    /// <summary>Panel de configuración.</summary>
    public GameObject panelConfiguracion;

    /// <summary>Panel de historial.</summary>
    public GameObject panelHistorial;

    /// <summary>Panel de selección de idioma.</summary>
    public GameObject panelIdioma;

    /// <summary>Panel de clasificación (ranking).</summary>
    public GameObject panelClasificacion;

    /// <summary>Panel de créditos.</summary>
    public GameObject panelCreditos;

    /// <summary>Panel de créditos alterno/final.</summary>
    public GameObject panelCreditosFinal;

    // =========================
    // PANELES PRINCIPALES
    // =========================

    /// <summary>Panel del menú principal del juego.</summary>
    public GameObject panelMenuPrincipal;

    /// <summary>Panel de inicio de sesión, al que se regresa al cerrar sesión.</summary>
    public GameObject panelInicioSesion;

    // =========================
    // MENÚ Y FONDO
    // =========================

    /// <summary>El RectTransform del panel lateral que se desliza al abrir/cerrar el menú.</summary>
    public RectTransform panelSalaOpciones;

    /// <summary>Fondo oscuro que aparece detrás del menú y bloquea la interacción con el resto de la pantalla.</summary>
    public GameObject panelFondo;

    /// <summary>Botón invisible sobre el fondo, usado para cerrar el menú si el jugador toca fuera de él.</summary>
    public Button btnFondoCerrar;

    // =========================
    // POSICIONES
    // =========================

    /// <summary>Posición del panel lateral cuando el menú está cerrado (fuera de pantalla).</summary>
    public Vector2 posicionCerrado;

    /// <summary>Posición del panel lateral cuando el menú está totalmente abierto.</summary>
    public Vector2 posicionAbierto;

    // =========================
    // ANIMACIÓN
    // =========================

    /// <summary>Qué tan rápido se desliza el menú al abrirse o cerrarse.</summary>
    public float velocidad = 8f;

    // =========================
    // ESTADO
    // =========================

    /// <summary>Indica si el menú está actualmente abierto.</summary>
    [SerializeField] private bool menuAbierto = false;

    /// <summary>Lista con todos los paneles de contenido del menú, usada para cerrarlos todos de golpe.</summary>
    private GameObject[] allPanels;

    /// <summary>Referencia a la animación de apertura/cierre en curso, para poder detenerla si hace falta.</summary>
    private Coroutine animacionActual;

    /// <summary>
    /// Permite consultar desde fuera si el menú está abierto en este momento.
    /// </summary>
    public bool EstaAbierto => menuAbierto;

    // =========================
    // INICIALIZACIÓN
    // =========================

    /// <summary>
    /// Al cargar el script, arma la lista interna de paneles y deja el
    /// menú en su estado cerrado desde el principio.
    /// </summary>
    void Awake()
    {
        InicializarReferencias();
        ForzarEstadoInicial();
    }

    /// <summary>
    /// Al iniciar, conecta todos los botones del menú con sus funciones.
    /// </summary>
    void Start()
    {
        AsignarListeners();
        Debug.Log("Estado inicial menú: " + ObtenerEstadoMenu());
    }

    /// <summary>
    /// Cada vez que este objeto se activa, se asegura de que el menú
    /// empiece cerrado (sin animación), por si quedó en un estado raro
    /// de una activación anterior.
    /// </summary>
    void OnEnable()
    {
        ForzarEstadoInicial();
    }

    /// <summary>
    /// Arma la lista con todos los paneles de contenido del menú (perfil,
    /// configuración, historial, idioma, clasificación, créditos), para
    /// poder cerrarlos todos juntos al abrir uno nuevo.
    /// </summary>
    void InicializarReferencias()
    {
        allPanels = new GameObject[]
        {
            panelPerfil,
            panelConfiguracion,
            panelHistorial,
            panelIdioma,
            panelClasificacion,
            panelCreditos
        };
    }

    /// <summary>
    /// Conecta el botón principal (abrir/cerrar menú), cada botón de
    /// navegación con su panel correspondiente, el botón de cerrar sesión,
    /// y el botón invisible del fondo.
    /// </summary>
    void AsignarListeners()
    {
        if (btnAbrirMenu != null)
        {
            btnAbrirMenu.onClick.RemoveAllListeners();
            btnAbrirMenu.onClick.AddListener(ToggleMenu);
        }

        if (btnPerfil != null)
            btnPerfil.onClick.AddListener(() => AbrirPanelDesdeMenu(panelPerfil));

        if (btnConfiguracion != null)
            btnConfiguracion.onClick.AddListener(() => AbrirPanelDesdeMenu(panelConfiguracion));

        if (btnHistorial != null)
            btnHistorial.onClick.AddListener(() => AbrirPanelDesdeMenu(panelHistorial));

        if (btnClasificacion != null)
            btnClasificacion.onClick.AddListener(() => AbrirPanelDesdeMenu(panelClasificacion));

        if (btnCreditos != null)
            btnCreditos.onClick.AddListener(() => AbrirPanelDesdeMenu(panelCreditos));

        if (btnIdioma != null)
            btnIdioma.onClick.AddListener(() => AbrirPanelDesdeMenu(panelIdioma));

        if (btnCreditosFinal != null)
            btnCreditosFinal.onClick.AddListener(() => AbrirPanelDesdeMenu(panelCreditosFinal));

        if (btnCerrarSesion != null)
            btnCerrarSesion.onClick.AddListener(CerrarSesion);

        if (btnFondoCerrar != null)
            btnFondoCerrar.onClick.AddListener(CerrarMenu);
    }

    /// <summary>
    /// Deja el menú de golpe en su posición cerrada, sin animación, y
    /// detiene cualquier animación que estuviera en curso.
    /// </summary>
    void ForzarEstadoInicial()
    {
        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
            animacionActual = null;
        }

        if (panelSalaOpciones != null)
            panelSalaOpciones.anchoredPosition = posicionCerrado;

        if (panelFondo != null)
            panelFondo.SetActive(false);

        menuAbierto = false;
    }

    // =========================
    // ESTADO
    // =========================

    /// <summary>
    /// Devuelve el estado actual del menú como texto, útil para depuración.
    /// </summary>
    /// <returns>"ABIERTO" o "CERRADO", según el estado actual.</returns>
    public string ObtenerEstadoMenu()
    {
        return menuAbierto ? "ABIERTO" : "CERRADO";
    }

    // =========================
    // CONTROL MENÚ
    // =========================

    /// <summary>
    /// Abre el menú si está cerrado, o lo cierra si está abierto.
    /// </summary>
    public void ToggleMenu()
    {
        if (menuAbierto)
            CerrarMenu();
        else
            AbrirMenu();
    }

    /// <summary>
    /// Muestra el fondo oscuro y desliza el menú hacia su posición abierta.
    /// </summary>
    public void AbrirMenu()
    {
        if (panelSalaOpciones == null) return;

        if (panelFondo != null)
            panelFondo.SetActive(true);

        if (animacionActual != null)
            StopCoroutine(animacionActual);

        animacionActual = StartCoroutine(MoverMenu(posicionAbierto));
        menuAbierto = true;
    }

    /// <summary>
    /// Inicia la animación de cierre del menú.
    /// </summary>
    public void CerrarMenu()
    {
        if (panelSalaOpciones == null) return;

        if (animacionActual != null)
            StopCoroutine(animacionActual);

        animacionActual = StartCoroutine(CerrarMenuCoroutine());
    }

    /// <summary>
    /// Desliza el menú hacia su posición cerrada y, una vez ahí,
    /// oculta el fondo oscuro y marca el menú como cerrado.
    /// </summary>
    IEnumerator CerrarMenuCoroutine()
    {
        yield return StartCoroutine(MoverMenu(posicionCerrado));

        if (panelFondo != null)
            panelFondo.SetActive(false);

        menuAbierto = false;
        animacionActual = null;
    }

    /// <summary>
    /// Desliza suavemente el panel lateral hacia la posición indicada,
    /// frame a frame, hasta llegar (casi) exactamente a ella.
    /// </summary>
    /// <param name="destino">La posición a la que se quiere mover el menú.</param>
    IEnumerator MoverMenu(Vector2 destino)
    {
        if (panelSalaOpciones == null) yield break;

        while (Vector2.Distance(panelSalaOpciones.anchoredPosition, destino) > 0.1f)
        {
            panelSalaOpciones.anchoredPosition = Vector2.Lerp(
                panelSalaOpciones.anchoredPosition,
                destino,
                velocidad * Time.deltaTime
            );

            yield return null;
        }

        panelSalaOpciones.anchoredPosition = destino;
    }

    // =========================
    // NAVEGACIÓN
    // =========================

    /// <summary>
    /// Cierra todos los paneles de contenido del menú, abre únicamente
    /// el panel indicado, y cierra el menú lateral.
    /// </summary>
    /// <param name="panelToOpen">El panel que se quiere mostrar.</param>
    void AbrirPanelDesdeMenu(GameObject panelToOpen)
    {
        foreach (GameObject panel in allPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        if (panelToOpen != null)
            panelToOpen.SetActive(true);

        CerrarMenu();
    }

    /// <summary>
    /// Cierra todos los paneles de contenido, oculta el menú principal,
    /// muestra la pantalla de inicio de sesión, cierra el menú lateral y cierra la sesión de Firebase.
    /// </summary>
    void CerrarSesion()
    {
    // NUEVO: cerramos la sesión real de Firebase, y limpiamos los datos locales
    FirebaseAuth.DefaultInstance.SignOut();

    PlayerPrefs.DeleteKey("nombreCompleto");
    PlayerPrefs.DeleteKey("usuario");
    PlayerPrefs.DeleteKey("correo");
    PlayerPrefs.DeleteKey("uid");
    PlayerPrefs.Save();

    foreach (GameObject panel in allPanels)
    {
        if (panel != null)
            panel.SetActive(false);
    }

    if (panelMenuPrincipal != null)
        panelMenuPrincipal.SetActive(false);

    if (panelInicioSesion != null)
        panelInicioSesion.SetActive(true);

    Debug.Log("Sesión cerrada");
    }
}