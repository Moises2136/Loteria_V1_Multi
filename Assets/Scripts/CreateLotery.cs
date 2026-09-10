using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla el panel de creación de una lotería nueva:
/// permite escribir su nombre, la crea en Firestore la primera vez que
/// se agrega una carta, lleva el conteo de cuántas cartas se han
/// agregado, y solo permite "Terminar" cuando ya se llegó al número
/// requerido de cartas.
/// </summary>
/// <remarks>
/// - El botón Guardar ya no se usa para evitar duplicados.
/// - El botón Agregar Carta crea la lotería si aún no existe.
/// - El botón Terminar solo se habilita cuando hay 15 cartas.
/// </remarks>
public class CreateLotery : MonoBehaviour
{
    /// <summary>Campo de texto donde se escribe el nombre de la lotería.</summary>
    [Header("Campo de texto para el nombre")]
    public TMP_InputField inputNombreLoteria;

    // =========================
    // BOTONES
    // =========================

    /// <summary>Botón de guardar (ya no se usa; se deshabilita y oculta al iniciar, para evitar loterías duplicadas).</summary>
    [Header("Botones")]
    public Button btnGuardar;

    /// <summary>Botón para ir a agregar una carta (crea la lotería en Firestore si aún no existe).</summary>
    public Button btnAgregarCarta;

    /// <summary>Botón para regresar al menú, cancelando el progreso de la lotería actual.</summary>
    public Button btnRegresar;

    /// <summary>Botón para terminar la creación de la lotería (solo se habilita al llegar a las cartas requeridas).</summary>
    public Button btnTerminar;

    // =========================
    // TEXTOS
    // =========================

    /// <summary>Texto que confirma cuando la lotería ya se guardó en Firestore.</summary>
    [Header("Textos")]
    public TMP_Text txtNombreGuardado;

    /// <summary>Texto usado para mostrar errores o mensajes de estado.</summary>
    public TMP_Text txtError;

    /// <summary>Texto que muestra cuántos caracteres lleva el nombre (ej. "12/100").</summary>
    public TMP_Text txtContadorCaracteres;

    /// <summary>Texto que muestra cuántas cartas se han agregado hasta ahora.</summary>
    public TMP_Text txtCantidadCartas;

    // =========================
    // PANELES
    // =========================

    /// <summary>Este mismo panel de crear lotería.</summary>
    [Header("Paneles")]
    public GameObject panelCrearLoteria;

    /// <summary>Panel al que se pasa para agregar la primera carta.</summary>
    public GameObject panelPaso1CrearCarta;

    /// <summary>Panel del menú principal, al que se regresa al cancelar.</summary>
    public GameObject panelMenu;

    // =========================
    // CONFIGURACIÓN
    // =========================

    /// <summary>Cantidad máxima de caracteres permitidos para el nombre de la lotería.</summary>
    [Header("Configuración")]
    public int limiteCaracteres = 100;

    /// <summary>Cuántas cartas se necesitan como mínimo para poder terminar la lotería.</summary>
    public int cartasRequeridas = 15;

    /// <summary>Nombre de la lotería que se guardó exitosamente en Firestore.</summary>
    private string nombreLoteriaActual = "";

    /// <summary>Cuántas cartas se han agregado hasta ahora a esta lotería.</summary>
    private int cantidadCartasAgregadas = 0;

    /// <summary>Llave usada para guardar/leer el ID de la lotería actual en PlayerPrefs.</summary>
    private const string KEY_LOTERIA_DOC_ID = "loteria_doc_id";

    /// <summary>Llave usada para guardar/leer el nombre de la lotería actual en PlayerPrefs.</summary>
    private const string KEY_LOTERIA_NOMBRE = "loteria_nombre";

