using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// Controla el panel de logros. Los logros se configuran en el Inspector y
/// se desbloquean calculándolos a partir de las estadísticas del jugador
/// (stats_ganadas / stats_perdidas en usuarios/{uid}), así que no se guardan
/// por separado y pertenecen a la cuenta, no al dispositivo.
/// </summary>
public class PanelLogros : MonoBehaviour
{
    /// <summary>Qué estadística se usa para medir el progreso de un logro.</summary>
    public enum TipoCondicion { PartidasJugadas, PartidasGanadas }

    /// <summary>Duración del fade al abrir/cerrar el popup (0 = instantáneo).</summary>
    [Min(0f)] public float duracionFade = 0.2f;

    /// <summary>Animación en curso, para cortarla si se abre/cierra otra.</summary>
    private Coroutine rutinaFade;

    /// <summary>
    /// Un logro configurable desde el Inspector: su nombre, cómo se desbloquea,
    /// la meta a alcanzar y el botón que lo representa en pantalla.
    /// </summary>
    [Serializable]
    public class Logro
    {
        public string nombre;
        [TextArea] public string comoDesbloquear;
        public TipoCondicion condicion;
        public int meta = 1;
        public Button boton;

        /// <summary>Candado o sombra encima del botón. Si no se asigna, el botón se tiñe de gris.(aun revisando consideración)</summary>
        public GameObject overlayBloqueado;

        [HideInInspector] public bool desbloqueado;
    }

    // =========================
    // LOGROS
    // =========================

    [Header("Logros (se configuran aquí)")]
    public Logro[] logros;

    // =========================
    // POPUP DE DETALLE
    // =========================

    /// <summary>Raíz del popup (contiene el fondo oscuro y la tarjeta como hermanos).</summary>
    [Header("Popup de detalle")]
    public GameObject panelDetalle;

    /// <summary>Fondo oscuro a pantalla completa; al tocarlo se cierra el popup.</summary>
    public Button btnFondoOscuro;

    public TMP_Text txtTitulo;
    public TMP_Text txtDescripcion;
    public TMP_Text txtEstado;

    // =========================
    // NAVEGACIÓN
    // =========================

    [Header("Botón regresar")]
    public Button btnRegresar;
    public GameObject panelLogros;
    public GameObject panelAnterior;

    /// <summary>Colores del botón según su estado (solo se usan si no hay overlay).</summary>
    private static readonly Color COLOR_DESBLOQUEADO = Color.white;
    private static readonly Color COLOR_BLOQUEADO = new Color(0.7f, 0.7f, 0.7f);

    private long ganadas;
    private long jugadas;

    // =========================
    // INICIALIZACIÓN
    // =========================

    void Start()
    {
        for (int i = 0; i < logros.Length; i++)
        {
            int idx = i;
            if (logros[i].boton != null)
            {
                logros[i].boton.onClick.RemoveAllListeners();
                logros[i].boton.onClick.AddListener(() => AbrirDetalle(idx));
            }
        }

        if (btnFondoOscuro != null)
        {
            btnFondoOscuro.onClick.RemoveAllListeners();
            btnFondoOscuro.onClick.AddListener(CerrarDetalle);
        }

        if (btnRegresar != null)
        {
            btnRegresar.onClick.RemoveAllListeners();
            btnRegresar.onClick.AddListener(Regresar);
        }
    }

    /// <summary>
    /// Cada vez que se abre el panel: cierra el popup, pinta con lo último
    /// conocido y pide las estadísticas actualizadas a Firestore.
    /// </summary>
    void OnEnable()
    {
        CerrarDetalleInstantaneo();
        PintarLogros();
        CargarStats();
    }

    // =========================
    // CARGA DE ESTADÍSTICAS
    // =========================

    /// <summary>Lee ganadas/perdidas del usuario y vuelve a evaluar los logros.</summary>
    void CargarStats()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        string uid = user != null ? user.UserId : PlayerPrefs.GetString("uid", "");
        if (string.IsNullOrEmpty(uid)) return;

