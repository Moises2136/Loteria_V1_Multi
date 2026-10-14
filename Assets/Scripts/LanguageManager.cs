using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla todo el sistema de idiomas del juego: cambia los
/// textos de la interfaz según el idioma elegido (español, inglés, francés
/// o italiano), actualiza los botones de selección de idioma para mostrar
/// cuál está activo, y guarda/recupera el idioma elegido usando PlayerPrefs.
/// Es un singleton, así que solo existe una instancia y se puede acceder
/// desde cualquier parte usando <c>LanguageManager.Instance</c>.
/// </summary>
public class LanguageManager : MonoBehaviour
{
    /// <summary>
    /// La única instancia de este script que existe en el juego.
    /// </summary>
    public static LanguageManager Instance;

    /// <summary>
    /// Los idiomas disponibles en la aplicación.
    /// </summary>
    public enum Language
    {
        Spanish,
        English,
        French,
        Italian
    }

    /// <summary>Idioma que está activo actualmente en la aplicación.</summary>
    [Header("Idioma actual")]
    public Language currentLanguage = Language.Spanish;

    /// <summary>
    /// Se dispara cada vez que se aplica un idioma (al iniciar y al cambiarlo).
    /// Lo escuchan los textos de prefabs que no están en localizedTexts.
    /// </summary>
    public static event System.Action<Language> OnIdiomaAplicado;

    /// <summary>
    /// Lista de todos los textos de la interfaz que cambian según el idioma
    /// (títulos, etiquetas, descripciones, etc). NO se deben poner aquí los
    /// botones de idioma — esos van en su propia sección más abajo.
    /// </summary>
    [Header("Textos traducibles de la UI (NO meter aquí los botones de idioma)")]
    public List<LocalizedText> localizedTexts = new List<LocalizedText>();

    // =========================
    // BOTONES DE IDIOMA
    // =========================

    /// <summary>Botón para seleccionar español.</summary>
    [Header("Botones de idioma")]
    public LanguageButtonUI spanishButton;

    /// <summary>Botón para seleccionar inglés.</summary>
    public LanguageButtonUI englishButton;

    /// <summary>Botón para seleccionar francés.</summary>
    public LanguageButtonUI frenchButton;

    /// <summary>Botón para seleccionar italiano.</summary>
    public LanguageButtonUI italianButton;

    // =========================
    // DROPDOWN DE IDIOMA (panel de inicio de sesión)
    // =========================
    /// <summary>Desplegable de idioma (panel de inicio de sesión).</summary>
    [Header("Dropdown de idioma (panel de inicio de sesión)")]

    public TMP_Dropdown languageDropdownInicio;
     /// <summary>Texto corto del dropdown cerrado ("ES", "EN"...).</summary>
    public TMP_Text dropdownLabelInicio;

    private readonly string[] dropdownNames = { "Español", "English", "Français", "Italiano" };
    private readonly string[] dropdownCodes = { "ES", "EN", "FR", "IT" };


    // =========================
    // DROPDOWN DE IDIOMA (panel Registrarse)
    // =========================
    /// <summary>Desplegable de idioma (panel registro).</summary>
    [Header("Dropdown de idioma (panel registro)")]
    public TMP_Dropdown languageDropdownRegistro;
     /// <summary>Texto corto del dropdown cerrado ("ES", "EN"...).</summary>
    public TMP_Text dropdownLabelRegistro;

    [Header("Banderas de los idiomas")]

    /// <summary>Banderas, en el mismo orden que el enum Language.</summary>
    public Sprite flagSpanish, flagEnglish, flagFrench, flagItalian;

    /// <summary>Imagen de la bandera del dropdown cerrado (inicio de sesión).</summary>
    public Image dropdownFlagInicio;

    /// <summary>Imagen de la bandera del dropdown cerrado (registro).</summary>
    public Image dropdownFlagRegistro;

        /// <summary>Las banderas en el mismo orden que el enum Language.</summary>
    public Sprite[] Flags => new[] { flagSpanish, flagEnglish, flagFrench, flagItalian };

