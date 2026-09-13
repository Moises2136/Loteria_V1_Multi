using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Este script se encarga de subir imágenes a Cloudinary, pero a través de un
/// proxy propio en Cloudflare Workers — así el nombre de la cuenta y el preset
/// de subida no quedan visibles directo en la app, y se puede controlar el
/// acceso desde el servidor.
/// </summary>
public class CloudinaryUploader : MonoBehaviour
{
    /// <summary>URL de tu Worker de Cloudflare que actúa como intermediario.</summary>
    [Header("Gateway (Cloudflare)")]
    public string gatewayUrl = "https://loteria-api-gateway.moisescg.workers.dev/cloudinary-upload";

    /// <summary>
    /// Sube una imagen al gateway a partir de su ruta en el disco.
    /// </summary>
    /// <param name="filePath">Ruta local del archivo de imagen a subir.</param>
    /// <param name="onSuccess">Se llama si la subida sale bien. Manda la URL final de la imagen.</param>
    /// <param name="onError">Se llama si algo falla. Manda un mensaje explicando el error.</param>
    public IEnumerator UploadImageFromPath(string filePath, Action<string> onSuccess, Action<string> onError)
    {
        Debug.Log("=== CLOUDINARY UPLOADER (via gateway) ===");

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            onError?.Invoke("Ruta inválida para imagen.");
            yield break;
        }

        byte[] imageBytes = File.ReadAllBytes(filePath);
        string publicId = "card_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

        yield return EnviarAlGateway(imageBytes, publicId, onSuccess, onError);
    }

    /// <summary>
    /// Sube una imagen al gateway directamente desde una textura en memoria.
    /// </summary>
    /// <param name="texture">La textura que se va a convertir a imagen y subir.</param>
    /// <param name="onSuccess">Se llama si la subida sale bien. Manda la URL final de la imagen.</param>
    /// <param name="onError">Se llama si algo falla. Manda un mensaje explicando el error.</param>
    public IEnumerator UploadTexture(Texture2D texture, Action<string> onSuccess, Action<string> onError)
    {
        Debug.Log("=== CLOUDINARY UPLOADER TEXTURE (via gateway) ===");

        if (texture == null)
        {
            onError?.Invoke("La textura a subir es null.");
            yield break;
        }

        byte[] imageBytes = texture.EncodeToPNG();
        string publicId = "card_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

        yield return EnviarAlGateway(imageBytes, publicId, onSuccess, onError);
    }

    /// <summary>
    /// Envía los bytes de la imagen a tu gateway de Cloudflare, que se encarga
    /// de reenviarlos a Cloudinary con el cloud name y el preset correctos.
    /// </summary>
    private IEnumerator EnviarAlGateway(byte[] imageBytes, string publicId, Action<string> onSuccess, Action<string> onError)
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageBytes, publicId + ".png", "image/png");
        form.AddField("public_id", publicId);

        using (UnityWebRequest request = UnityWebRequest.Post(gatewayUrl, form))
        {
            yield return request.SendWebRequest();

            Debug.Log("Gateway response code: " + request.responseCode);
            Debug.Log("Gateway raw response: " + request.downloadHandler.text);

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke("Error subiendo imagen: " + request.responseCode + " - " + request.error + "\n" + request.downloadHandler.text);
                yield break;
            }

            CloudinaryResponse response = JsonUtility.FromJson<CloudinaryResponse>(request.downloadHandler.text);

            if (response == null || string.IsNullOrWhiteSpace(response.secure_url))
            {
                onError?.Invoke("No se recibió secure_url.\n" + request.downloadHandler.text);
                yield break;
            }

            Debug.Log("SUBIDA OK: " + response.secure_url);
            onSuccess?.Invoke(response.secure_url);
        }
    }

    /// <summary>
    /// Estructura que representa la respuesta que manda Cloudinary
    /// después de subir una imagen: incluye la URL final y su ID público.
    /// </summary>
    [Serializable]
    private class CloudinaryResponse
    {
        public string secure_url;
        public string public_id;
    }
}