using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla el panel de configuración del juego: permite ajustar
/// por SEPARADO el volumen de la voz y el volumen de los efectos de sonido (FX),
/// elegir entre voz masculina o femenina, y probar cómo suena esa voz (ya sea
/// generándola con ElevenLabs TTS, o usando un clip de respaldo guardado
/// localmente si el TTS no está disponible).
/// <para>
/// Los volúmenes quedan disponibles para cualquier script mediante
/// <see cref="VolumenVoz"/> y <see cref="VolumenFX"/>, que leen lo guardado en
/// PlayerPrefs (así funcionan aunque este panel nunca se haya abierto).
/// </para>
/// </summary>
public class PanelConfiguración : MonoBehaviour
{
    // =========================
    // AUDIO: VOZ
    // =========================

    /// <summary>Control deslizante para ajustar el volumen de la VOZ.</summary>
    [FormerlySerializedAs("sliderVolumen")]
    public Slider sliderVolumenVoz;

    /// <summary>Texto que muestra el porcentaje de volumen de la VOZ (ej. "80%").</summary>
    [FormerlySerializedAs("textoPorcentajeVolumen")]
    public TMP_Text textoPorcentajeVolumenVoz;

    /// <summary>AudioSource usado para reproducir la prueba de voz (y la voz del juego, si comparte este).</summary>
    public AudioSource audioSourcePrueba;

    // =========================
    // AUDIO: FX (EFECTOS DE SONIDO)
    // =========================

    /// <summary>Control deslizante para ajustar el volumen de los EFECTOS DE SONIDO (FX).</summary>
    [Header("FX (efectos de sonido)")]
    public Slider sliderVolumenFX;

    /// <summary>Texto que muestra el porcentaje de volumen de los FX (ej. "80%").</summary>
    public TMP_Text textoPorcentajeVolumenFX;

    /// <summary>
    /// AudioSources de efectos que ya existen en la escena (clics, aciertos, popups...).
    /// A todos se les aplica el volumen FX. Los que se reproducen con PlayOneShot
    /// desde otros scripts deben multiplicar por <see cref="VolumenFX"/>.
    /// </summary>
    public AudioSource[] audioSourcesFX;

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
    // TTS
    // =========================

    /// <summary>Referencia al script que genera audio con ElevenLabsTTS (texto a voz).</summary>
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

    /// <summary>Llave usada para guardar el volumen de la VOZ (se conserva la anterior para no perder lo ya guardado).</summary>
    private const string KEY_VOLUMEN = "config_volumen";

    /// <summary>Llave usada para guardar el volumen de los FX.</summary>
    private const string KEY_VOLUMEN_FX = "config_volumen_fx";

    /// <summary>Llave usada para guardar la voz elegida en PlayerPrefs.</summary>
    private const string KEY_VOZ = "config_voz"; // 0 = hombre, 1 = mujer

    /// <summary>Volumen actual de la VOZ, de 0 (silencio) a 1 (máximo).</summary>
    private float volumenVozActual = 0f;

    /// <summary>Volumen actual de los FX, de 0 (silencio) a 1 (máximo).</summary>
    private float volumenFXActual = 0f;

    /// <summary>Voz seleccionada actualmente: 0 para hombre, 1 para mujer.</summary>
    private int vozSeleccionada = 0;

    // =========================
    // VOLÚMENES PÚBLICOS (PARA OTROS SCRIPTS)
    // =========================

    /// <summary>
    /// Volumen de la VOZ guardado (0 a 1). Úsalo en cualquier script que reproduzca voz:
    /// <c>audioSource.volume = PanelConfiguración.VolumenVoz;</c>
    /// </summary>
    public static float VolumenVoz => PlayerPrefs.GetFloat(KEY_VOLUMEN, 1f);

    /// <summary>
    /// Volumen de los FX guardado (0 a 1). Úsalo en cualquier script que reproduzca efectos:
    /// <c>audioSource.PlayOneShot(clip, PanelConfiguración.VolumenFX);</c>
    /// </summary>
    public static float VolumenFX => PlayerPrefs.GetFloat(KEY_VOLUMEN_FX, 1f);

    // =========================
    // INICIALIZACIÓN
    // =========================

    /// <summary>
    /// Al iniciar, carga la configuración guardada, prepara la interfaz,
    /// conecta los botones, y aplica los volúmenes (voz y FX) y la voz guardados.
    /// </summary>
    void Start()
    {
        CargarConfiguracion();
        ConfigurarUIInicial();
        ConfigurarEventos();
        AplicarVolumenVoz();
        AplicarVolumenFX();
        AplicarEstadoVisualVoz();
    }

    /// <summary>
    /// Deja los sliders (voz y FX) con el rango correcto (0 a 1) y su valor
    /// inicial cargado, y actualiza los textos de porcentaje.
    /// </summary>
    void ConfigurarUIInicial()
    {
        if (sliderVolumenVoz != null)
        {
            sliderVolumenVoz.minValue = 0f;
            sliderVolumenVoz.maxValue = 1f;
            sliderVolumenVoz.SetValueWithoutNotify(volumenVozActual);
        }

        // FX
        if (sliderVolumenFX != null)
        {
            sliderVolumenFX.minValue = 0f;
            sliderVolumenFX.maxValue = 1f;
            sliderVolumenFX.SetValueWithoutNotify(volumenFXActual);
        }

        ActualizarTextoVolumenVoz();
        ActualizarTextoVolumenFX();
    }