    // =========================
    // COLORES
    // =========================

    /// <summary>Color que se le pone al texto del botón del idioma actualmente activo.</summary>
    [Header("Colores")]
    public Color activeTextColor = new Color(0.47f, 0.71f, 0.21f);

    /// <summary>Color que se le pone al texto de los botones de idiomas que NO están activos.</summary>
    public Color inactiveTextColor = Color.gray;

    /// <summary>Llave usada para guardar el idioma elegido en PlayerPrefs.</summary>
    private const string LANGUAGE_KEY = "GAME_LANGUAGE";

    // =========================
    // TRANSICION DE IDIOMA 
    // =========================

    /// <summary>
    /// Duración de cada mitad de la transición (desvanecer y aparecer).
    /// La transición completa dura el doble. En 0, el cambio es instantáneo.
    /// </summary>
    
    [Header("Transición al cambiar idioma")]
    [Min(0f)] public float duracionTransicion = 0.2f;
    
    ///<summary>Transición en curso, para poder reiniciarla si se cambia de idioma a media animación.</summary>
     
    private Coroutine rutinaTransicion;
    
    /// <summary>
    /// Alpha original de cada texto. Se guarda la primera vez para que, si una
    /// transición se interrumpe, los textos no se queden medio transparentes.
    /// </summary>
    private readonly Dictionary<TMP_Text, float> alfaOriginal = new Dictionary<TMP_Text, float>();

    /// <summary>
    /// Se ejecuta al cargar el script. Aquí se asegura de que solo exista
    /// una instancia de este manager en todo el juego (patrón singleton).
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Al iniciar, conecta los botones de idioma, carga el idioma guardado
    /// de una sesión anterior (si existe), y aplica ese idioma a toda la interfaz.
    /// </summary>
    private void Start()
    {
        Debug.Log("LanguageManager iniciado");

        SetupButtons();
        SetupDropdownIniciarSesion();
        SetupDropdownRegistroR();
        LoadLanguage();
        ApplyLanguage();

    }

    /// <summary>
    /// Se ejecuta automáticamente cuando cambias algún valor desde el
    /// Inspector en el editor de Unity (sin necesidad de estar en Play).
    /// Sirve para ver el cambio de idioma reflejado al momento mientras
    /// trabajas en el editor.
    /// </summary>
    private void OnValidate()
    {
        // Evita errores si todavía no están cargadas todas las referencias
        if (localizedTexts == null)
            return;

        ApplyLanguage();
    }

    #region CONFIGURAR BOTONES

    /// <summary>
    /// Conecta cada botón de idioma con la función que activa ese idioma.
    /// </summary>
    private void SetupButtons()
    {
        if (spanishButton != null && spanishButton.button != null)
        {
            spanishButton.button.onClick.RemoveListener(SetSpanish);
            spanishButton.button.onClick.AddListener(SetSpanish);
        }

        if (englishButton != null && englishButton.button != null)
        {
            englishButton.button.onClick.RemoveListener(SetEnglish);
            englishButton.button.onClick.AddListener(SetEnglish);
        }

        if (frenchButton != null && frenchButton.button != null)
        {
            frenchButton.button.onClick.RemoveListener(SetFrench);
            frenchButton.button.onClick.AddListener(SetFrench);
        }

        if (italianButton != null && italianButton.button != null)
        {
            italianButton.button.onClick.RemoveListener(SetItalian);
            italianButton.button.onClick.AddListener(SetItalian);
        }

        Debug.Log("Botones de idioma enlazados");
    }

    #endregion

    #region DROPDOWNIniciarSesion

    /// <summary>Llena el dropdown con banderas y nombres, y lo conecta.</summary>
    private void SetupDropdownIniciarSesion()
    {
        if (languageDropdownInicio == null)
            return;

        languageDropdownInicio.ClearOptions();
        languageDropdownInicio.AddOptions(new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData(dropdownNames[0], flagSpanish, Color.white),
            new TMP_Dropdown.OptionData(dropdownNames[1], flagEnglish, Color.white),
            new TMP_Dropdown.OptionData(dropdownNames[2], flagFrench, Color.white),
            new TMP_Dropdown.OptionData(dropdownNames[3], flagItalian, Color.white),
        });

