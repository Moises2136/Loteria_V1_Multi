using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla el modo oscuro/claro del juego: al presionar el botón de Modo Oscuro
/// (Dark_Button) o el de Modo Claro (Light_Button), cambia el color de los fondos,
/// textos, imágenes, texto escrito en inputs, placeholders, sombras y tintes de
/// selectables registrados en las listas del Inspector, cambia el sprite blanco/negro
/// de las imágenes registradas, y anima con un crossfade el cambio entre la textura
/// negra y blanca de cada botón.
/// <para>
/// La primera vez que se abre la app, el modo inicial sigue el tema del sistema
/// (solo Android). Después se respeta la última elección del usuario, guardada en PlayerPrefs.
/// </para>
/// <para>
/// Además publica el modo y los colores actuales de forma estática, para que los
/// prefabs que lleven <see cref="DarkModeItem"/> se pinten solos al activarse,
/// incluso si se instancian en runtime (como la lista de un dropdown) o estaban
/// apagados cuando cambió el modo.
/// </para>
/// </summary>
public class DarkModeManager : MonoBehaviour
{
    // =========================
    // ESTADO PÚBLICO (ESTÁTICO)
    // =========================

    /// <summary>Verdadero si el modo oscuro está activo. Consultable desde cualquier script.</summary>
    public static bool IsDark { get; private set; }

    /// <summary>Verdadero cuando el manager ya publicó sus colores.</summary>
    public static bool Listo { get; private set; }

    /// <summary>Color de fondo del modo actual.</summary>
    public static Color Fondo { get; private set; }

    /// <summary>Color de texto del modo actual.</summary>
    public static Color Texto { get; private set; }

    /// <summary>Color de placeholder del modo actual.</summary>
    public static Color Placeholder { get; private set; }

    /// <summary>Color (gris) de las sombras del modo actual. Su alpha se ignora.</summary>
    public static Color Sombra { get; private set; }

    /// <summary>Prefabs y paneles registrados que siguen el modo oscuro.</summary>
    private static readonly HashSet<DarkModeItem> items = new HashSet<DarkModeItem>();

    /// <summary>Clave de PlayerPrefs donde se guarda la elección del usuario (1 = oscuro, 0 = claro).</summary>
    private const string PrefKeyDarkMode = "dark_mode";

    // =========================
    // MODO ACTUAL
    // =========================

    /// <summary>
    /// Indica si el modo oscuro está activo actualmente.
    /// Se sobrescribe en Awake con la elección guardada o con el tema del sistema.
    /// </summary>
    [Header("Modo actual")]
    [SerializeField] private bool darkMode = false;

    // =========================
    // BOTONES
    // =========================

    /// <summary>Botón (padre) que activa el modo oscuro. Ej: Dark_Button.</summary>
    [Header("Botones")]
    [SerializeField] private Button darkModeButton;

    /// <summary>Botón (padre) que activa el modo claro. Ej: Light_Button.</summary>
    [SerializeField] private Button lightModeButton;

    // =========================
    // ELEMENTOS DE LA ESCENA
    // =========================

    /// <summary>Lista de fondos/paneles cuyo color cambia según el modo.</summary>
    [Header("Fondos / Paneles")]
    [SerializeField] private List<Image> backgrounds = new List<Image>();

    /// <summary>Lista de textos normales (títulos, etiquetas, botones) cuyo color cambia según el modo.</summary>
    [Header("Textos")]
    [SerializeField] private List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();

    /// <summary>Lista de imágenes (íconos, sprites) cuyo color cambia según el modo.</summary>
    [Header("Imágenes que deben cambiar")]
    [SerializeField] private List<Image> images = new List<Image>();

    /// <summary>Texto que escribe el usuario dentro de los inputs (el hijo "Text" del input).</summary>
    [Header("Inputs: texto escrito")]
    [SerializeField] private List<TextMeshProUGUI> textosInput = new List<TextMeshProUGUI>();

    /// <summary>Texto de ayuda de los inputs (el hijo "Placeholder", en tu caso "Contra").</summary>
    [Header("Inputs: placeholders")]
    [SerializeField] private List<TextMeshProUGUI> placeholders = new List<TextMeshProUGUI>();

