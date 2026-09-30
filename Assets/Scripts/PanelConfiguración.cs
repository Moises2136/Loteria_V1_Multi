using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla el panel de configuración del juego: permite ajustar
/// el volumen general, elegir entre voz masculina o femenina, y probar cómo
/// suena esa voz (ya sea generándola con OpenAI TTS, o usando un clip de
/// respaldo guardado localmente si el TTS no está disponible).
/// </summary>
public class PanelConfiguración : MonoBehaviour
{
    // =========================
    // AUDIO
    // =========================

    /// <summary>Control deslizante para ajustar el volumen general.</summary>
    public Slider sliderVolumen;

    /// <summary>Texto que muestra el porcentaje de volumen actual (ej. "80%").</summary>
    public TMP_Text textoPorcentajeVolumen;

    /// <summary>AudioSource usado para reproducir la prueba de voz.</summary>
    public AudioSource audioSourcePrueba;

    // =========================
    // BOTONES DE VOZ
    // =========================

    /// <summary>Botón para seleccionar la voz masculina.</summary>
    public Button botonVozHombre;

    /// <summary>Botón para seleccionar la voz femenina.</summary>
    public Button botonVozMujer;

    /// <summary>Botón para escuchar cómo suena la voz seleccionada.</summary>
    public Button botonProbarVoz;

    // =========================
    // ESTADOS VISUALES
    // =========================

    /// <summary>Ícono que se muestra cuando la voz masculina NO está seleccionada.</summary>
    public GameObject hombreDesactivado;

    /// <summary>Ícono que se muestra cuando la voz masculina SÍ está seleccionada.</summary>
    public GameObject hombreActivado;

    /// <summary>Ícono que se muestra cuando la voz femenina NO está seleccionada.</summary>
    public GameObject mujerDesactivado;

    /// <summary>Ícono que se muestra cuando la voz femenina SÍ está seleccionada.</summary>
    public GameObject mujerActivado;

    // =========================
    // CLIPS LOCALES
    // =========================

    /// <summary>Audio de respaldo para la voz masculina, por si no hay conexión al TTS online.</summary>
    public AudioClip clipVozHombre;

    /// <summary>Audio de respaldo para la voz femenina, por si no hay conexión al TTS online.</summary>
    public AudioClip clipVozMujer;

    // =========================
    // OPENAI TTS
    // =========================

    /// <summary>Referencia al script que genera audio con ElevenLabsTTs (texto a voz).</summary>
    public ElevenLabsTTS ttsManager;

    /// <summary>Texto de ejemplo que se lee en voz alta al probar la voz.</summary>
    [TextArea] public string textoPrueba = "Bienvenido a la lotería mexicana de plantas medicinales.";

    // =========================
    // NAVEGACIÓN
    // =========================

    /// <summary>Botón para regresar al panel anterior.</summary>
    public Button botonRegresar;

    /// <summary>Panel al que se regresa al presionar el botón.</summary>
    public GameObject panelAnterior;

    /// <summary>Este mismo panel de configuración.</summary>
    public GameObject panelConfig;

    // =========================
    // CONFIGURACIÓN (PLAYERPREFS)
    // =========================

    /// <summary>Llave usada para guardar el volumen en PlayerPrefs.</summary>
    private const string KEY_VOLUMEN = "config_volumen";

    /// <summary>Llave usada para guardar la voz elegida en PlayerPrefs.</summary>
    private const string KEY_VOZ = "config_voz"; // 0 = hombre, 1 = mujer

    /// <summary>Volumen actual, en un rango de 0 (silencio) a 1 (máximo).</summary>
    private float volumenActual = 0f;

    /// <summary>Voz seleccionada actualmente: 0 para hombre, 1 para mujer.</summary>
    private int vozSeleccionada = 0;

    // =========================
    // INICIALIZACIÓN
    // =========================

    /// <summary>
    /// Al iniciar, carga la configuración guardada, prepara la interfaz,
    /// conecta los botones, y aplica el volumen y la voz guardados.
    /// </summary>
    void Start()
    {
        CargarConfiguracion();
        ConfigurarUIInicial();
        ConfigurarEventos();
        AplicarVolumen();
        AplicarEstadoVisualVoz();
    }

    /// <summary>
    /// Deja el slider de volumen con el rango correcto (0 a 1) y su valor
    /// inicial cargado, y actualiza el texto de porcentaje.
    /// </summary>
    void ConfigurarUIInicial()
    {
        if (sliderVolumen != null)
        {
            sliderVolumen.minValue = 0f;
            sliderVolumen.maxValue = 1f;
            sliderVolumen.value = volumenActual;
        }

        ActualizarTextoVolumen();
    }

    /// <summary>
    /// Conecta el slider de volumen y todos los botones de esta pantalla
    /// con sus funciones correspondientes.
    /// </summary>
    void ConfigurarEventos()
    {
        if (sliderVolumen != null)
        {
            sliderVolumen.onValueChanged.RemoveAllListeners();
            sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
        }

        if (botonVozHombre != null)
        {
            botonVozHombre.onClick.RemoveAllListeners();
            botonVozHombre.onClick.AddListener(SeleccionarVozHombre);
        }

        if (botonVozMujer != null)
        {
            botonVozMujer.onClick.RemoveAllListeners();
            botonVozMujer.onClick.AddListener(SeleccionarVozMujer);
        }

        if (botonProbarVoz != null)
        {
            botonProbarVoz.onClick.RemoveAllListeners();
            botonProbarVoz.onClick.AddListener(ProbarVozSeleccionada);
        }

        if (botonRegresar != null)
        {
            botonRegresar.onClick.RemoveAllListeners();
            botonRegresar.onClick.AddListener(Regresar);
        }
    }

    // =========================
    // GUARDADO Y CARGA
    // =========================