        languageDropdownInicio.onValueChanged.RemoveListener(OnDropdownChangedI);
        languageDropdownInicio.onValueChanged.AddListener(OnDropdownChangedI);
    }

    /// <summary>Se llama cuando el usuario elige un idioma en el dropdown.</summary>
    private void OnDropdownChangedI(int index)
    {
        SetLanguage((Language)index);
    }

    /// <summary>Pone el dropdown en el idioma actual sin disparar OnDropdownChangedI.</summary>
    private void UpdateDropdownI()
    {
        if (languageDropdownInicio == null || languageDropdownInicio.options.Count == 0)
            return;

        languageDropdownInicio.SetValueWithoutNotify((int)currentLanguage);

        if (dropdownLabelInicio != null)
            dropdownLabelInicio.text = dropdownCodes[(int)currentLanguage];

        if (dropdownFlagInicio != null)
            dropdownFlagInicio.sprite = Flags[(int)currentLanguage];
    }

    #endregion

    #region DROPDOWNRegistro

    /// <summary>Llena el dropdown con banderas y nombres, y lo conecta.</summary>
    private void SetupDropdownRegistroR()
    {
        if (languageDropdownRegistro == null)
            return;

        languageDropdownRegistro.ClearOptions();
        languageDropdownRegistro.AddOptions(new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData(dropdownNames[0], flagSpanish, Color.white),
            new TMP_Dropdown.OptionData(dropdownNames[1], flagEnglish, Color.white),
            new TMP_Dropdown.OptionData(dropdownNames[2], flagFrench, Color.white),
            new TMP_Dropdown.OptionData(dropdownNames[3], flagItalian, Color.white),
        });

        languageDropdownRegistro.onValueChanged.RemoveListener(OnDropdownChangedR);
        languageDropdownRegistro.onValueChanged.AddListener(OnDropdownChangedR);
    }

    /// <summary>Se llama cuando el usuario elige un idioma en el dropdown.</summary>
    private void OnDropdownChangedR(int index)
    {
        SetLanguage((Language)index);
    }

    /// <summary>Pone el dropdown en el idioma actual sin disparar OnDropdownChangedR.</summary>
    private void UpdateDropdownR()
    {
        if (languageDropdownRegistro == null || languageDropdownRegistro.options.Count == 0)
            return;

        languageDropdownRegistro.SetValueWithoutNotify((int)currentLanguage);

        if (dropdownLabelRegistro != null)
            dropdownLabelRegistro.text = dropdownCodes[(int)currentLanguage];

        if (dropdownFlagRegistro != null)
            dropdownFlagRegistro.sprite = Flags[(int)currentLanguage];
    }

    #endregion

    #region BOTONES DE IDIOMA

    /// <summary>Cambia el idioma activo a español.</summary>
    public void SetSpanish()
    {
        Debug.Log("Se presionó el botón Español");
        SetLanguage(Language.Spanish);
    }

    /// <summary>Cambia el idioma activo a inglés.</summary>
    public void SetEnglish()
    {
        Debug.Log("Se presionó el botón Inglés");
        SetLanguage(Language.English);
    }

    /// <summary>Cambia el idioma activo a francés.</summary>
    public void SetFrench()
    {
        Debug.Log("Se presionó el botón Francés");
        SetLanguage(Language.French);
    }

    /// <summary>Cambia el idioma activo a italiano.</summary>
    public void SetItalian()
    {
        Debug.Log("Se presionó el botón Italiano");
        SetLanguage(Language.Italian);
    }

    #endregion

    /// <summary>
    /// Cambia el idioma activo del juego, lo guarda de forma permanente,
    /// y actualiza toda la interfaz para reflejar el nuevo idioma.
    /// </summary>
    /// <param name="newLanguage">El nuevo idioma a activar.</param>
    public void SetLanguage(Language newLanguage)
    {
    currentLanguage = newLanguage;
    SaveLanguage();

    Debug.Log("Idioma cambiado a: " + currentLanguage);

    if (duracionTransicion <= 0f || !isActiveAndEnabled)
    {
        ApplyLanguage();
        return;
    }

    if (rutinaTransicion != null)
        StopCoroutine(rutinaTransicion);

    rutinaTransicion = StartCoroutine(TransicionIdioma());
    }

    /// <summary>
    /// Desvanece todos los textos traducibles, aplica el nuevo idioma
    /// y los vuelve a mostrar suavemente.
    /// </summary>
    
    private IEnumerator TransicionIdioma()
    {
    List<TMP_Text> textos = ObtenerTextosAnimables();

    foreach (TMP_Text t in textos)
    {
        if (!alfaOriginal.ContainsKey(t))
            alfaOriginal[t] = t.alpha;
    }

    // 1) Desvanecer
    yield return Fundir(textos, false);

    // 2) Cambiar textos (invisibles)
    ApplyLanguage();

    // ApplyLanguage le pone color con alpha 1 a los botones de idioma; los regresamos a 0
    foreach (TMP_Text t in textos)
    {
        if (t != null) t.alpha = 0f;
    }

    // 3) Aparecer
    yield return Fundir(textos, true);

    rutinaTransicion = null;
    }
    
    /// <summary>
    /// Anima el alpha de los textos: hacia 0 (desvanecer) o hacia su alpha original (aparecer).
    /// </summary>
    /// <param name="textos">Textos a animar.</param>
    /// <param name="aparecer">true = aparecer, false = desvanecer.</param>
    
    private IEnumerator Fundir(List<TMP_Text> textos, bool aparecer)
    {
    var inicio = new Dictionary<TMP_Text, float>();
    foreach (TMP_Text t in textos)
    {
        if (t != null) inicio[t] = t.alpha;
    }

    for (float tiempo = 0f; tiempo < duracionTransicion; tiempo += Time.unscaledDeltaTime)
    {
        float p = Mathf.SmoothStep(0f, 1f, tiempo / duracionTransicion);

        foreach (TMP_Text t in textos)
        {
            if (t == null) continue;
            float destino = aparecer ? alfaOriginal[t] : 0f;
            t.alpha = Mathf.Lerp(inicio[t], destino, p);
        }

        yield return null;
    }

    foreach (TMP_Text t in textos)
    {
        if (t != null) t.alpha = aparecer ? alfaOriginal[t] : 0f;
    }
    }
    
    /// <summary>
    /// Junta todos los textos que cambian con el idioma: los de la lista,
    /// los de los botones de idioma y las etiquetas de los dropdowns.
    /// </summary>
    private List<TMP_Text> ObtenerTextosAnimables()
    {
    var lista = new List<TMP_Text>();

    foreach (LocalizedText item in localizedTexts)
    {
        if (item != null && item.tmpText != null)
            lista.Add(item.tmpText);
    }

    AgregarSiExiste(lista, spanishButton?.tmpText);
    AgregarSiExiste(lista, englishButton?.tmpText);
    AgregarSiExiste(lista, frenchButton?.tmpText);
    AgregarSiExiste(lista, italianButton?.tmpText);
    AgregarSiExiste(lista, dropdownLabelInicio);
    AgregarSiExiste(lista, dropdownLabelRegistro);

    return lista;
    }
    
    private void AgregarSiExiste(List<TMP_Text> lista, TMP_Text texto)
    {
    if (texto != null && !lista.Contains(texto))
        lista.Add(texto);
    }

    /// <summary>
    /// Devuelve el texto en el idioma actual. Si falta la traducción, usa español.
    /// Para textos que se escriben desde código (estados, mensajes, etc.).
    /// </summary>
    public static string Traducir(string es, string en, string fr, string it)
    {
        Language idioma = Instance != null ? Instance.currentLanguage : Language.Spanish;

        string valor = idioma switch
        {
            Language.English => en,
            Language.French  => fr,
            Language.Italian => it,
            _ => es
        };

        return string.IsNullOrEmpty(valor) ? es : valor;
    }

    /// <summary>
    /// Aplica el idioma actual a toda la interfaz: actualiza los textos
    /// generales y los botones de selección de idioma.
    /// </summary>
    public void ApplyLanguage()
    {
        Debug.Log("Aplicando idioma: " + currentLanguage);

        UpdateLocalizedTexts();
        UpdateLanguageButtons();
        UpdateDropdownI();
        UpdateDropdownR();
        OnIdiomaAplicado?.Invoke(currentLanguage);
    }

    /// <summary>
    /// Recorre todos los textos registrados en <see cref="localizedTexts"/>
    /// y actualiza cada uno con su traducción correspondiente al idioma actual.
    /// </summary>
    /// <remarks>
    /// Si un elemento de la lista no tiene su <c>tmpText</c> asignado en el
    /// Inspector, se salta ese elemento y avisa con un <c>Debug.LogWarning</c>
    /// ("tmpText null en localizedTexts") en vez de causar un error —
    /// por eso ese mensaje puede aparecer en la consola sin que el juego se rompa.
    /// </remarks>
    private void UpdateLocalizedTexts()
    {
        if (localizedTexts == null || localizedTexts.Count == 0)
        {
            Debug.LogWarning("No hay textos en localizedTexts");
            return;
        }

        foreach (LocalizedText item in localizedTexts)
        {
            if (item == null)
            {
                Debug.LogWarning("Elemento null en localizedTexts");
                continue;
            }

            if (item.tmpText == null)
            {
                Debug.LogWarning("tmpText null en localizedTexts");
                continue;
            }

            string translatedText = GetTranslatedText(item);
            item.tmpText.text = translatedText;

            Debug.Log("Texto actualizado [" + item.tmpText.name + "] -> " + translatedText);
        }
    }

    /// <summary>
    /// Devuelve la traducción correspondiente de un texto, según el idioma actual.
    /// </summary>
    /// <param name="item">El texto traducible del que se quiere obtener la traducción.</param>
    /// <returns>El texto en el idioma actualmente activo.</returns>
    private string GetTranslatedText(LocalizedText item)
    {
        switch (currentLanguage)
        {
            case Language.Spanish:
                return item.spanish;

            case Language.English:
                return item.english;

            case Language.French:
                return item.french;

            case Language.Italian:
                return item.italian;

            default:
                return item.spanish;
        }
    }

    /// <summary>
    /// Actualiza los cuatro botones de idioma (texto, color y estilo).
    /// </summary>
    private void UpdateLanguageButtons()
    {
        UpdateSingleButton(spanishButton, Language.Spanish);
        UpdateSingleButton(englishButton, Language.English);
        UpdateSingleButton(frenchButton, Language.French);
        UpdateSingleButton(italianButton, Language.Italian);
    }

    /// <summary>
    /// Actualiza un botón de idioma individual: le pone el nombre del
    /// idioma (traducido según el idioma activo), y lo resalta con color
    /// y negrita si es el idioma actualmente seleccionado.
    /// </summary>
    /// <param name="buttonUI">El botón de idioma a actualizar.</param>
    /// <param name="buttonLanguage">El idioma que representa este botón.</param>
    private void UpdateSingleButton(LanguageButtonUI buttonUI, Language buttonLanguage)
    {
        if (buttonUI == null)
        {
            Debug.LogWarning("buttonUI es NULL para " + buttonLanguage);
            return;
        }

        if (buttonUI.tmpText == null)
        {
            Debug.LogWarning("tmpText es NULL para " + buttonLanguage);
            return;
        }

        // Texto dinámico según idioma actual
        string buttonText = GetLanguageDisplayName(buttonLanguage);
        buttonUI.tmpText.text = buttonText;

        // Determinar si es el idioma activo
        bool isActive = currentLanguage == buttonLanguage;

        // Aplicar color y estilo
        buttonUI.tmpText.color = isActive ? activeTextColor : inactiveTextColor;
        buttonUI.tmpText.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;

        Debug.Log("Actualizado botón: " + buttonLanguage + " -> " + buttonText);
    }

    /// <summary>
    /// Devuelve el nombre de un idioma, ya traducido al idioma que está
    /// activo actualmente (por ejemplo, el botón de francés se llama
    /// "Francés (FR)" si el idioma activo es español, pero "French (FR)"
    /// si el idioma activo es inglés).
    /// </summary>
    /// <param name="buttonLanguage">El idioma cuyo nombre se quiere obtener.</param>
    /// <returns>El nombre del idioma, traducido al idioma actualmente activo.</returns>
    private string GetLanguageDisplayName(Language buttonLanguage)
    {
        switch (currentLanguage)
        {
            case Language.Spanish:
                switch (buttonLanguage)
                {
                    case Language.Spanish: return "Español (MX)";
                    case Language.English: return "Inglés (US)";
                    case Language.French: return "Francés (FR)";
                    case Language.Italian: return "Italiano (IT)";
                }
                break;

            case Language.English:
                switch (buttonLanguage)
                {
                    case Language.Spanish: return "Spanish (MX)";
                    case Language.English: return "English (US)";
                    case Language.French: return "French (FR)";
                    case Language.Italian: return "Italian (IT)";
                }
                break;

            case Language.French:
                switch (buttonLanguage)
                {
                    case Language.Spanish: return "Espagnol (MX)";
                    case Language.English: return "Anglais (US)";
                    case Language.French: return "Français (FR)";
                    case Language.Italian: return "Italien (IT)";
                }
                break;

            case Language.Italian:
                switch (buttonLanguage)
                {
                    case Language.Spanish: return "Spagnolo (MX)";
                    case Language.English: return "Inglese (US)";
                    case Language.French: return "Francese (FR)";
                    case Language.Italian: return "Italiano (IT)";
                }
                break;
        }

        return "";
    }

    /// <summary>
    /// Guarda el idioma actual en PlayerPrefs, para recordarlo la próxima
    /// vez que se abra el juego.
    /// </summary>
    private void SaveLanguage()
    {
        PlayerPrefs.SetInt(LANGUAGE_KEY, (int)currentLanguage);
        PlayerPrefs.Save();

        Debug.Log("Idioma guardado: " + currentLanguage);
    }

    /// <summary>
    /// Lee de PlayerPrefs el idioma guardado de una sesión anterior. Si no
    /// hay ninguno guardado, deja el idioma que ya tenía por defecto.
    /// </summary>
    private void LoadLanguage()
    {
        if (PlayerPrefs.HasKey(LANGUAGE_KEY))
        {
            currentLanguage = (Language)PlayerPrefs.GetInt(LANGUAGE_KEY);
            Debug.Log("Idioma cargado: " + currentLanguage);
        }
        else
        {
            Debug.Log("No había idioma guardado. Se usa por defecto: " + currentLanguage);
        }
    }
}

/// <summary>
/// Representa un texto de la interfaz que cambia según el idioma:
/// guarda el componente de texto y su traducción en cada uno de los
/// cuatro idiomas disponibles.
/// </summary>
[System.Serializable]
public class LocalizedText
{
    /// <summary>El componente de texto que se va a actualizar.</summary>
    public TMP_Text tmpText;

    /// <summary>Traducción al español.</summary>
    [TextArea(2, 4)] public string spanish;

    /// <summary>Traducción al inglés.</summary>
    [TextArea(2, 4)] public string english;

    /// <summary>Traducción al francés.</summary>
    [TextArea(2, 4)] public string french;

    /// <summary>Traducción al italiano.</summary>
    [TextArea(2, 4)] public string italian;
}

/// <summary>
/// Representa un botón de selección de idioma: guarda el botón en sí
/// y el texto que muestra el nombre del idioma.
/// </summary>
[System.Serializable]
public class LanguageButtonUI
{
    /// <summary>El botón que el jugador presiona para elegir este idioma.</summary>
    public Button button;

    /// <summary>El texto que muestra el nombre de este idioma.</summary>
    public TMP_Text tmpText;
}