    /// <summary>
    /// Al iniciar, deshabilita el botón de guardar (ya no se usa), conecta
    /// los demás botones con sus funciones, recupera el progreso guardado
    /// de una sesión anterior si lo hay, y actualiza los contadores en pantalla.
    /// </summary>
    void Start()
    {
        // Guardar ya no se usa
        if (btnGuardar != null)
        {
            btnGuardar.onClick.RemoveAllListeners();
            btnGuardar.interactable = false;
            btnGuardar.gameObject.SetActive(false); // si prefieres ocultarlo
        }

        if (btnAgregarCarta != null)
        {
            btnAgregarCarta.onClick.RemoveAllListeners();
            btnAgregarCarta.onClick.AddListener(IrAPanelAgregarCarta);
        }

        if (btnRegresar != null)
        {
            btnRegresar.onClick.RemoveAllListeners();
            btnRegresar.onClick.AddListener(RegresarMenu);
        }

        if (btnTerminar != null)
        {
            btnTerminar.onClick.RemoveAllListeners();
            btnTerminar.onClick.AddListener(TerminarLoteria);
        }

        if (inputNombreLoteria != null)
        {
            inputNombreLoteria.onValueChanged.RemoveAllListeners();
            inputNombreLoteria.onValueChanged.AddListener(ActualizarContadorCaracteres);
            ActualizarContadorCaracteres(inputNombreLoteria.text);
        }

        cantidadCartasAgregadas = LoteriaTemp.totalCartas;
        ActualizarContadorCartas();
        ActualizarEstadoBotonTerminar();

        if (txtError != null) txtError.text = "";
        if (txtNombreGuardado != null) txtNombreGuardado.text = "";

        // Si ya había una lotería en progreso (guardada en una sesión anterior), recuperamos su nombre
        string nombreGuardado = PlayerPrefs.GetString(KEY_LOTERIA_NOMBRE, "");
        if (!string.IsNullOrWhiteSpace(nombreGuardado) && inputNombreLoteria != null)
        {
            inputNombreLoteria.text = nombreGuardado;
            nombreLoteriaActual = nombreGuardado;
        }
    }

    /// <summary>
    /// Actualiza el contador de caracteres del nombre (ej. "12/100"), y lo
    /// pinta de rojo si se pasó del límite permitido.
    /// </summary>
    /// <param name="texto">El texto actual del campo de nombre.</param>
    void ActualizarContadorCaracteres(string texto)
    {
        int cantidad = texto.Length;

        if (txtContadorCaracteres != null)
        {
            txtContadorCaracteres.text = cantidad + "/" + limiteCaracteres;
            txtContadorCaracteres.color = cantidad > limiteCaracteres ? Color.red : Color.black;
        }
    }

    /// <summary>
    /// Actualiza el texto que muestra cuántas cartas se han agregado hasta ahora.
    /// </summary>
    void ActualizarContadorCartas()
    {
        if (txtCantidadCartas != null)
            txtCantidadCartas.text = cantidadCartasAgregadas.ToString();
    }

    /// <summary>
    /// Habilita el botón de "Terminar" solo si ya se alcanzó (o superó) la
    /// cantidad de cartas requerida.
    /// </summary>
    void ActualizarEstadoBotonTerminar()
    {
        if (btnTerminar != null)
            btnTerminar.interactable = cantidadCartasAgregadas >= cartasRequeridas;
    }

    // =========================
    // CONTEO DE CARTAS
    // =========================

    /// <summary>
    /// Suma una carta más al conteo, actualiza <see cref="LoteriaTemp.totalCartas"/>
    /// (para mantenerlo sincronizado), y refresca la interfaz.
    /// </summary>
    public void SumarCarta()
    {
        cantidadCartasAgregadas++;
        LoteriaTemp.totalCartas = cantidadCartasAgregadas;

        ActualizarContadorCartas();
        ActualizarEstadoBotonTerminar();
    }

    /// <summary>
    /// Vuelve a leer el conteo de cartas desde <see cref="LoteriaTemp.totalCartas"/>
    /// y refresca la interfaz. Se llama desde fuera (por ejemplo desde
    /// <c>PanelAgregarCartaUI</c>) cada vez que se guarda una carta nueva.
    /// </summary>
    public void RefrescarContadorDesdeFuera()
    {
        cantidadCartasAgregadas = LoteriaTemp.totalCartas;
        ActualizarContadorCartas();
        ActualizarEstadoBotonTerminar();
    }

