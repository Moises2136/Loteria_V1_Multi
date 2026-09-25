using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// Controla el panel de resultados generales del jugador. Las estadísticas
/// se guardan en Firestore dentro de usuarios/{uid}, así pertenecen a la
/// cuenta y no al dispositivo.
/// </summary>
public class PanelResultados : MonoBehaviour
{
    [Header("Textos")]
    public TMP_Text txtPartidasJugadas;
    public TMP_Text txtGanadas;
    public TMP_Text txtPerdidas;
    public TMP_Text txtRatio;
    public TMP_Text txtUltimoResultado;

    [Header("Boton regresar")]
    public Button btnRegresar;
    public GameObject panelResultados;
    public GameObject panelAnterior;

    // Nombres de los campos dentro del documento usuarios/{uid}
    public const string CAMPO_GANADAS = "stats_ganadas";
    public const string CAMPO_PERDIDAS = "stats_perdidas";
    private const string CAMPO_ULTIMO = "stats_ultimo_resultado";

    void Start()
    {
        if (btnRegresar != null)
        {
            btnRegresar.onClick.RemoveAllListeners();
            btnRegresar.onClick.AddListener(Regresar);
        }
    }

    void OnEnable()
    {
        CargarResultados();
    }

    /// <summary>
    /// Devuelve el uid del usuario actual (Firebase primero, PlayerPrefs como respaldo).
    /// </summary>
    private static string ObtenerUid()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null) return user.UserId;
        return PlayerPrefs.GetString("uid", "");
    }

    /// <summary>
    /// Lee las estadísticas desde Firestore y actualiza los textos.
    /// </summary>
    public void CargarResultados()
    {
        string uid = ObtenerUid();
        if (string.IsNullOrEmpty(uid))
        {
            MostrarEnPantalla(0, 0, "Sin sesión");
            return;
        }

        FirebaseFirestore.DefaultInstance
            .Collection("usuarios").Document(uid)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (this == null) return; // el panel se destruyó mientras esperábamos

                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("No se pudieron cargar estadísticas: " + task.Exception);
                    return;
                }

                DocumentSnapshot doc = task.Result;
                long ganadas = doc.Exists && doc.ContainsField(CAMPO_GANADAS) ? doc.GetValue<long>(CAMPO_GANADAS) : 0;
                long perdidas = doc.Exists && doc.ContainsField(CAMPO_PERDIDAS) ? doc.GetValue<long>(CAMPO_PERDIDAS) : 0;
                string ultimo = doc.Exists && doc.ContainsField(CAMPO_ULTIMO) ? doc.GetValue<string>(CAMPO_ULTIMO) : "Sin partidas";

                MostrarEnPantalla(ganadas, perdidas, ultimo);
            });
    }

    /// <summary>
    /// Pinta los números en los textos y calcula el porcentaje de victorias.
    /// </summary>
    private void MostrarEnPantalla(long ganadas, long perdidas, string ultimo)
    {
        long jugadas = ganadas + perdidas;
        float ratio = jugadas > 0 ? (float)ganadas / jugadas * 100f : 0f;

        if (txtPartidasJugadas != null) txtPartidasJugadas.text = jugadas.ToString();
        if (txtGanadas != null) txtGanadas.text = ganadas.ToString();
        if (txtPerdidas != null) txtPerdidas.text = perdidas.ToString();
        if (txtRatio != null) txtRatio.text = ratio.ToString("F1") + "%";
        if (txtUltimoResultado != null) txtUltimoResultado.text = "Último resultado: " + ultimo;
    }

    public static void RegistrarVictoria() => Registrar(true);
    public static void RegistrarDerrota() => Registrar(false);

    /// <summary>
    /// Suma 1 a ganadas o perdidas directamente en Firestore y guarda el último resultado.
    /// </summary>
    private static void Registrar(bool gano)
    {
        string uid = ObtenerUid();
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("No hay uid; no se registró el resultado.");
            return;
        }

        var data = new Dictionary<string, object>
        {
            { gano ? CAMPO_GANADAS : CAMPO_PERDIDAS, FieldValue.Increment(1L) },
            { CAMPO_ULTIMO, gano ? "Ganaste" : "Perdiste" }
        };

        FirebaseFirestore.DefaultInstance
            .Collection("usuarios").Document(uid)
            .SetAsync(data, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    Debug.LogError("Error registrando resultado: " + task.Exception);
            });
    }

    public void Regresar()
    {
        if (panelResultados != null) panelResultados.SetActive(false);
        if (panelAnterior != null) panelAnterior.SetActive(true);
    }
}