    /// <summary>
    /// Sombras que cambian a gris según el modo. Acepta Shadow y Outline.
    /// Cada sombra conserva su transparencia original.
    /// </summary>
    [Header("Sombras")]
    [SerializeField] private List<Shadow> sombras = new List<Shadow>();

    /// <summary>
    /// Toggles, botones o dropdowns de la escena cuyo tinte (normal, hover, presionado)
    /// sigue el modo. Su Image debe estar en blanco, porque el tinte se multiplica con ella.
    /// </summary>
    [Header("Selectables (toggles, botones con tinte)")]
    [SerializeField] private List<Selectable> selectables = new List<Selectable>();

    // =========================
    // COLORES
    // =========================

    /// <summary>Color de fondo cuando el modo claro está activo.</summary>
    [Header("Colores")]
    [SerializeField] private Color lightBackground = Color.white;

    /// <summary>Color de fondo cuando el modo oscuro está activo.</summary>
    [SerializeField] private Color darkBackground = new Color(0.08f, 0.08f, 0.08f);

    /// <summary>Color de texto cuando el modo claro está activo.</summary>
    [SerializeField] private Color lightText = Color.black;

    /// <summary>Color de texto cuando el modo oscuro está activo.</summary>
    [SerializeField] private Color darkText = Color.white;

    /// <summary>Color del placeholder en modo claro (gris oscuro, #323232).</summary>
    [Header("Placeholder")]
    [SerializeField] private Color lightPlaceholder = new Color32(0x32, 0x32, 0x32, 255);

    /// <summary>Color del placeholder en modo oscuro (gris medio, #7E7E7E).</summary>
    [SerializeField] private Color darkPlaceholder = new Color32(0x7E, 0x7E, 0x7E, 255);

    /// <summary>Gris de las sombras en modo claro. Solo importa el RGB: el alpha de cada sombra se conserva.</summary>
    [Header("Sombras (solo RGB, el alpha de cada sombra se conserva)")]
    [SerializeField] private Color lightShadow = new Color32(0x32, 0x32, 0x32, 255);

    /// <summary>Gris de las sombras en modo oscuro. Solo importa el RGB: el alpha de cada sombra se conserva.</summary>
    [SerializeField] private Color darkShadow = new Color32(0x7E, 0x7E, 0x7E, 255);

    // =========================
    // SPRITES BLANCO / NEGRO
    // =========================

    /// <summary>
    /// Imagen cuyo sprite cambia entre una versión blanca y una negra según el modo.
    /// </summary>
    [System.Serializable]
    public class SpriteCambiante
    {
        [Tooltip("Solo para identificar este elemento en el Inspector, no afecta la lógica.")]
        public string nombre;

        [Tooltip("Image a la que se le cambia el sprite. Su Color debe estar en blanco puro.")]
        public Image imagen;

        [Tooltip("Sprite que se usa en MODO CLARO")]
        public Sprite spriteBlanco;

        [Tooltip("Sprite que se usa en MODO OSCURO")]
        public Sprite spriteNegro;
    }

    /// <summary>
    /// Imágenes que cambian de sprite (blanco en modo claro, negro en modo oscuro).
    /// No las agregues también a la lista de "Imágenes que deben cambiar".
    /// </summary>
    [Header("Imágenes con sprite blanco/negro")]
    [SerializeField] private List<SpriteCambiante> spritesCambiantes = new List<SpriteCambiante>();

    // =========================
    // ICONOS DE BOTONES
    // =========================

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

        [Tooltip("Image del hijo con la textura NEGRA")]
        public Image iconoNegro;

