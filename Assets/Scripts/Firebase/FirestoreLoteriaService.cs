using System;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;

/// <summary>
/// Este script se encarga de toda la comunicación con Firestore (la base de datos)
/// relacionada con las loterías: crearlas, guardar sus cartas, y cargar tanto
/// las loterías como sus cartas desde la nube. Es un singleton, así que solo
/// existe una instancia y se puede acceder desde cualquier parte usando
/// <c>FirestoreLoteriaService.Instance</c>.
/// </summary>
public class FirestoreLoteriaService : MonoBehaviour
{
    /// <summary>
    /// La única instancia de este script que existe en el juego.
    /// </summary>
    public static FirestoreLoteriaService Instance { get; private set; }

    /// <summary>
    /// Referencia a la base de datos de Firestore. Se usa internamente
    /// en todas las funciones para leer y guardar información.
    /// </summary>
    private FirebaseFirestore db;

    /// <summary>
    /// Se ejecuta al cargar el script. Aquí se asegura de que solo exista
    /// una instancia (patrón singleton), evita que se destruya al cambiar
    /// de escena, y se conecta con Firestore.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            db = FirebaseFirestore.DefaultInstance;
            Debug.Log("FirestoreLoteriaService listo.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error obteniendo Firestore.DefaultInstance: " + ex);
        }
    }

    /// <summary>
    /// Indica si la conexión con Firestore ya está lista para usarse.
    /// </summary>
    /// <returns>Verdadero si ya se puede leer/guardar en la base de datos.</returns>
    public bool EstaListo()
    {
        return db != null;
    }

    // =========================
    // CREAR LOTERÍA
    // =========================

    /// <summary>
    /// Crea una nueva lotería en Firestore con el nombre indicado.
    /// </summary>
    /// <param name="nombre">Nombre que va a tener la lotería.</param>
    /// <param name="onSuccess">Se llama si todo sale bien. Manda el ID de la lotería creada.</param>
    /// <param name="onError">Se llama si algo falla. Manda un mensaje explicando el error.</param>
    public void CrearLoteria(string nombre, Action<string> onSuccess, Action<string> onError)
    {
        if (!EstaListo())
        {
            onError?.Invoke("Firestore no está listo.");
            return;
        }

        // Buscamos el uid del usuario actual guardado localmente
        string uid = PlayerPrefs.GetString("uid", "");
        if (string.IsNullOrWhiteSpace(uid))
        {
            onError?.Invoke("No hay uid guardado en PlayerPrefs.");
            return;
        }

        // Armamos los datos que se van a guardar en Firestore
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "nombre", nombre },
            { "creadorUid", uid },
            { "creadoEn", Timestamp.GetCurrentTimestamp() }
        };

        // Guardamos el nuevo documento en la colección "loterias"
        db.Collection("loterias")
          .AddAsync(data)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted || task.IsCanceled)
              {
                  Debug.LogError("Error creando lotería: " + task.Exception);
                  onError?.Invoke("No se pudo crear la lotería.");
                  return;
              }

              // Firestore genera automáticamente un ID para el documento nuevo
              string loteriaDocId = task.Result.Id;
              Debug.Log("Lotería creada con docId: " + loteriaDocId);
              onSuccess?.Invoke(loteriaDocId);
          });
    }

    // =========================
    // GUARDAR CARTA
    // =========================

    /// <summary>
    /// Guarda una carta dentro de una lotería específica en Firestore.
    /// </summary>
    /// <param name="loteriaDocId">ID de la lotería a la que pertenece la carta.</param>
    /// <param name="carta">Los datos de la carta que se van a guardar.</param>
    /// <param name="onSuccess">Se llama si la carta se guardó correctamente.</param>
    /// <param name="onError">Se llama si algo falla. Manda un mensaje explicando el error.</param>
    public void GuardarCarta(string loteriaDocId, CartaData carta, Action onSuccess, Action<string> onError)
    {
        if (!EstaListo())
        {
            onError?.Invoke("Firestore no está listo.");
            return;
        }

        if (string.IsNullOrWhiteSpace(loteriaDocId))
        {
            onError?.Invoke("loteriaDocId vacío.");
            return;
        }

        // Armamos los datos de la carta que se van a guardar
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "id", carta.id },
            { "nombre", carta.nombre },
            { "descripcion", carta.descripcion },
            { "rutaImagen", carta.rutaImagen }
        };

        // La carta se guarda dentro de la subcolección "cartas" de su lotería
        db.Collection("loterias")
          .Document(loteriaDocId)
          .Collection("cartas")
          .Document(carta.id.ToString())
          .SetAsync(data)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted || task.IsCanceled)
              {
                  Debug.LogError("Error guardando carta: " + task.Exception);
                  onError?.Invoke("No se pudo guardar la carta.");
                  return;
              }

              Debug.Log("Carta guardada en Firestore: " + carta.nombre);
              onSuccess?.Invoke();
          });
    }

    // =========================
    // CARGAR LOTERÍAS
    // =========================

    /// <summary>
    /// Carga la lista de todas las loterías guardadas en Firestore.
    /// </summary>
    /// <param name="onSuccess">Se llama si todo sale bien. Manda una lista con el ID y el nombre de cada lotería.</param>
    /// <param name="onError">Se llama si algo falla. Manda un mensaje explicando el error.</param>
    public void CargarLoterias(Action<List<(string docId, string nombre)>> onSuccess, Action<string> onError)
    {
        if (!EstaListo())
        {
            onError?.Invoke("Firestore no está listo.");
            return;
        }

        db.Collection("loterias")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted || task.IsCanceled)
              {
                  Debug.LogError("Error cargando loterías: " + task.Exception);
                  onError?.Invoke("No se pudieron cargar las loterías.");
                  return;
              }

              List<(string docId, string nombre)> resultado = new List<(string docId, string nombre)>();

              // Recorremos cada documento (lotería) que llegó de Firestore
              foreach (DocumentSnapshot doc in task.Result.Documents)
              {
                  // Si el documento no tiene nombre guardado, usamos un valor por defecto
                  string nombre = doc.ContainsField("nombre")
                      ? doc.GetValue<string>("nombre")
                      : "Sin nombre";

                  resultado.Add((doc.Id, nombre));
              }

              onSuccess?.Invoke(resultado);
          });
    }

    // =========================
    // CARGAR CARTAS DE UNA LOTERÍA
    // =========================

    /// <summary>
    /// Carga todas las cartas que pertenecen a una lotería específica.
    /// </summary>
    /// <param name="loteriaDocId">ID de la lotería de la que se quieren cargar las cartas.</param>
    /// <param name="onSuccess">Se llama si todo sale bien. Manda la lista de cartas encontradas.</param>
    /// <param name="onError">Se llama si algo falla. Manda un mensaje explicando el error.</param>
    public void CargarCartasDeLoteria(string loteriaDocId, Action<List<CartaData>> onSuccess, Action<string> onError)
    {
        if (!EstaListo())
        {
            onError?.Invoke("Firestore no está listo.");
            return;
        }

        db.Collection("loterias")
          .Document(loteriaDocId)
          .Collection("cartas")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted || task.IsCanceled)
              {
                  Debug.LogError("Error cargando cartas: " + task.Exception);
                  onError?.Invoke("No se pudieron cargar las cartas.");
                  return;
              }

              List<CartaData> cartas = new List<CartaData>();

              // Recorremos cada documento (carta) que llegó de Firestore
              foreach (DocumentSnapshot doc in task.Result.Documents)
              {
                  CartaData carta = new CartaData();

                  // Solo llenamos los campos que sí vienen en el documento,
                  // así evitamos errores si algún dato falta
                  if (doc.ContainsField("id")) carta.id = Convert.ToInt32(doc.GetValue<long>("id"));
                  if (doc.ContainsField("nombre")) carta.nombre = doc.GetValue<string>("nombre");
                  if (doc.ContainsField("descripcion")) carta.descripcion = doc.GetValue<string>("descripcion");
                  if (doc.ContainsField("rutaImagen")) carta.rutaImagen = doc.GetValue<string>("rutaImagen");

                  cartas.Add(carta);
              }

              onSuccess?.Invoke(cartas);
          });
    }
}