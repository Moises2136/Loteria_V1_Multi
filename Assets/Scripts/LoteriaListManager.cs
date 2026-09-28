using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Muestra en el menú principal las tarjetas de las loterías jugadas
/// recientemente (hasta 3), con su nombre real, cantidad de cartas, y
/// una miniatura tomada de cualquiera de sus cartas. Los botones de
/// Editar/Jugar de cada tarjeta no hacen nada todavía (se dejan tal
/// cual vienen en CardLoteriaUI).
/// </summary>
public class LoteriaListManager : MonoBehaviour
{
    /// <summary>Contenedor donde se instancian las tarjetas de lotería.</summary>
    public Transform contenedor;

    /// <summary>Prefab de la tarjeta de lotería (debe tener CardLoteriaUI).</summary>
    public GameObject prefabLoteria;

    /// <summary>Usado para descargar la imagen de miniatura de cada tarjeta.</summary>
    public RemoteImageLoader remoteImageLoader;

    void Start()
    {
        CargarRecientes();
    }

    /// <summary>
    /// Borra las tarjetas actuales y vuelve a armar la lista con las
    /// loterías jugadas recientemente.
    /// </summary>
    public void CargarRecientes()
    {
        foreach (Transform hijo in contenedor)
            Destroy(hijo.gameObject);

        List<string> recientes = RecentLoteriasManager.ObtenerRecientes();

        foreach (string docId in recientes)
            CargarUnaReciente(docId);
    }

    /// <summary>
    /// Pide a Firestore los datos de una lotería reciente (nombre y sus
    /// cartas) para poder armar su tarjeta.
    /// </summary>
    void CargarUnaReciente(string docId)
    {
        if (FirestoreLoteriaService.Instance == null) return;

        FirestoreLoteriaService.Instance.CargarLoteriaPorId(docId,
            infoLoteria =>
            {
                FirestoreLoteriaService.Instance.CargarCartasDeLoteria(docId,
                    cartas => MostrarTarjeta(infoLoteria.nombre, infoLoteria.esPublica, cartas),
                    error => Debug.LogWarning("No se pudieron cargar cartas de " + docId + ": " + error)
                );
            },
            error => Debug.LogWarning("No se pudo cargar lotería reciente " + docId + ": " + error)
        );
    }

    /// <summary>
    /// Crea la tarjeta visual de una lotería reciente: nombre, cantidad
    /// de cartas, y una miniatura tomada de cualquiera de sus cartas.
    /// </summary>
    void MostrarTarjeta(string nombre, bool esPublica, List<CartaData> cartas)
    {
       if (contenedor == null || prefabLoteria == null) return;

    GameObject nueva = Instantiate(prefabLoteria, contenedor);
    CardLoteriaUI card = nueva.GetComponent<CardLoteriaUI>();
    if (card == null) return;

    DatosLoteria datos = new DatosLoteria
    {
        nombreLoteria = nombre,
        esPublica = esPublica
    };

    foreach (var c in cartas)
        datos.cartas.Add(new DatosCarta { nombreCarta = c.nombre });

    card.Configurar(datos);

    // Buscamos la primera carta que sí tenga imagen
    string urlPortada = null;
    foreach (var c in cartas)
    {
        if (!string.IsNullOrWhiteSpace(c.rutaImagen))
        {
            urlPortada = c.rutaImagen;
            break;
        }
    }

    if (urlPortada == null)
    {
        Debug.LogWarning($"La lotería '{nombre}' no tiene cartas con imagen.");
        return;
    }

    if (remoteImageLoader == null)
    {
        Debug.LogWarning("remoteImageLoader no está asignado en LoteriaListManager.");
        return;
    }

    StartCoroutine(remoteImageLoader.LoadImage(urlPortada, card.imgPortada, sprite =>
    {
        if (card == null || card.imgPortada == null) return;

        if (sprite != null)
        {
            card.imgPortada.sprite = sprite;
            card.imgPortada.color = Color.white;   // quita el tinte negro
            card.imgPortada.preserveAspect = true;
        }
        else
        {
            Debug.LogWarning("No se pudo descargar la portada: " + urlPortada);
        }
    }));
    }
}