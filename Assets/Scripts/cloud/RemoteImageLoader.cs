using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Este script se encarga de descargar una imagen desde internet (dada su URL)
/// y convertirla en algo que Unity pueda mostrar en pantalla (un Sprite).
/// </summary>
public class RemoteImageLoader : MonoBehaviour
{
    /// <summary>
    /// Descarga una imagen desde una URL y, si todo sale bien,
    /// la asigna a un componente Image y/o avisa mediante un callback.
    /// </summary>
    /// <param name="url">Dirección web de la imagen a descargar.</param>
    /// <param name="target">Componente Image donde se va a mostrar la imagen descargada (opcional).</param>
    /// <param name="onLoaded">Función opcional que se llama cuando la imagen ya está lista, mandando el Sprite generado.</param>
    /// <remarks>
    /// Este método se ejecuta como coroutine, por lo que debe llamarse con <c>StartCoroutine(...)</c>.
    /// Si la URL está vacía o la descarga falla, simplemente se detiene sin lanzar error,
    /// pero se muestra un aviso en la consola.
    /// </remarks>
    public IEnumerator LoadImage(string url, Image target, Action<Sprite> onLoaded = null)
    {
        // Si no hay URL, no hay nada que descargar
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning("URL vacía.");
            yield break;
        }

        // Pedimos la imagen como textura desde la URL
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        // Si algo salió mal en la descarga, avisamos y nos detenemos
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error cargando imagen remota: " + request.error);
            yield break;
        }

        // Convertimos la textura descargada en un Sprite que Unity pueda mostrar
        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        // Si nos dieron un Image, le asignamos el sprite directamente
        if (target != null)
            target.sprite = sprite;

        // Avisamos a quien nos llamó que la imagen ya está lista
        onLoaded?.Invoke(sprite);
    }
}