    /// <summary>
    /// Establece directamente la cantidad de cartas agregadas (por ejemplo,
    /// si se está editando una lotería que ya tenía cartas previas), y
    /// refresca la interfaz.
    /// </summary>
    /// <param name="nuevaCantidad">La nueva cantidad de cartas a establecer.</param>
    public void SetCantidadCartas(int nuevaCantidad)
    {
        cantidadCartasAgregadas = nuevaCantidad;
        LoteriaTemp.totalCartas = nuevaCantidad;

        ActualizarContadorCartas();
        ActualizarEstadoBotonTerminar();
    }

    // =========================
    // CREAR LOTERÍA / IR A AGREGAR CARTA
    // =========================

    /// <summary>
    /// Se ejecuta al presionar "Agregar Carta". Si ya se llegó al máximo de
    /// cartas, lo impide. Si ya existe una lotería guardada con el mismo
    /// nombre, reutiliza esa (sin crear una duplicada) y pasa directo al
    /// panel de agregar carta. Si no existe o el nombre cambió, crea una
    /// lotería nueva en Firestore primero.
    /// </summary>
    void IrAPanelAgregarCarta()
    {
        string nombreActual = inputNombreLoteria != null ? inputNombreLoteria.text.Trim() : "";
        string loteriaDocId = PlayerPrefs.GetString(KEY_LOTERIA_DOC_ID, "");
        string nombreGuardado = PlayerPrefs.GetString(KEY_LOTERIA_NOMBRE, "");

        if (cantidadCartasAgregadas >= cartasRequeridas)
        {
            if (txtError != null)
                txtError.text = "Ya llegaste al máximo de " + cartasRequeridas + " cartas.";
            return;
        }

        // Si ya existe una lotería guardada y el nombre no cambió, no crear otra.
        if (!string.IsNullOrWhiteSpace(loteriaDocId) && nombreActual == nombreGuardado)
        {
            if (panelCrearLoteria != null) panelCrearLoteria.SetActive(false);
            if (panelPaso1CrearCarta != null) panelPaso1CrearCarta.SetActive(true);

            Debug.Log("Usando lotería existente. Abriendo panel Agregar Carta.");
            return;
        }

        // Si no existe o el nombre cambió, crear nueva.
        CrearLoteriaEnFirestore(true);
    }

    /// <summary>
    /// Valida el nombre de la lotería (no vacío, dentro del límite de
    /// caracteres) y, si todo está bien, la crea en Firestore. Al terminar,
    /// guarda su ID y nombre localmente y, si se pide, pasa al panel de
    /// agregar carta.
    /// </summary>
    /// <param name="abrirPanelCartaDespues">
    /// Si es verdadero, al terminar de crear la lotería se pasa automáticamente
    /// al panel de agregar carta.
    /// </param>
    void CrearLoteriaEnFirestore(bool abrirPanelCartaDespues)
    {
        if (inputNombreLoteria == null)
        {
            Debug.LogError("No se asignó inputNombreLoteria en el Inspector.");
            return;
        }

        string nombre = inputNombreLoteria.text.Trim();

        if (string.IsNullOrEmpty(nombre))
        {
            if (txtError != null) txtError.text = "El nombre no puede estar vacío.";
            return;
        }

        if (nombre.Length > limiteCaracteres)
        {
            if (txtError != null) txtError.text = "El nombre no puede tener más de " + limiteCaracteres + " caracteres.";
            return;
        }

        if (FirestoreLoteriaService.Instance == null || !FirestoreLoteriaService.Instance.EstaListo())
        {
            if (txtError != null) txtError.text = "Firestore no está listo.";
            return;
        }

        if (btnAgregarCarta != null) btnAgregarCarta.interactable = false;
        if (txtError != null) txtError.text = "Creando lotería...";

        FirestoreLoteriaService.Instance.CrearLoteria(
            nombre,
            loteriaDocId =>
            {
                nombreLoteriaActual = nombre;

                PlayerPrefs.SetString(KEY_LOTERIA_DOC_ID, loteriaDocId);
                PlayerPrefs.SetString(KEY_LOTERIA_NOMBRE, nombre);
                PlayerPrefs.Save();

                if (txtError != null) txtError.text = "";
                if (txtNombreGuardado != null) txtNombreGuardado.text = "Lotería creada: " + nombre + " ✅";

                if (btnAgregarCarta != null) btnAgregarCarta.interactable = true;

                Debug.Log("Lotería guardada en Firestore: " + nombre + " | DocID: " + loteriaDocId);

                if (abrirPanelCartaDespues)
                {
                    if (panelCrearLoteria != null) panelCrearLoteria.SetActive(false);
                    if (panelPaso1CrearCarta != null) panelPaso1CrearCarta.SetActive(true);

                    Debug.Log("Transición al panel Agregar Carta");
                }
            },
            error =>
            {
                if (txtError != null) txtError.text = error;
                if (btnAgregarCarta != null) btnAgregarCarta.interactable = true;
            }
        );
    }