    /// <summary>
    /// Conecta los sliders (voz y FX) y todos los botones de esta pantalla
    /// con sus funciones correspondientes.
    /// </summary>
    void ConfigurarEventos()
    {
        if (sliderVolumenVoz != null)
        {
            sliderVolumenVoz.onValueChanged.RemoveAllListeners();
            sliderVolumenVoz.onValueChanged.AddListener(CambiarVolumenVoz);
        }

        // FX
        if (sliderVolumenFX != null)
        {
            sliderVolumenFX.onValueChanged.RemoveAllListeners();
            sliderVolumenFX.onValueChanged.AddListener(CambiarVolumenFX);
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
    /// Lee de PlayerPrefs los volúmenes (voz y FX) y la voz guardados de una sesión anterior.
    /// </summary>
    void CargarConfiguracion()
    {
        volumenVozActual = PlayerPrefs.GetFloat(KEY_VOLUMEN, 1f);
        volumenFXActual = PlayerPrefs.GetFloat(KEY_VOLUMEN_FX, 1f);   // FX
        vozSeleccionada = PlayerPrefs.GetString("VOICE_TYPE", "female") == "male" ? 0 : 1;
    }

    /// <summary>
    /// Guarda los volúmenes (voz y FX) en PlayerPrefs, para recordarlos
    /// la próxima vez que se abra el juego.
    /// </summary>
    void GuardarConfiguracion()
    {
        PlayerPrefs.SetFloat(KEY_VOLUMEN, volumenVozActual);
        PlayerPrefs.SetFloat(KEY_VOLUMEN_FX, volumenFXActual);   // FX
        PlayerPrefs.Save();
    }

    // =========================
    // CONTROL DE VOLUMEN: VOZ
    // =========================

    /// <summary>
    /// Se llama cada vez que el usuario mueve el slider de volumen de la VOZ.
    /// Aplica el nuevo volumen, actualiza el texto, y lo guarda.
    /// </summary>
    /// <param name="valor">El nuevo valor de volumen, entre 0 y 1.</param>
    public void CambiarVolumenVoz(float valor)
    {
        volumenVozActual = valor;
        AplicarVolumenVoz();
        ActualizarTextoVolumenVoz();
        GuardarConfiguracion();
    }

    /// <summary>
    /// Aplica el volumen de la VOZ al AudioSource de prueba. Ya no se usa
    /// <c>AudioListener.volume</c> porque afectaría también a los FX; se deja en 1
    /// y cada grupo de sonido (voz / FX) se ajusta por separado.
    /// </summary>
    void AplicarVolumenVoz()
    {
        AudioListener.volume = 1f;

        if (audioSourcePrueba != null)
            audioSourcePrueba.volume = volumenVozActual;
    }

    /// <summary>
    /// Actualiza el texto que muestra el volumen de la VOZ como porcentaje (ej. "80%").
    /// </summary>
    void ActualizarTextoVolumenVoz()
    {
        if (textoPorcentajeVolumenVoz != null)
        {
            int porcentaje = Mathf.RoundToInt(volumenVozActual * 100f);
            textoPorcentajeVolumenVoz.text = porcentaje + "%";
        }
    }

    // =========================
    // CONTROL DE VOLUMEN: FX
    // =========================

    /// <summary>
    /// Se llama cada vez que el usuario mueve el slider de volumen de los FX.
    /// Aplica el nuevo volumen, actualiza el texto, y lo guarda.
    /// </summary>
    /// <param name="valor">El nuevo valor de volumen FX, entre 0 y 1.</param>
    public void CambiarVolumenFX(float valor)
    {
        volumenFXActual = valor;
        AplicarVolumenFX();
        ActualizarTextoVolumenFX();
        GuardarConfiguracion();
    }

    /// <summary>
    /// Aplica el volumen de los FX a todos los AudioSources de efectos asignados
    /// en <see cref="audioSourcesFX"/>.
    /// </summary>
    void AplicarVolumenFX()
    {
        if (audioSourcesFX == null) return;

        foreach (AudioSource fx in audioSourcesFX)
        {
            if (fx != null)
                fx.volume = volumenFXActual;
        }
    }

    /// <summary>
    /// Actualiza el texto que muestra el volumen de los FX como porcentaje (ej. "80%").
    /// </summary>
    void ActualizarTextoVolumenFX()
    {
        if (textoPorcentajeVolumenFX != null)
        {
            int porcentaje = Mathf.RoundToInt(volumenFXActual * 100f);
            textoPorcentajeVolumenFX.text = porcentaje + "%";
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
    /// intenta generar el audio con ElevenLabs; si no hay un <see cref="ttsManager"/>
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
        audioSourcePrueba.volume = volumenVozActual;
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
    /// Devuelve el volumen actual de la VOZ.
    /// </summary>
    /// <returns>Un valor entre 0 (silencio) y 1 (volumen máximo).</returns>
    public float ObtenerVolumenVozActual()
    {
        return volumenVozActual;
    }

    /// <summary>
    /// Devuelve el volumen actual de los FX.
    /// </summary>
    /// <returns>Un valor entre 0 (silencio) y 1 (volumen máximo).</returns>
    public float ObtenerVolumenFXActual()
    {
        return volumenFXActual;
    }
}