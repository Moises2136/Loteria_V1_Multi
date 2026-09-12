using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Este script se encarga de convertir texto en voz usando el servicio de ElevenLabs,
/// pero a través de un proxy propio en Cloudflare Workers — así la llave secreta de
/// ElevenLabs nunca viaja dentro de la app, solo vive en el servidor.
/// </summary>
public class ElevenLabsTTS : MonoBehaviour
{
    // =========================
    // GATEWAY (CLOUDFLARE)
    // =========================

    /// <summary>URL de tu Worker de Cloudflare que actúa como intermediario seguro.</summary>
    [Header("Gateway (Cloudflare)")]
    public string gatewayUrl = "https://loteria-api-gateway.moisescg.workers.dev/elevenlabs-tts";

    // =========================
    // VOCES
    // =========================

    /// <summary>ID de la voz femenina disponible.</summary>
    [Header("Voice IDs")]
    public string femaleVoiceId = "EXAVITQu4vr4xnSDxMaL";

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
    /// Estructura que agrupa los ajustes de voz que se le mandan al gateway.
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
    /// Estructura con toda la información que se le envía al gateway de Cloudflare
    /// (que a su vez la reenvía a ElevenLabs con la llave secreta).
    /// </summary>
    [Serializable]
    public class TTSRequest
    {
        public string voiceId;
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
    /// Arma la petición y la manda a tu gateway de Cloudflare (no directo a ElevenLabs),
    /// y reproduce el audio que llega de vuelta.
    /// </summary>
    /// <param name="text">El texto que se va a convertir en audio.</param>
    private IEnumerator GenerateAndPlay(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        if (audioSource == null)
        {
            Debug.LogError("No hay AudioSource asignado.");
            yield break;
        }

        // Armamos los datos que se van a enviar al gateway (incluye el voiceId,
        // que antes iba en la URL directa de ElevenLabs)
        TTSRequest requestData = new TTSRequest
        {
            voiceId = currentVoiceId,
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

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        // La petición ahora va a TU gateway, no directo a ElevenLabs
        using (UnityWebRequest request = new UnityWebRequest(gatewayUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerAudioClip(gatewayUrl, AudioType.MPEG);

            request.SetRequestHeader("Content-Type", "application/json");
            // Ya NO se manda la llave de ElevenLabs aquí — vive segura en Cloudflare

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error TTS (gateway): " + request.responseCode + " - " + request.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

            if (clip == null)
            {
                Debug.LogError("No se pudo convertir la respuesta en AudioClip.");
                yield break;
            }

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();

            Debug.Log("Audio reproducido correctamente.");
        }
    }
}