    // =========================
    // TERMINAR
    // =========================

    /// <summary>
    /// Se ejecuta al presionar "Terminar". Solo continúa si ya se alcanzó
    /// la cantidad exacta (o más) de cartas requeridas.
    /// </summary>
    /// <remarks>
    /// Por ahora solo registra en consola que la lotería se completó;
    /// falta conectar a dónde debe llevar al jugador después (por ejemplo,
    /// de vuelta al menú).
    /// </remarks>
    void TerminarLoteria()
    {
        if (cantidadCartasAgregadas < cartasRequeridas)
        {
            if (txtError != null)
                txtError.text = "Necesitas exactamente " + cartasRequeridas + " cartas para terminar.";
            return;
        }

        if (txtError != null)
            txtError.text = "";

        Debug.Log("Lotería terminada correctamente con " + cantidadCartasAgregadas + " cartas.");

        // Aquí luego puedes mandar al siguiente panel o menú
        // Ejemplo:
        // panelCrearLoteria.SetActive(false);
        // panelMenu.SetActive(true);
    }

    // =========================
    // REGRESAR / CANCELAR
    // =========================

    /// <summary>
    /// Cancela por completo la creación de la lotería actual: borra el
    /// progreso guardado en PlayerPrefs, reinicia todos los contadores y
    /// textos en pantalla, limpia la lista visual de cartas ya agregadas,
    /// y regresa al menú principal.
    /// </summary>
    void RegresarMenu()
    {
        // Limpiar progreso temporal de la lotería actual
        PlayerPrefs.DeleteKey(KEY_LOTERIA_DOC_ID);
        PlayerPrefs.DeleteKey(KEY_LOTERIA_NOMBRE);
        PlayerPrefs.DeleteKey("ultimo_id_carta");
        PlayerPrefs.Save();

        nombreLoteriaActual = "";
        cantidadCartasAgregadas = 0;
        LoteriaTemp.totalCartas = 0;

        // Limpiar input del nombre
        if (inputNombreLoteria != null)
            inputNombreLoteria.text = "";

        // Limpiar textos
        if (txtNombreGuardado != null)
            txtNombreGuardado.text = "";

        if (txtError != null)
            txtError.text = "";

        ActualizarContadorCaracteres("");
        ActualizarContadorCartas();
        ActualizarEstadoBotonTerminar();

        // Si tienes lista visual de cartas, límpiala también
        CardListManage cardList = FindFirstObjectByType<CardListManage>();
        if (cardList != null)
            cardList.LimpiarCartas();

        if (panelCrearLoteria != null)
            panelCrearLoteria.SetActive(false);

        if (panelMenu != null)
            panelMenu.SetActive(true);

        Debug.Log("Creación de lotería cancelada y progreso limpiado.");
    }

    // =========================
    // GETTERS
    // =========================

    /// <summary>
    /// Devuelve el nombre de la lotería que se guardó exitosamente en Firestore.
    /// </summary>
    /// <returns>El nombre de la lotería actual.</returns>
    public string ObtenerNombreLoteria()
    {
        return nombreLoteriaActual;
    }

    /// <summary>
    /// Devuelve cuántas cartas se han agregado hasta ahora a la lotería actual.
    /// </summary>
    /// <returns>La cantidad de cartas agregadas.</returns>
    public int ObtenerCantidadCartas()
    {
        return cantidadCartasAgregadas;
    }
}
