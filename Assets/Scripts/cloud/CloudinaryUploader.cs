using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Este script se encarga de subir imágenes a Cloudinary (un servicio en la nube
/// para guardar imágenes) y devolver la URL donde quedó guardada la imagen.
/// Ofrece dos formas de subir: desde un archivo en el disco, o directamente
/// desde una textura que ya está en memoria.
/// </summary>
public class CloudinaryUploader : MonoBehaviour
{
    // =========================
    // CONFIGURACIÓN CLOUDINARY
    // =========================

    /// <summary>Nombre de la cuenta ("cloud") de Cloudinary donde se suben las imágenes.</summary>
    [Header("Cloudinary")]
    public string cloudName = "de3tdxblk";

    /// <summary>
    /// Nombre del "preset" de subida sin firma, configurado en Cloudinary,
    /// que define permisos y ajustes para las imágenes que se suben.
    /// </summary>
    public string unsignedUploadPreset = "ecoloteria";

    /// <summary>Carpeta dentro de Cloudinary donde se van a guardar las imágenes.</summary>
    public string folder = "ecoloteria/cartas";

    // =========================
    // SUBIR DESDE UN ARCHIVO
    // =========================

    /// <summary>
    /// Sube una imagen a Cloudinary a partir de su ruta en el disco.
    /// </summary>
    /// <param name="filePath">Ruta local del archivo de imagen a subir.</param>
    /// <param name="onSuccess">Se llama si la subida sale bien. Manda la URL final de la imagen.</param>
    /// <param name="onError">Se llama si algo falla. Manda un mensaje explicando el error.</param>
    /// <remarks>
    /// Este método se ejecuta como coroutine, por lo que debe llamarse con <c>StartCoroutine(...)</c>.
    /// </remarks>
    public IEnumerator UploadImageFromPath(string filePath, Action<string> onSuccess, Action<string> onError)
    {
        Debug.Log("=== CLOUDINARY UPLOADER ===");

        // Si la ruta no existe o está vacía, no hay nada que subir
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            onError?.Invoke("Ruta inválida para imagen.");
            yield break;
        }

        // Leemos el archivo de imagen como bytes
        byte[] imageBytes = File.ReadAllBytes(filePath);
        string uploadUrl = "https://api.cloudinary.com/v1_1/" + cloudName + "/image/upload";

        // Generamos un ID único para esta imagen, basado en la fecha y hora actual
        string publicId = "card_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // Armamos el formulario que se va a enviar a Cloudinary
        WWWForm form = new WWWForm();
        form.AddField("upload_preset", unsignedUploadPreset);
        form.AddField("folder", folder);
        form.AddField("public_id", publicId);
        form.AddBinaryData("file", imageBytes, Path.GetFileName(filePath));

        Debug.Log("Cloudinary URL: " + uploadUrl);
        Debug.Log("Preset: " + unsignedUploadPreset);
        Debug.Log("Folder: " + folder);
        Debug.Log("Public ID: " + publicId);

        // Enviamos la imagen a Cloudinary
        using (UnityWebRequest request = UnityWebRequest.Post(uploadUrl, form))
        {
            yield return request.SendWebRequest();

            Debug.Log("Cloudinary response code: " + request.responseCode);
            Debug.Log("Cloudinary raw response: " + request.downloadHandler.text);

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(
                    "Error Cloudinary: " +
                    request.responseCode +
                    " - " +
                    request.error +
                    "\n" +
                    request.downloadHandler.text
                );
                yield break;
            }

            // Convertimos la respuesta JSON de Cloudinary en un objeto que podamos leer
            CloudinaryResponse response = JsonUtility.FromJson<CloudinaryResponse>(request.downloadHandler.text);

            if (response == null || string.IsNullOrWhiteSpace(response.secure_url))
            {
                onError?.Invoke("Cloudinary no devolvió secure_url.\n" + request.downloadHandler.text);
                yield break;
            }

            Debug.Log("SUBIDA OK: " + response.secure_url);
            onSuccess?.Invoke(response.secure_url);
        }
    }

    // =========================
    // SUBIR DESDE UNA TEXTURA
    // =========================

    /// <summary>
    /// Sube una imagen a Cloudinary directamente desde una textura que ya está en memoria
    /// (sin necesidad de tener un archivo guardado en el disco).
    /// </summary>
    /// <param name="texture">La textura que se va a convertir a imagen y subir.</param>
    /// <param name="onSuccess">Se llama si la subida sale bien. Manda la URL final de la imagen.</param>
    /// <param name="onError">Se llama si algo falla. Manda un mensaje explicando el error.</param>
    /// <remarks>
    /// Este método se ejecuta como coroutine, por lo que debe llamarse con <c>StartCoroutine(...)</c>.
    /// </remarks>
    public IEnumerator UploadTexture(Texture2D texture, Action<string> onSuccess, Action<string> onError)
    {
        Debug.Log("=== CLOUDINARY UPLOADER TEXTURE ===");

        // Si no hay textura, no hay nada que subir
        if (texture == null)
        {
            onError?.Invoke("La textura a subir es null.");
            yield break;
        }

        // Convertimos la textura a formato PNG en bytes
        byte[] imageBytes = texture.EncodeToPNG();
        string uploadUrl = "https://api.cloudinary.com/v1_1/" + cloudName + "/image/upload";
        string publicId = "card_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // Armamos el formulario que se va a enviar a Cloudinary
        WWWForm form = new WWWForm();
        form.AddField("upload_preset", unsignedUploadPreset);
        form.AddField("folder", folder);
        form.AddField("public_id", publicId);
        form.AddBinaryData("file", imageBytes, publicId + ".png", "image/png");

        Debug.Log("Cloudinary URL: " + uploadUrl);
        Debug.Log("Preset: " + unsignedUploadPreset);
        Debug.Log("Folder: " + folder);
        Debug.Log("Public ID: " + publicId);

        // Enviamos la imagen a Cloudinary
        using (UnityWebRequest request = UnityWebRequest.Post(uploadUrl, form))
        {
            yield return request.SendWebRequest();

            Debug.Log("Cloudinary response code: " + request.responseCode);
            Debug.Log("Cloudinary raw response: " + request.downloadHandler.text);

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke("Error Cloudinary: " + request.responseCode + " - " + request.error + "\n" + request.downloadHandler.text);
                yield break;
            }

            // Convertimos la respuesta JSON de Cloudinary en un objeto que podamos leer
            CloudinaryResponse response = JsonUtility.FromJson<CloudinaryResponse>(request.downloadHandler.text);

            if (response == null || string.IsNullOrWhiteSpace(response.secure_url))
            {
                onError?.Invoke("Cloudinary no devolvió secure_url.\n" + request.downloadHandler.text);
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
        /// <summary>URL segura (https) donde quedó guardada la imagen.</summary>
        public string secure_url;

        /// <summary>ID público que Cloudinary le asignó a la imagen.</summary>
        public string public_id;
    }
}