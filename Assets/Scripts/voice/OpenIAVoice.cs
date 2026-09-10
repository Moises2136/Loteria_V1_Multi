using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Esta clase representa la información que le mandamos a OpenAI
/// para pedirle que convierta un texto en audio (voz).
/// </summary>
[System.Serializable]
public class TTSPayload
{
    /// <summary>Modelo de voz que vamos a usar (por ejemplo "gpt-4o-mini-tts").</summary>
    public string model;

    /// <summary>Qué voz queremos que use (por ejemplo "alloy", "nova", etc).</summary>
    public string voice;

    /// <summary>El texto que se va a convertir en audio.</summary>
    public string input;

    /// <summary>Instrucciones extra opcionales, por ejemplo "habla más lento".</summary>
    public string instructions;

    /// <summary>Formato en el que queremos recibir el audio (ejemplo: "wav").</summary>
    public string format;
}

/// <summary>
/// Este script se encarga de pedirle a OpenAI que convierta texto en voz,
/// descargar ese audio, y reproducirlo en el juego.
/// </summary>
public class OpenIAVoice : MonoBehaviour
{
    // =========================
    // CONFIGURACIÓN OPENAI
    // =========================

    /// <summary>
    /// La clave (API Key) que nos deja usar el servicio de OpenAI.
    /// </summary>
    [Header("OpenAI")]
    [TextArea]
    public string apiKey;

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
    // MODELO
    // =========================

    /// <summary>
    /// Nombre del modelo de voz que se va a usar por defecto.
    /// </summary>
    [Header("Modelo")]
    public string model = "gpt-4o-mini-tts";

    // =========================
    // MÉTODO PÚBLICO
    // =========================

    /// <summary>
    /// Llama a esta función cuando quieras que un texto se convierta en voz y se reproduzca.
    /// </summary>
    /// <param name="texto">Lo que quieres que se escuche.</param>
    /// <param name="voz">Qué voz usar.</param>
    /// <param name="instrucciones">Opcional: cómo quieres que suene (tono, velocidad, etc).</param>
    public void GenerarYReproducir(string texto, string voz, string instrucciones = "")
    {
        // Arranca el proceso en segundo plano (coroutine) para no trabar el juego
        StartCoroutine(GenerarAudioCoroutine(texto, voz, instrucciones));
    }

    // =========================
    // PETICIÓN A OPENAI
    // =========================

    /// <summary>
    /// Se encarga de armar la petición, mandarla a OpenAI,
    /// y esperar a que llegue el audio de vuelta.
    /// </summary>
    private IEnumerator GenerarAudioCoroutine(string texto, string voz, string instrucciones)
    {
        // Si no hay API key configurada, no podemos continuar
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogError("Falta la API key.");
            yield break;
        }

        // Si no hay AudioSource asignado, tampoco podemos continuar
        if (audioSource == null)
        {
            Debug.LogError("Falta asignar AudioSource.");
            yield break;
        }

        // Armamos el paquete de datos (payload) que se va a enviar
        TTSPayload payload = new TTSPayload
        {
            model = model,
            voice = voz,
            input = texto,
            instructions = instrucciones,
            format = "wav"
        };

        // Convertimos el payload a texto JSON y luego a bytes, que es como se manda por internet
        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        // Creamos la solicitud (request) tipo POST hacia la API de OpenAI
        using (UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/audio/speech", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            // Enviamos la solicitud y esperamos la respuesta sin trabar el juego
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                // Si algo salió mal, lo mostramos en la consola
                Debug.LogError("Error TTS: " + request.error);
                Debug.LogError("Respuesta: " + request.downloadHandler.text);
                yield break;
            }

            // Si todo salió bien, obtenemos los bytes del audio generado
            byte[] audioData = request.downloadHandler.data;

            // Guardamos ese audio y lo reproducimos
            yield return StartCoroutine(CargarWavDesdeBytes(audioData));
        }
    }

    // =========================
    // CARGA Y REPRODUCCIÓN DEL WAV
    // =========================

    /// <summary>
    /// Guarda el audio recibido como archivo temporal,
    /// lo carga en Unity y lo reproduce.
    /// </summary>
    /// <param name="wavBytes">Los datos del audio en formato WAV.</param>
    private IEnumerator CargarWavDesdeBytes(byte[] wavBytes)
    {
        // Definimos dónde se va a guardar el archivo temporalmente
        string tempPath = System.IO.Path.Combine(Application.persistentDataPath, "tts_temp.wav");

        // Guardamos el archivo de audio en esa ruta
        System.IO.File.WriteAllBytes(tempPath, wavBytes);

        // Convertimos la ruta local en una URL que Unity pueda leer
        string fileUrl = "file://" + tempPath;

        // Cargamos el archivo de audio desde esa URL
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.WAV))
        {
            yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (www.result != UnityWebRequest.Result.Success)
#else
            if (www.isNetworkError || www.isHttpError)
#endif
            {
                Debug.LogError("Error cargando WAV: " + www.error);
                yield break;
            }

            // Obtenemos el clip de audio y lo reproducimos
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}