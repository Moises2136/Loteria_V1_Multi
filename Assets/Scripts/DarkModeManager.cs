using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla el modo oscuro/claro del juego: al presionar el
/// botón de Modo Oscuro (Dark_Button) o el de Modo Claro (Light_Button),
/// cambia el color de todos los fondos, textos e imágenes registrados en
/// las listas del Inspector, y además anima con un crossfade (difuminado)
/// el cambio entre la textura negra y blanca de cada botón, para que no
/// se sienta brusco.
/// </summary>
public class DarkModeManager : MonoBehaviour
{
    /// <summary>Indica si el modo oscuro está activo actualmente.</summary>
    [Header("Modo actual")]
    [SerializeField] private bool darkMode = false;

    /// <summary>Botón (padre) que activa el modo oscuro. Ej: Dark_Button.</summary>
    [Header("Botones")]
    [SerializeField] private Button darkModeButton;

    /// <summary>Botón (padre) que activa el modo claro. Ej: Light_Button.</summary>
    [SerializeField] private Button lightModeButton;

    /// <summary>Lista de fondos/paneles cuyo color cambia según el modo.</summary>
    [Header("Fondos / Paneles")]
    [SerializeField] private List<Image> backgrounds = new List<Image>();

    /// <summary>Lista de textos cuyo color cambia según el modo.</summary>
    [Header("Textos")]
    [SerializeField] private List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();

    /// <summary>Lista de imágenes (íconos, sprites) cuyo color cambia según el modo.</summary>
    [Header("Imágenes que deben cambiar")]
    [SerializeField] private List<Image> images = new List<Image>();

    /// <summary>Color de fondo cuando el modo claro está activo.</summary>
    [Header("Colores")]
    [SerializeField] private Color lightBackground = Color.white;

    /// <summary>Color de fondo cuando el modo oscuro está activo.</summary>
    [SerializeField] private Color darkBackground = new Color(0.08f, 0.08f, 0.08f);

    /// <summary>Color de texto cuando el modo claro está activo.</summary>
    [SerializeField] private Color lightText = Color.black;

    /// <summary>Color de texto cuando el modo oscuro está activo.</summary>
    [SerializeField] private Color darkText = Color.white;

    /// <summary>
    /// Representa un botón (Light_Button o Dark_Button) que tiene dos hijos
    /// con la textura negra y blanca, entre los que se hace un crossfade
    /// al cambiar de modo.
    /// </summary>
    [System.Serializable]
    public class IconoBoton
    {
        [Tooltip("Solo para identificar este elemento en el Inspector, no afecta la lógica.")]
        public string nombre;

        [Tooltip("Image del hijo con la textura NEGRA (ej. Light_Button_Black).")]
        public Image iconoNegro;

        [Tooltip("Image del hijo con la textura BLANCA (ej. Light_Button_White).")]
        public Image iconoBlanco;
    }

    /// <summary>Los 2 botones (Light_Button y Dark_Button) que deben hacer crossfade entre su textura negra y blanca.</summary>
    [Header("Iconos de botones (crossfade Negro/Blanco)")]
    [SerializeField] private List<IconoBoton> iconosBotones = new List<IconoBoton>();

    /// <summary>Duración en segundos de la animación de difuminado.</summary>
    [Header("Animación")]
    [SerializeField] private float fadeDuration = 0.25f;

    /// <summary>Guarda la corrutina activa de cada icono, para poder cancelarla si el modo cambia rápido varias veces.</summary>
    private readonly Dictionary<IconoBoton, Coroutine> corrutinasActivas = new Dictionary<IconoBoton, Coroutine>();

    /// <summary>
    /// Al iniciar, conecta cada botón con su modo correspondiente (oscuro
    /// o claro), y aplica de inmediato (sin animar) el modo actual a todos
    /// los elementos registrados.
    /// </summary>
    private void Start()
    {
        if (darkModeButton != null)
        {
            darkModeButton.onClick.AddListener(ActivarModoOscuro);
        }
        else
        {
            Debug.LogWarning("DarkModeButton is not assigned in the inspector.");
        }

        if (lightModeButton != null)
        {
            lightModeButton.onClick.AddListener(ActivarModoClaro);
        }
        else
        {
            Debug.LogWarning("LightModeButton is not assigned in the inspector.");
        }

        // "instant: true" evita que los íconos hagan fade la primera vez que carga la escena.
        ApplyDarkMode(instant: true);
    }

    /// <summary>
    /// Activa el modo oscuro (llamado por el botón Dark_Button).
    /// </summary>
    public void ActivarModoOscuro()
    {
        SetDarkMode(true);
    }

    /// <summary>
    /// Activa el modo claro / blanco (llamado por el botón Light_Button).
    /// </summary>
    public void ActivarModoClaro()
    {
        SetDarkMode(false);
    }

    /// <summary>
    /// Cambia entre modo oscuro y modo claro, y aplica el cambio de inmediato.
    /// </summary>
    public void ToggleDarkMode()
    {
        SetDarkMode(!darkMode);
    }