    /// <summary>
    /// Lee de PlayerPrefs el volumen y la voz guardados de una sesión anterior.
    /// </summary>
    void CargarConfiguracion()
    {
        volumenActual = PlayerPrefs.GetFloat(KEY_VOLUMEN, 1f);
        vozSeleccionada = PlayerPrefs.GetString("VOICE_TYPE", "female") == "male" ? 0 : 1;
    }

    /// <summary>
    /// Guarda el volumen y la voz actuales en PlayerPrefs, para recordarlos
    /// la próxima vez que se abra el juego.
    /// </summary>
    void GuardarConfiguracion()
    {
        PlayerPrefs.SetFloat(KEY_VOLUMEN, volumenActual);
        PlayerPrefs.Save();
    }

    // =========================
    // CONTROL DE VOLUMEN
    // =========================

    /// <summary>
    /// Se llama cada vez que el usuario mueve el slider de volumen.
    /// Aplica el nuevo volumen, actualiza el texto, y lo guarda.
    /// </summary>
    /// <param name="valor">El nuevo valor de volumen, entre 0 y 1.</param>
    public void CambiarVolumen(float valor)
    {
        volumenActual = valor;
        AplicarVolumen();
        ActualizarTextoVolumen();
        GuardarConfiguracion();
    }

    /// <summary>
    /// Aplica el volumen actual tanto al audio general del juego
    /// (<c>AudioListener.volume</c>) como al AudioSource de prueba.
    /// </summary>
    void AplicarVolumen()
    {
        AudioListener.volume = volumenActual;

        if (audioSourcePrueba != null)
            audioSourcePrueba.volume = volumenActual;
    }

    /// <summary>
    /// Actualiza el texto que muestra el volumen como porcentaje (ej. "80%").
    /// </summary>
    void ActualizarTextoVolumen()
    {
        if (textoPorcentajeVolumen != null)
        {
            int porcentaje = Mathf.RoundToInt(volumenActual * 100f);
            textoPorcentajeVolumen.text = porcentaje + "%";
        }
    }

    // =========================
    // SELECCIÓN DE VOZ
    // =========================

    /// <summary>
    /// Selecciona la voz masculina, actualiza los íconos, y guarda la elección.
    /// </summary>
    public void SeleccionarVozHombre()
    {
        vozSeleccionada = 0;
        AplicarEstadoVisualVoz();
        GuardarConfiguracion();
        if (ttsManager != null) ttsManager.SetVoice("male");
        else PlayerPrefs.SetString("VOICE_TYPE", "male");
    }

    /// <summary>
    /// Selecciona la voz femenina, actualiza los íconos, y guarda la elección.
    /// </summary>
    public void SeleccionarVozMujer()
    {
        vozSeleccionada = 1;
        AplicarEstadoVisualVoz();
        GuardarConfiguracion();
        if (ttsManager != null) ttsManager.SetVoice("female");
        else PlayerPrefs.SetString("VOICE_TYPE", "female");
    }

    /// <summary>
    /// Actualiza qué íconos se ven activados o desactivados, según cuál
    /// voz esté seleccionada en este momento.
    /// </summary>
    void AplicarEstadoVisualVoz()
    {
        bool hombreActivo = (vozSeleccionada == 0);
        bool mujerActivo = (vozSeleccionada == 1);

        if (hombreActivado != null) hombreActivado.SetActive(hombreActivo);
        if (hombreDesactivado != null) hombreDesactivado.SetActive(!hombreActivo);

        if (mujerActivado != null) mujerActivado.SetActive(mujerActivo);
        if (mujerDesactivado != null) mujerDesactivado.SetActive(!mujerActivo);
    }

    // =========================
    // PRUEBA DE VOZ
    // =========================

    /// <summary>
    /// Reproduce un texto de ejemplo usando la voz seleccionada. Primero
    /// intenta generar el audio con ElevenLabs si no hay un <see cref="ttsManager"/>
    /// asignado, usa un clip de audio local guardado como respaldo.
    /// </summary>
    public void ProbarVozSeleccionada()
{
    if (ttsManager != null)
    {
        ttsManager.SpeakText(textoPrueba);
        return;
    }

    // Respaldo local
    if (audioSourcePrueba == null)
    {
        Debug.LogWarning("No asignaste audioSourcePrueba.");
        return;
    }

    AudioClip clipAReproducir = vozSeleccionada == 0 ? clipVozHombre : clipVozMujer;

    if (clipAReproducir == null)
    {
        Debug.LogWarning("No asignaste clip local y tampoco TTS.");
        return;
    }

    audioSourcePrueba.Stop();
    audioSourcePrueba.clip = clipAReproducir;
    audioSourcePrueba.volume = volumenActual;
    audioSourcePrueba.Play();
}

    // =========================
    // NAVEGACIÓN
    // =========================

    /// <summary>
    /// Cierra el panel de configuración y regresa al panel anterior.
    /// </summary>
    public void Regresar()
    {
        if (panelConfig != null)
            panelConfig.SetActive(false);

        if (panelAnterior != null)
            panelAnterior.SetActive(true);
    }

    // =========================
    // GETTERS
    // =========================

    /// <summary>
    /// Devuelve cuál voz está seleccionada actualmente (0 = hombre, 1 = mujer).
    /// </summary>
    /// <returns>0 si está seleccionada la voz masculina, 1 si es la femenina.</returns>
    public int ObtenerVozSeleccionada()
    {
        return vozSeleccionada;
    }

    /// <summary>
    /// Devuelve el volumen actual configurado.
    /// </summary>
    /// <returns>Un valor entre 0 (silencio) y 1 (volumen máximo).</returns>
    public float ObtenerVolumenActual()
    {
        return volumenActual;
    }
}