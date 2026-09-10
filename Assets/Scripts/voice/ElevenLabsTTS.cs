using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Este script se encarga de convertir texto en voz usando el servicio de ElevenLabs,
/// y de reproducir ese audio en el juego. También permite elegir entre voz
/// femenina o masculina, y guarda esa preferencia para la próxima vez.
/// </summary>
public class ElevenLabsTTS : MonoBehaviour
{
    // =========================
    // CONFIGURACIÓN ELEVENLABS
    // =========================

    /// <summary>
    /// La clave (API Key) que nos deja usar el servicio de ElevenLabs.
    /// </summary>
    [Header("ElevenLabs")]
    [TextArea] public string apiKey = "TU_API_KEY_AQUI";

    // =========================
    // VOCES
    // =========================

    /// <summary>ID de la voz femenina disponible.</summary>
    [Header("Voice IDs")]
    public string femaleVoiceId = "twZzvyYIl5ntDHC1Vatv";

    /// <summary>ID de la voz masculina disponible.</summary>
    public string maleVoiceId = "ErXwobaYiN019PkySvjV";

    /// <summary>
    /// Guarda qué voz se está usando actualmente (se decide en tiempo real,
    /// no se edita desde el Inspector).
    /// </summary>
    private string currentVoiceId;

    /// <summary>Modelo de voz de ElevenLabs que se va a usar.</summary>
    public string modelId = "eleven_multilingual_v2";

    /// <summary>Idioma en el que se va a generar el audio (ejemplo: "es" para español).</summary>
    public string languageCode = "es";

    // =========================
    // AUDIO
    // =========================

    /// <summary>
    /// El componente de Unity donde se va a escuchar el audio generado.
    /// Se arrastra desde el Inspector.
    /// </summary>
    [Header("Audio")]
    public AudioSource audioSource;

    // =========================
    // AJUSTES DE VOZ
    // =========================

    /// <summary>Qué tan estable/consistente suena la voz (0 = más variable, 1 = más estable).</summary>
    [Header("Voice Settings")]
    [Range(0f, 1f)] public float stability = 0.45f;

    /// <summary>Qué tanto se parece la voz generada a la voz original (0 a 1).</summary>
    [Range(0f, 1f)] public float similarityBoost = 0.8f;

    /// <summary>Qué tanto estilo/expresividad le pone la voz al hablar (0 a 1).</summary>
    [Range(0f, 1f)] public float style = 0.2f;

    /// <summary>Si está activado, mejora la claridad de la voz.</summary>
    public bool useSpeakerBoost = true;

    /// <summary>Qué tan rápido habla la voz (1 = velocidad normal).</summary>
    [Range(0.7f, 1.2f)] public float speed = 0.95f;

    /// <summary>
    /// Estructura que agrupa los ajustes de voz que se le mandan a ElevenLabs.
    /// </summary>
    [Serializable]
    public class VoiceSettings
    {
        public float stability;
        public float similarity_boost;
        public float style;
        public bool use_speaker_boost;
        public float speed;
    }

    /// <summary>
    /// Estructura con toda la información que se le envía a ElevenLabs
    /// para generar el audio (el texto, el modelo, el idioma y los ajustes de voz).
    /// </summary>
    [Serializable]
    public class TTSRequest
    {
        public string text;
        public string model_id;
        public string language_code;
        public VoiceSettings voice_settings;
    }

    /// <summary>
    /// Al iniciar el juego, se elige automáticamente la voz guardada
    /// en las preferencias del jugador.
    /// </summary>
    private void Start()
    {
        SetVoiceByPreference();
        //SpeakText("Bienvenido a la lotería mexicana de plantas medicinales");
    }

    // =========================
    // MANEJO DE VOZ
    // =========================

    /// <summary>
    /// Revisa qué voz eligió el jugador anteriormente (guardada en PlayerPrefs)
    /// y la deja lista para usarse. Si no hay ninguna guardada, usa la femenina por defecto.
    /// </summary>
    public void SetVoiceByPreference()
    {
        string voiceType = PlayerPrefs.GetString("VOICE_TYPE", "female");
        currentVoiceId = voiceType == "male" ? maleVoiceId : femaleVoiceId;
        Debug.Log("Voz actual: " + voiceType + " | Voice ID: " + currentVoiceId);
    }

    /// <summary>
    /// Cambia la voz activa y guarda esa elección para la próxima vez que se abra el juego.
    /// </summary>
    /// <param name="type">Tipo de voz a usar: "male" o "female".</param>
    public void SetVoice(string type)
    {
        PlayerPrefs.SetString("VOICE_TYPE", type);
        PlayerPrefs.Save();
        SetVoiceByPreference();
    }

    // =========================
    // GENERAR Y REPRODUCIR AUDIO
    // =========================

    /// <summary>
    /// Llama a esta función cuando quieras que un texto se convierta en voz y se reproduzca.
    /// </summary>
    /// <param name="text">El texto que se va a convertir en audio.</param>
    public void SpeakText(string text)
    {
        StartCoroutine(GenerateAndPlay(text));
    }

    /// <summary>
    /// Se encarga de armar la solicitud, mandarla a ElevenLabs,
    /// y reproducir el audio que llega de vuelta.
    /// </summary>
    /// <param name="text">El texto que se va a convertir en audio.</param>
    private IEnumerator GenerateAndPlay(string text)
    {
        // Si no hay texto, no hay nada que convertir
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        // Si no hay AudioSource asignado, no podemos reproducir nada
        if (audioSource == null)
        {
            Debug.LogError("No hay AudioSource asignado.");
            yield break;
        }

        // Armamos la URL con la voz que está activa en este momento
        string url = $"https://api.elevenlabs.io/v1/text-to-speech/{currentVoiceId}?output_format=mp3_44100_128";

        // Armamos los datos que se van a enviar (texto, modelo, idioma y ajustes de voz)
        TTSRequest requestData = new TTSRequest
        {
            text = text,
            model_id = modelId,
            language_code = languageCode,
            voice_settings = new VoiceSettings
            {
                stability = stability,
                similarity_boost = similarityBoost,
                style = style,
                use_speaker_boost = useSpeakerBoost,
                speed = speed
            }
        };

        // Convertimos los datos a JSON y luego a bytes para poder enviarlos
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        // Creamos la solicitud POST hacia ElevenLabs
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("xi-api-key", apiKey);
            request.SetRequestHeader("Accept", "audio/mpeg");

            // Enviamos la solicitud y esperamos la respuesta sin trabar el juego
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error ElevenLabs TTS: " + request.responseCode + " - " + request.error);
                yield break;
            }

            // Convertimos la respuesta en un clip de audio que Unity puede reproducir
            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

            if (clip == null)
            {
                Debug.LogError("No se pudo convertir la respuesta en AudioClip.");
                yield break;
            }

            // Reproducimos el audio
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();

            Debug.Log("Audio reproducido correctamente.");
        }
    }
}