    /// <summary>
    /// Establece directamente el modo oscuro como activado o desactivado,
    /// y aplica el cambio (con animación) a todos los elementos registrados.
    /// </summary>
    /// <param name="enabled">Verdadero para activar el modo oscuro, falso para el modo claro.</param>
    public void SetDarkMode(bool enabled)
    {
        darkMode = enabled;
        ApplyDarkMode();
    }

    /// <summary>
    /// Recorre las tres listas (fondos, textos, imágenes) y les aplica el
    /// color correspondiente según si el modo oscuro está activo o no.
    /// Además dispara el crossfade de las texturas de los botones.
    /// </summary>
    /// <param name="instant">Si es verdadero, aplica el cambio sin animar (usado al iniciar la escena).</param>
    private void ApplyDarkMode(bool instant = false)
    {
        // Fondos de los paneles y extras que manejen colores
        foreach (Image background in backgrounds)
        {
            if (background != null)
            {
                background.color = darkMode ? darkBackground : lightBackground;
            }
        }

        // Textos del juego para modificar su color
        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null)
            {
                text.color = darkMode ? darkText : lightText;
            }
        }

        // Imágenes agregadas como los sprites
        foreach (Image image in images)
        {
            if (image != null)
            {
                image.color = darkMode ? darkText : lightText;
            }
        }

        AnimarIconosBotones(instant);
    }

    /// <summary>
/// Para cada botón registrado en <see cref="iconosBotones"/>, decide cuál
/// textura debe entrar (aparecer) y cuál debe salir (desaparecer), y
/// lanza el crossfade correspondiente.
/// </summary>
/// <param name="instant">Si es verdadero, cambia sin animar.</param>
private void AnimarIconosBotones(bool instant)
{
    foreach (IconoBoton icono in iconosBotones)
    {
        if (icono == null) continue;

        // En modo oscuro mostramos la textura blanca (contrasta con el fondo oscuro).
        // En modo claro mostramos la textura negra.
        Image entrante = darkMode ? icono.iconoBlanco : icono.iconoNegro;
        Image saliente = darkMode ? icono.iconoNegro : icono.iconoBlanco;

        if (instant)
        {
            if (saliente != null)
            {
                saliente.gameObject.SetActive(false);
            }

            if (entrante != null)
            {
                entrante.gameObject.SetActive(true);
                Color c = entrante.color;
                c.a = 1f;
                entrante.color = c;
            }

            continue;
        }

        // Si este ícono ya estaba animando (el usuario cambió de modo muy rápido),
        // cancelamos la corrutina anterior para que no se pisen entre sí.
        if (corrutinasActivas.TryGetValue(icono, out Coroutine corrutinaAnterior) && corrutinaAnterior != null)
        {
            StopCoroutine(corrutinaAnterior);
        }

        Coroutine nuevaCorrutina = StartCoroutine(CrossfadeIcono(saliente, entrante));
        corrutinasActivas[icono] = nuevaCorrutina;
    }
}

/// <summary>
/// Hace un crossfade entre dos Image: activa el GameObject que va a
/// entrar, sube su alpha mientras baja el del que sale, y al terminar
/// desactiva el GameObject del que salió (para dejarlo tal como estaba:
/// apagado con SetActive(false)).
/// </summary>
/// <param name="saliente">Image de la textura que se está ocultando.</param>
/// <param name="entrante">Image de la textura que se está mostrando.</param>
private IEnumerator CrossfadeIcono(Image saliente, Image entrante)
{
    // Activamos el que entra ANTES de animar, si no, su fade no se vería.
    if (entrante != null)
    {
        entrante.gameObject.SetActive(true);
        Color cEntranteInicial = entrante.color;
        cEntranteInicial.a = 0f;
        entrante.color = cEntranteInicial;
    }

    float tiempoTranscurrido = 0f;
    float alphaInicialSaliente = saliente != null ? saliente.color.a : 0f;

    while (tiempoTranscurrido < fadeDuration)
    {
        tiempoTranscurrido += Time.deltaTime;
        float progreso = Mathf.Clamp01(tiempoTranscurrido / fadeDuration);

        if (saliente != null)
        {
            Color c = saliente.color;
            c.a = Mathf.Lerp(alphaInicialSaliente, 0f, progreso);
            saliente.color = c;
        }

        if (entrante != null)
        {
            Color c = entrante.color;
            c.a = Mathf.Lerp(0f, 1f, progreso);
            entrante.color = c;
        }

        yield return null;
    }

    // Al terminar, dejamos el saliente apagado (SetActive false) como estaba antes,
    // y el entrante bien encendido con alpha 1.
    if (saliente != null)
    {
        Color c = saliente.color;
        c.a = 0f;
        saliente.color = c;
        saliente.gameObject.SetActive(false);
    }

    if (entrante != null)
    {
        Color c = entrante.color;
        c.a = 1f;
        entrante.color = c;
    }
}
}