        [Tooltip("Image del hijo con la textura BLANCA")]
        public Image iconoBlanco;
    }

    /// <summary>Los 2 botones (Light_Button y Dark_Button) que hacen crossfade entre su textura negra y blanca.</summary>
    [Header("Iconos de botones (crossfade Negro/Blanco)")]
    [SerializeField] private List<IconoBoton> iconosBotones = new List<IconoBoton>();

    /// <summary>Duración en segundos de la animación de difuminado.</summary>
    [Header("Animación")]
    [SerializeField] private float fadeDuration = 0.25f;

    /// <summary>Guarda la corrutina activa de cada icono, para cancelarla si el modo cambia rápido varias veces.</summary>
    private readonly Dictionary<IconoBoton, Coroutine> corrutinasActivas = new Dictionary<IconoBoton, Coroutine>();

    // =========================
    // REGISTRO DE PREFABS
    // =========================

    /// <summary>
    /// Lo llaman los prefabs al activarse. Si el manager ya está listo,
    /// el prefab se pinta al instante con el modo actual.
    /// </summary>
    /// <param name="item">Prefab o panel que se registra.</param>
    public static void Registrar(DarkModeItem item)
    {
        items.Add(item);

        if (Listo)
            item.Aplicar(Fondo, Texto, Placeholder, Sombra);
    }

    /// <summary>Lo llaman los prefabs al desactivarse, para dejar de recibir cambios.</summary>
    /// <param name="item">Prefab o panel que se quita.</param>
    public static void Quitar(DarkModeItem item)
    {
        items.Remove(item);
    }

    // =========================
    // AYUDANTES ESTÁTICOS
    // =========================

    /// <summary>
    /// Cambia el color de una sombra al gris indicado, conservando su transparencia original
    /// (así una sombra tenue sigue siendo tenue en ambos modos).
    /// </summary>
    /// <param name="sombra">Sombra u Outline a pintar.</param>
    /// <param name="gris">Color del modo actual (se usa solo su RGB).</param>
    public static void AplicarSombra(Shadow sombra, Color gris)
    {
        if (sombra == null) return;

        Color c = gris;
        c.a = sombra.effectColor.a;
        sombra.effectColor = c;
    }

    /// <summary>
    /// Pinta el tinte de un Toggle, botón o ítem de dropdown según el modo:
    /// normal con el fondo, y hover/presionado mezclando fondo y texto.
    /// Su Image debe estar en blanco, porque el tinte se multiplica con ella.
    /// </summary>
    /// <param name="s">Selectable a pintar.</param>
    /// <param name="fondo">Color de fondo del modo actual.</param>
    /// <param name="texto">Color de texto del modo actual.</param>
    public static void AplicarSelectable(Selectable s, Color fondo, Color texto)
    {
        if (s == null) return;

        ColorBlock cb = s.colors;
        cb.normalColor = fondo;
        cb.highlightedColor = Color.Lerp(fondo, texto, 0.12f);   // hover
        cb.selectedColor = cb.highlightedColor;
        cb.pressedColor = Color.Lerp(fondo, texto, 0.22f);
        s.colors = cb;
    }

    /// <summary>
    /// Devuelve true si el dispositivo tiene activado el tema oscuro del sistema.
    /// Solo funciona en Android (en el Editor y otras plataformas devuelve false).
    /// </summary>
    private static bool SistemaEnModoOscuro()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var resources = activity.Call<AndroidJavaObject>("getResources"))
            using (var config = resources.Call<AndroidJavaObject>("getConfiguration"))
            {
                int uiMode = config.Get<int>("uiMode");
                return (uiMode & 0x30) == 0x20;   // UI_MODE_NIGHT_MASK / UI_MODE_NIGHT_YES
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("No se pudo leer el tema del sistema: " + e.Message);
            return false;
        }
#else
        return false;
