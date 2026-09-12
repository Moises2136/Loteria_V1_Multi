using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Este script mantiene en memoria (caché) todas las cartas de la lotería
/// actual, ya con sus imágenes descargadas, para que cualquier panel
/// (sala de espera, juego Solo, juego Multijugador) pueda usarlas de
/// inmediato sin tener que volver a pedirlas a Firestore ni descargar
/// las imágenes de nuevo cada vez. Es un singleton, así que solo existe
/// una instancia y se puede acceder desde cualquier parte usando
/// <c>LoteriaDataCache.Instance</c>.
/// </summary>
public class LoteriaDataCache : MonoBehaviour
{
    /// <summary>
    /// La única instancia de este script que existe en el juego.
    /// </summary>
    public static LoteriaDataCache Instance;

    /// <summary>
    /// Representa una carta ya lista para usarse: sus datos básicos más
    /// su imagen ya descargada (a diferencia de <c>CartaData</c>, que
    /// solo trae la URL de la imagen tal como viene de Firestore).
    /// </summary>
    public class CartaCache
    {
        /// <summary>ID de la carta.</summary>
        public int id;

        /// <summary>Nombre de la carta.</summary>
        public string nombre;

        /// <summary>Descripción de la carta.</summary>
        public string descripcion;

        /// <summary>Imagen de la carta, ya descargada y lista para mostrarse.</summary>
        public Sprite sprite;
    }

    /// <summary>Todas las cartas actualmente en caché, indexadas por su ID.</summary>
    public Dictionary<int, CartaCache> Cartas { get; private set; } = new Dictionary<int, CartaCache>();

    /// <summary>Indica si el caché ya terminó de cargar y está listo para usarse.</summary>
    public bool Listo { get; private set; } = false;

    /// <summary>Indica si el caché está actualmente en proceso de carga.</summary>
    public bool Cargando { get; private set; } = false;

    /// <summary>Cuántas cartas hay actualmente en el caché.</summary>
    public int TotalCartas => Cartas.Count;

    /// <summary>Referencia al script que descarga las imágenes de las cartas desde sus URLs.</summary>
    [SerializeField] private RemoteImageLoader remoteImageLoader;

    /// <summary>Se dispara cuando el caché termina de cargar exitosamente todas las cartas.</summary>
    public event System.Action OnCacheListoOK;

    /// <summary>Se dispara si algo falla durante la carga. Manda un mensaje describiendo el error.</summary>
    public event System.Action<string> OnCacheError;

    /// <summary>
    /// Se ejecuta al cargar el script. Aquí se asegura de que solo exista
    /// una instancia de este manager en todo el juego (patrón singleton),
    /// y evita que se destruya al cambiar de escena.
    /// </summary>
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Pide que se carguen las cartas de una lotería, pero solo si el
    /// caché no estaba ya listo o ya cargando. Si ya estaba listo,
    /// avisa de inmediato que todo está bien (sin volver a descargar nada).
    /// </summary>
    /// <param name="loteriaDocId">ID de la lotería cuyas cartas se quieren cargar.</param>
    public void CargarSiNecesario(string loteriaDocId)
    {
        if (Listo)
        {
            OnCacheListoOK?.Invoke();
            return;
        }

        if (Cargando) return;

        Cargando = true;
        Listo = false;
        Cartas.Clear();

        StartCoroutine(CargarCoroutine(loteriaDocId));
    }

    /// <summary>
    /// Hace todo el proceso de carga en dos pasos: primero pide la lista
    /// de cartas a Firestore, y después descarga todas sus imágenes en
    /// paralelo. Cuando todo termina, marca el caché como listo y avisa
    /// mediante <see cref="OnCacheListoOK"/>.
    /// </summary>
    /// <param name="loteriaDocId">ID de la lotería cuyas cartas se van a cargar.</param>
    private IEnumerator CargarCoroutine(string loteriaDocId)
    {
    // Paso 1 — pedir lista de cartas a Firestore (una sola vez)
    bool firestoreListo = false;
    string errorFirestore = null;
    List<CartaData> cartasFirestore = null; // CartaData ya existe en tu proyecto

    FirestoreLoteriaService.Instance.CargarCartasDeLoteria(
        loteriaDocId,
        cartas => { cartasFirestore = cartas; firestoreListo = true; },
        error => { errorFirestore = error; firestoreListo = true; }
    );

    yield return new WaitUntil(() => firestoreListo);

    if (errorFirestore != null)
        {
        Cargando = false;
        OnCacheError?.Invoke(errorFirestore);
        yield break;
        }

    if (cartasFirestore == null || cartasFirestore.Count == 0)
        {
        Cargando = false;
        OnCacheError?.Invoke("La lotería no tiene cartas.");
        yield break;
        }

    // Paso 2 — descargar todas las imágenes en paralelo
    int pendientes = cartasFirestore.Count;

    foreach (var c in cartasFirestore)
        {
        var entrada = new CartaCache
            {
            id = c.id,
            nombre = c.nombre,
            descripcion = c.descripcion,
            sprite = null
            };
        Cartas[c.id] = entrada;

        if (string.IsNullOrWhiteSpace(c.rutaImagen))
            {
            pendientes--;
            continue;
            }

        // Captura local para el closure
        CartaCache entradaLocal = entrada;

        StartCoroutine(remoteImageLoader.LoadImage(
            c.rutaImagen,
            null,            // no necesitamos Image de destino aquí
            sprite =>
            {
                entradaLocal.sprite = sprite;
                pendientes--;
            }
        ));
        }

    // NUEVO: esperamos a que todas las imágenes terminen, pero con un límite
    // de tiempo. Si alguna imagen falla sin avisar (sin llamar al callback),
    // esto evita que el caché se quede "cargando" para siempre.
    float tiempoEsperado = 0f;
    const float TIEMPO_MAXIMO_ESPERA = 15f;

    while (pendientes > 0 && tiempoEsperado < TIEMPO_MAXIMO_ESPERA)
    {
        tiempoEsperado += Time.deltaTime;
        yield return null;
    }

    if (pendientes > 0)
    {
        Debug.LogWarning($"LoteriaDataCache: {pendientes} imagen(es) no terminaron de descargar a tiempo. Se continúa con las que sí cargaron.");
    }

    Cargando = false;
    Listo = true;
    OnCacheListoOK?.Invoke();
    }

    /// <summary>
    /// Obtiene una carta específica del caché por su ID.
    /// </summary>
    /// <param name="id">ID de la carta que se quiere obtener.</param>
    /// <returns>La carta en caché, o <c>null</c> si no existe con ese ID.</returns>
    public CartaCache ObtenerCarta(int id)
    {
        Cartas.TryGetValue(id, out var carta);
        return carta;
    }

    /// <summary>
    /// Borra todas las cartas del caché y lo deja marcado como no listo
    /// (útil para forzar una recarga la próxima vez que se pida, por
    /// ejemplo al cambiar de lotería).
    /// </summary>
    public void Limpiar()
    {
        Cartas.Clear();
        Listo = false;
        Cargando = false;
    }
}