using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine;

/// <summary>
/// Este script llena un menú desplegable (dropdown) con todas las
/// loterías guardadas en Firestore, para que el jugador elija con cuál
/// quiere jugar. Al elegir una, guarda su ID y nombre localmente, para
/// que el resto del juego (como <c>PanelJuegaLoteriaSoloUI</c> o
/// <c>PanelSalaEsperaAnfitrionUI</c>) sepa cuál lotería está activa.
/// </summary>
public class LoteriaDropdownLoader : MonoBehaviour
{
    /// <summary>El menú desplegable donde se listan las loterías disponibles.</summary>
    [Header("UI")]
    public TMP_Dropdown dropdownLoterias;

    /// <summary>Texto usado para mostrar el estado de la carga (cargando, error, seleccionado).</summary>
    public TMP_Text textoEstado;

    /// <summary>Referencia a la base de datos de Firestore.</summary>
    private FirebaseFirestore db;

    /// <summary>
    /// Lista con los IDs de documento de cada lotería, en el mismo orden
    /// en que aparecen en el dropdown (sin contar la opción "Selecciona
    /// tu lotería" del inicio).
    /// </summary>
    private List<string> loteriaDocIds = new List<string>();

    /// <summary>Llave usada para guardar el ID de la lotería elegida en PlayerPrefs.</summary>
    private const string KEY_LOTERIA_DOC_ID = "loteria_doc_id";

    /// <summary>Llave usada para guardar el nombre de la lotería elegida en PlayerPrefs.</summary>
    private const string KEY_LOTERIA_NOMBRE = "loteria_nombre";

    /// <summary>
    /// Al iniciar, conecta el dropdown con su función de selección.
    /// </summary>
    void Start()
    {
        if (dropdownLoterias != null)
        {
            dropdownLoterias.onValueChanged.RemoveAllListeners();
            dropdownLoterias.onValueChanged.AddListener(OnLoteriaSeleccionada);
        }
    }

    /// <summary>
    /// Cada vez que se abre el panel, vuelve a pedir las loterías. Así, si la
    /// primera carga falló (sin sesión o sin permisos), se reintenta sola.
    /// </summary>
    /// <remarks>
    /// La primera vez, <c>OnEnable</c> corre antes que <c>Start</c>, por eso
    /// la referencia a Firestore se asigna aquí y no en <c>Start</c>.
    /// </remarks>
    void OnEnable()
    {
        db = FirebaseFirestore.DefaultInstance;
        CargarLoterias();
    }

    /// <summary>
    /// Pide a Firestore la lista de todas las loterías guardadas, y llena
    /// el dropdown con sus nombres (agregando primero una opción de
    /// "Selecciona tu lotería" que no cuenta como una lotería real).
    /// </summary>
    public void CargarLoterias()
    {
        if (db == null)
        {
            if (textoEstado != null) textoEstado.text = "Firestore no está listo.";
            return;
        }

        if (textoEstado != null) textoEstado.text = "Cargando loterías...";

        db.Collection("loterias")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (this == null) return; // el objeto se destruyó mientras esperábamos

              if (task.IsFaulted || task.IsCanceled)
              {
                  Debug.LogError("Error cargando loterías: " + task.Exception);
                  if (textoEstado != null) textoEstado.text = "Error al cargar loterías.";
                  return;
              }

              loteriaDocIds.Clear();

              List<TMP_Dropdown.OptionData> opciones = new List<TMP_Dropdown.OptionData>();
              opciones.Add(new TMP_Dropdown.OptionData("Selecciona tu lotería"));

              foreach (DocumentSnapshot doc in task.Result.Documents)
              {
                  string nombre = doc.ContainsField("nombre")
                      ? doc.GetValue<string>("nombre")
                      : "Sin nombre";

                  loteriaDocIds.Add(doc.Id);
                  opciones.Add(new TMP_Dropdown.OptionData(nombre));
              }

              if (dropdownLoterias != null)
              {
                  dropdownLoterias.ClearOptions();
                  dropdownLoterias.AddOptions(opciones);
                  dropdownLoterias.value = 0;
                  dropdownLoterias.RefreshShownValue();
              }

              if (textoEstado != null)
                  textoEstado.text = "Loterías cargadas.";
          });
    }

    /// <summary>
    /// Se ejecuta automáticamente cuando el jugador elige una opción del
    /// dropdown. Si elige la opción "Selecciona tu lotería" (índice 0),
    /// borra la selección guardada. Si elige una lotería real, guarda su
    /// ID y nombre en PlayerPrefs para que el resto del juego sepa cuál está activa.
    /// </summary>
    /// <param name="index">Índice de la opción elegida en el dropdown.</param>
    private void OnLoteriaSeleccionada(int index)
    {
        if (index <= 0)
        {
            PlayerPrefs.DeleteKey(KEY_LOTERIA_DOC_ID);
            PlayerPrefs.DeleteKey(KEY_LOTERIA_NOMBRE);
            PlayerPrefs.Save();
            return;
        }

        // Restamos 1 porque el índice 0 del dropdown es la opción "Selecciona tu lotería",
        // que no cuenta como parte de la lista real de loterías
        int realIndex = index - 1;

        if (realIndex < 0 || realIndex >= loteriaDocIds.Count)
            return;

        string docId = loteriaDocIds[realIndex];
        string nombre = dropdownLoterias.options[index].text;

        PlayerPrefs.SetString(KEY_LOTERIA_DOC_ID, docId);
        PlayerPrefs.SetString(KEY_LOTERIA_NOMBRE, nombre);
        PlayerPrefs.Save();

        RecentLoteriasManager.RegistrarJugada(docId);

        Debug.Log("Lotería seleccionada: " + nombre + " | " + docId);

        if (textoEstado != null)
            textoEstado.text = "Lotería seleccionada: " + nombre;
    }
}