#endif
    }

    // =========================
    // CICLO DE VIDA
    // =========================

    /// <summary>
    /// Decide el modo inicial: si el usuario ya eligió uno, se respeta; si no, se usa
    /// el tema del sistema. Luego publica el modo y los colores antes de que cualquier prefab los pida.
    /// </summary>
    private void Awake()
    {
        if (PlayerPrefs.HasKey(PrefKeyDarkMode))
            darkMode = PlayerPrefs.GetInt(PrefKeyDarkMode) == 1;
        else
            darkMode = SistemaEnModoOscuro();

        Publicar();
    }

    /// <summary>Al destruirse el manager, los prefabs dejan de considerarlo listo.</summary>
    private void OnDestroy()
    {
        Listo = false;
    }

    /// <summary>
    /// Al iniciar, conecta cada botón con su modo correspondiente (oscuro o claro),
    /// y aplica de inmediato (sin animar) el modo actual a todos los elementos registrados.
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

    /// <summary>Copia el modo y los colores actuales a las propiedades estáticas.</summary>
    private void Publicar()
    {
        IsDark = darkMode;
        Fondo = darkMode ? darkBackground : lightBackground;
        Texto = darkMode ? darkText : lightText;
        Placeholder = darkMode ? darkPlaceholder : lightPlaceholder;
        Sombra = darkMode ? darkShadow : lightShadow;
        Listo = true;
    }

    // =========================
    // CAMBIO DE MODO
    // =========================

    /// <summary>Activa el modo oscuro (llamado por el botón Dark_Button).</summary>
    public void ActivarModoOscuro()
    {
        SetDarkMode(true);
    }

    /// <summary>Activa el modo claro / blanco (llamado por el botón Light_Button).</summary>
    public void ActivarModoClaro()
    {
        SetDarkMode(false);
    }

    /// <summary>Cambia entre modo oscuro y modo claro, y aplica el cambio de inmediato.</summary>
    public void ToggleDarkMode()
    {
        SetDarkMode(!darkMode);
    }

    /// <summary>
    /// Establece directamente el modo oscuro como activado o desactivado,
    /// guarda la elección del usuario y aplica el cambio (con animación)
    /// a todos los elementos registrados.
    /// </summary>
    /// <param name="enabled">Verdadero para activar el modo oscuro, falso para el modo claro.</param>
    public void SetDarkMode(bool enabled)
    {
        darkMode = enabled;
        PlayerPrefs.SetInt(PrefKeyDarkMode, enabled ? 1 : 0);
        ApplyDarkMode();
    }

    /// <summary>
    /// Publica los colores, recorre las listas (fondos, textos, imágenes, texto de inputs,
    /// placeholders, sombras, selectables y sprites blanco/negro) y los prefabs registrados
    /// aplicándoles el valor del modo actual, y dispara el crossfade de las texturas de los botones.
    /// </summary>
    /// <param name="instant">Si es verdadero, aplica el cambio sin animar (usado al iniciar la escena).</param>
    private void ApplyDarkMode(bool instant = false)
    {
        Publicar();

        // Fondos de los paneles y extras que manejen colores
        foreach (Image background in backgrounds)
        {
            if (background != null)
            {
                background.color = Fondo;
            }
        }

        // Textos normales del juego
        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null)
            {
                text.color = Texto;
            }
        }

        // Imágenes agregadas como los sprites
        foreach (Image image in images)
        {
            if (image != null)
            {
                image.color = Texto;
            }
        }

        // Texto que escribe el usuario en los inputs
        foreach (TextMeshProUGUI t in textosInput)
        {
            if (t != null)
            {
                t.color = Texto;
            }
        }

        // Placeholders (gris, no blanco ni negro puros)
        foreach (TextMeshProUGUI p in placeholders)
        {
            if (p != null)
            {
                p.color = Placeholder;
            }
        }

        // Sombras en gris, conservando su transparencia
        foreach (Shadow s in sombras)
        {
            AplicarSombra(s, Sombra);
        }

        // Tintes de toggles, botones e ítems (normal, hover, presionado)
        foreach (Selectable s in selectables)
        {
            AplicarSelectable(s, Fondo, Texto);
        }

        // Imágenes que cambian de sprite: blanco en modo claro, negro en modo oscuro
        foreach (SpriteCambiante sc in spritesCambiantes)
        {
            if (sc != null && sc.imagen != null)
            {
                sc.imagen.sprite = darkMode ? sc.spriteNegro : sc.spriteBlanco;
            }
        }

        // Prefabs y paneles que se registraron solos
        foreach (DarkModeItem item in items)
        {
            if (item != null)
            {
                item.Aplicar(Fondo, Texto, Placeholder, Sombra);
            }
        }

        AnimarIconosBotones(instant);
    }

    // =========================
    // CROSSFADE DE ICONOS
    // =========================

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
    /// desactiva el GameObject del que salió.
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

        // Al terminar, dejamos el saliente apagado y el entrante bien encendido con alpha 1.
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