        FirebaseFirestore.DefaultInstance
            .Collection("usuarios").Document(uid)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (this == null) return; // el panel se destruyó mientras esperábamos

                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("No se pudieron cargar stats para logros: " + task.Exception);
                    return;
                }

                DocumentSnapshot doc = task.Result;
                ganadas = LeerLong(doc, PanelResultados.CAMPO_GANADAS);
                jugadas = ganadas + LeerLong(doc, PanelResultados.CAMPO_PERDIDAS);

                PintarLogros();
            });
    }

    static long LeerLong(DocumentSnapshot doc, string campo) =>
        doc.Exists && doc.ContainsField(campo) ? doc.GetValue<long>(campo) : 0;

    long Progreso(Logro l) =>
        l.condicion == TipoCondicion.PartidasGanadas ? ganadas : jugadas;

    // =========================
    // PINTAR LOGROS
    // =========================

    /// <summary>
    /// Marca cada logro como desbloqueado o no. Usa el overlay si está asignado;
    /// si no, tiñe el botón de gris cuando está bloqueado.
    /// </summary>
    void PintarLogros()
    {
        foreach (var l in logros)
        {
            l.desbloqueado = Progreso(l) >= l.meta;

            if (l.overlayBloqueado != null)
                l.overlayBloqueado.SetActive(!l.desbloqueado);
            else if (l.boton != null && l.boton.image != null)
                l.boton.image.color = l.desbloqueado ? COLOR_DESBLOQUEADO : COLOR_BLOQUEADO;
        }
    }

    // =========================
    // POPUP
    // =========================

    /// <summary>Abre el popup con el nombre, cómo desbloquearlo y el progreso del logro.</summary>
    void AbrirDetalle(int idx)
    {
        var l = logros[idx];
        long progreso = Math.Min(Progreso(l), l.meta);

        if (txtTitulo != null) txtTitulo.text = l.nombre;
        if (txtDescripcion != null) txtDescripcion.text = l.comoDesbloquear;
        if (txtEstado != null)
            txtEstado.text = l.desbloqueado ? "¡Desbloqueado!" : $"Progreso: {progreso}/{l.meta}";

        if (duracionFade <= 0f || !isActiveAndEnabled)
        {
            foreach (GameObject obj in ObjetosPopup()) obj.SetActive(true);
            return;
        }

        IniciarFade(true);
    }

    /// <summary>Cierra el popup con fade (lo llama el fondo oscuro).</summary>
    void CerrarDetalle()
    {
        bool abierto = panelDetalle != null && panelDetalle.activeSelf;

        if (duracionFade <= 0f || !isActiveAndEnabled || !abierto)
        {
            CerrarDetalleInstantaneo();
            return;
        }

        IniciarFade(false);
    }

    /// <summary>Apaga el popup y el fondo al instante, dejando su alpha listo para la próxima vez.</summary>
    void CerrarDetalleInstantaneo()
    {
        if (rutinaFade != null)
        {
            StopCoroutine(rutinaFade);
            rutinaFade = null;
        }

        foreach (GameObject obj in ObjetosPopup())
        {
            RestaurarAlpha(obj);
            obj.SetActive(false);
        }
    }

    void IniciarFade(bool mostrar)
    {
        if (rutinaFade != null) StopCoroutine(rutinaFade);
            rutinaFade = StartCoroutine(Fade(mostrar));
    }

    /// <summary>
    /// Anima el alpha del fondo oscuro y del popup. Al mostrar: de transparente a visible.
    /// Al cerrar: de visible a transparente y luego los apaga.
    /// </summary>
    IEnumerator Fade(bool mostrar)
    {
        List<GameObject> objetos = ObjetosPopup();
        var grupos = new List<CanvasGroup>();
        var inicio = new List<float>();

        foreach (GameObject obj in objetos)
        {
            CanvasGroup cg = obj.TryGetComponent(out CanvasGroup c) ? c : obj.AddComponent<CanvasGroup>();

            if (mostrar && !obj.activeSelf)
            {
                cg.alpha = 0f;
                obj.SetActive(true);
            }

            cg.blocksRaycasts = false;   // sin toques durante la animación
            grupos.Add(cg);
            inicio.Add(cg.alpha);
        }

        float destino = mostrar ? 1f : 0f;

        for (float t = 0f; t < duracionFade; t += Time.unscaledDeltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / duracionFade);
            for (int i = 0; i < grupos.Count; i++)
                grupos[i].alpha = Mathf.Lerp(inicio[i], destino, p);

            yield return null;
        }

        for (int i = 0; i < grupos.Count; i++)
        {
            grupos[i].alpha = 1f;
            grupos[i].blocksRaycasts = true;

            if (!mostrar)
                objetos[i].SetActive(false);
        }

        rutinaFade = null;
    }

    /// <summary>El fondo oscuro (el objeto del botón) y el popup.</summary>
    List<GameObject> ObjetosPopup()
    {
        var lista = new List<GameObject>();
        if (btnFondoOscuro != null) lista.Add(btnFondoOscuro.gameObject);
        if (panelDetalle != null) lista.Add(panelDetalle);
        return lista;
    }

    void RestaurarAlpha(GameObject obj)
    {
        if (obj != null && obj.TryGetComponent(out CanvasGroup cg))
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
        }
    }

    // =========================
    // NAVEGACIÓN
    // =========================

    public void Regresar()
    {
        CerrarDetalle();
        if (panelLogros != null) panelLogros.SetActive(false);
        if (panelAnterior != null) panelAnterior.SetActive(true);
    }
}