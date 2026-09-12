using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Clase base para cualquier sistema de texto a voz que pase por el
/// Cloudflare Worker en vez de llamar directo a la API externa. Centraliza
/// la URL del Worker, el secreto compartido, y la lógica de red (armar el
/// POST, mandarlo, descargar el AudioClip resultante). Las clases hijas
/// (<see cref="ElevenLabsTTS"/>, <see cref="OpenIAVoice"/>) solo definen su
/// endpoint dentro del Worker y cómo arman su payload específico.
/// </summary>
public abstract class TTSProxyBase : MonoBehaviour
{
    /// <summary>
    /// URL base de tu Cloudflare Worker, ej: https://proxy-lottery-mg.moisescg.workers.dev
    /// Se comparte entre todos los proveedores de TTS.
    /// </summary>
    [Header("Cloudflare Worker (compartido)")]
    [SerializeField] protected string workerUrl = "https://proxy-lottery-mg.moisescg.workers.dev";

    /// <summary>
    /// Debe coincidir EXACTO con el valor de SHARED_APP_SECRET que pusiste en Cloudflare.
    /// </summary>
    [SerializeField] protected string appSecret = "PON_AQUI_EL_MISMO_VALOR_QUE_EN_CLOUDFLARE";

    /// <summary>
    /// El componente de Unity donde se va a escuchar el audio generado.
    /// Se arrastra desde el Inspector en cada clase hija.
    /// </summary>
    [Header("Audio")]
    public AudioSource audioSource;

    /// <summary>
    /// Cada proveedor define a qué ruta del Worker le pega (ej. "/tts/eleven"
    /// o "/tts/openai"), para que un mismo Worker pueda atender a varios.
    /// </summary>
    protected abstract string Endpoint { get; }

    /// <summary>
    /// Manda el POST al Worker con el JSON ya armado por la clase hija, y
    /// reproduce el AudioClip resultante en <see cref="audioSource"/>.
    /// </summary>
    /// <param name="jsonBody">El cuerpo JSON específico de cada proveedor.</param>
    /// <param name="onError">Se llama si algo falla, con un mensaje descriptivo.</param>
    protected IEnumerator EnviarYReproducir(string jsonBody, System.Action<string> onError = null)
    {
        if (audioSource == null)
        {
            onError?.Invoke("No hay AudioSource asignado.");
            yield break;
        }

        string url = workerUrl.TrimEnd('/') + Endpoint;
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-App-Secret", appSecret);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Error TTS ({Endpoint}): {request.responseCode} - {request.error}");
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

            if (clip == null)
            {
                onError?.Invoke("No se pudo convertir la respuesta en AudioClip.");
                yield break;
            }

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
