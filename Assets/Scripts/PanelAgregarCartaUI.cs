using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ImageCropperNamespace;

/// <summary>
/// Este script controla el panel donde se agrega una carta nueva a una
/// lotería: permite elegir una imagen desde la galería del dispositivo,
/// recortarla, escribir el nombre y la descripción de la carta, y al
/// guardar, sube la imagen a Cloudinary y guarda la carta completa en
/// Firestore.
/// </summary>
public class PanelAgregarCartaUI : MonoBehaviour
{
    // =========================
    // PANELES
    // =========================

    /// <summary>Este mismo panel de agregar carta.</summary>
    [Header("Paneles")]
    public GameObject panelAgregarCarta;

    /// <summary>Panel de creación de lotería, al que se regresa al terminar o cancelar.</summary>
    public GameObject panelCrearLoteria;

    // =========================
    // UI
    // =========================

    /// <summary>Imagen donde se muestra la vista previa de la carta seleccionada.</summary>
    [Header("UI")]
    public Image previewImagen;

    /// <summary>Botón para abrir la galería y elegir una imagen.</summary>
    public Button btnSubirImagen;

    /// <summary>Campo de texto donde se escribe el nombre de la carta.</summary>
    public TMP_InputField inputNombreCarta;

    /// <summary>Campo de texto donde se escribe la descripción de la carta.</summary>
    public TMP_InputField inputDescripcionCarta;

    /// <summary>Botón para guardar la carta (sube la imagen y la guarda en la base de datos).</summary>
    public Button btnGuardarCarta;

    /// <summary>Botón para cancelar y descartar la carta que se estaba armando.</summary>
    public Button btnCancelar;

    // =========================
    // TEXTOS
    // =========================

    /// <summary>Texto usado para mostrar errores o mensajes de estado.</summary>
    [Header("Textos")]
    public TMP_Text txtError;

    /// <summary>Texto que muestra cuántos caracteres lleva el nombre (ej. "12/100").</summary>
    public TMP_Text txtLimiteNombre;

    /// <summary>Texto que muestra cuántos caracteres lleva la descripción (ej. "45/200").</summary>
    public TMP_Text txtLimiteDescripcion;

    /// <summary>Texto/aviso que invita a agregar una foto, y se oculta en cuanto ya hay una imagen elegida.</summary>
    public TMP_Text txtAgregarFoto;

    // =========================
    // CONFIGURACIÓN
    // =========================

    /// <summary>Cantidad máxima de caracteres permitidos para el nombre de la carta.</summary>
    [Header("Configuración")]
    public int limiteNombre = 100;

    /// <summary>Cantidad máxima de caracteres permitidos para la descripción de la carta.</summary>
    public int limiteDescripcion = 200;

    // =========================
    // MANAGERS
    // =========================

    /// <summary>Referencia al script que administra la lista visual de cartas ya agregadas.</summary>
    [Header("Managers")]
    public CardListManage cardListManage;

    /// <summary>Referencia al script de creación de lotería, para refrescar el contador de cartas.</summary>
    public CreateLotery createLotery;

    /// <summary>Referencia al script que sube imágenes a Cloudinary.</summary>
    public CloudinaryUploader cloudinaryUploader;

    /// <summary>Ruta local de la imagen seleccionada desde la galería.</summary>
    private string selectedImagePath = "";

    /// <summary>Llave usada para leer el ID de la lotería actual guardado en PlayerPrefs.</summary>
    private const string KEY_LOTERIA_DOC_ID = "loteria_doc_id";

    /// <summary>
    /// Al iniciar, conecta todos los botones y campos de texto con sus
    /// funciones correspondientes, y deja la interfaz en su estado limpio inicial.
    /// </summary>
    void Start()
    {
        if (btnSubirImagen != null)
        {
            btnSubirImagen.onClick.RemoveAllListeners();
            btnSubirImagen.onClick.AddListener(SeleccionarImagenDesdeGaleria);
        }

        if (btnGuardarCarta != null)
        {
            btnGuardarCarta.onClick.RemoveAllListeners();
            btnGuardarCarta.onClick.AddListener(GuardarCartaConCloudinaryYFirestore);
        }

        if (btnCancelar != null)
        {
            btnCancelar.onClick.RemoveAllListeners();
            btnCancelar.onClick.AddListener(Cancelar);
        }

        // Cada vez que se escribe, actualizamos el contador de caracteres en pantalla
        if (inputNombreCarta != null)
        {
            inputNombreCarta.onValueChanged.RemoveAllListeners();
            inputNombreCarta.onValueChanged.AddListener(ActualizarLimiteNombre);
            ActualizarLimiteNombre(inputNombreCarta.text);
        }

        if (inputDescripcionCarta != null)
        {
            inputDescripcionCarta.onValueChanged.RemoveAllListeners();
            inputDescripcionCarta.onValueChanged.AddListener(ActualizarLimiteDescripcion);
            ActualizarLimiteDescripcion(inputDescripcionCarta.text);
        }

        LimpiarUI();
    }

    /// <summary>
    /// Actualiza el contador de caracteres del campo de nombre (ej. "12/100").
    /// </summary>
    /// <param name="texto">El texto actual del campo de nombre.</param>
    void ActualizarLimiteNombre(string texto)
    {
        if (txtLimiteNombre != null)
            txtLimiteNombre.text = texto.Length + "/" + limiteNombre;
    }

    /// <summary>
    /// Actualiza el contador de caracteres del campo de descripción (ej. "45/200").
    /// </summary>
    /// <param name="texto">El texto actual del campo de descripción.</param>
    void ActualizarLimiteDescripcion(string texto)
    {
        if (txtLimiteDescripcion != null)
            txtLimiteDescripcion.text = texto.Length + "/" + limiteDescripcion;
    }

    // =========================
    // SELECCIÓN Y RECORTE DE IMAGEN
    // =========================

    /// <summary>
    /// Abre la galería del dispositivo para que el usuario elija una imagen,
    /// y luego abre la herramienta de recorte para ajustarla a un cuadro
    /// perfecto (proporción 1:1) antes de usarla como imagen de la carta.
    /// </summary>
    /// <remarks>
    /// El resultado final del recorte se guarda temporalmente en
    /// <c>CartTemp.imagen</c>, listo para subirse cuando se guarde la carta.
    /// </remarks>
    public void SeleccionarImagenDesdeGaleria()
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (string.IsNullOrEmpty(path))
                return;

            Texture2D texturaOriginal = NativeGallery.LoadImageAtPath(path, 2048, false, false);

            if (texturaOriginal == null)
            {
                MostrarError("No se pudo cargar la imagen.");
                return;
            }

            selectedImagePath = path;

            // Configuración de la herramienta de recorte: fuerza un recorte cuadrado (1:1)
            ImageCropper.Settings settings = new ImageCropper.Settings
            {
                ovalSelection = false,
                autoZoomEnabled = true,
                imageBackground = Color.black,
                selectionMinAspectRatio = 1f,
                selectionMaxAspectRatio = 1f,
                markTextureNonReadable = false,
                visibleButtons = ImageCropper.Button.FlipHorizontal |
                                 ImageCropper.Button.FlipVertical |
                                 ImageCropper.Button.Rotate90Degrees,
                guidelinesVisibility = ImageCropper.Visibility.AlwaysVisible,
                initialOrientation = ImageCropper.Orientation.Normal
            };

            ImageCropper.Instance.Show(
                texturaOriginal,
                (bool result, Texture originalImage, Texture2D croppedImage) =>
                {
                    if (!result || croppedImage == null)
                    {
                        MostrarError("Recorte cancelado.");
                        return;
                    }

                    CartTemp.imagen = croppedImage;

                    Sprite sprite = Sprite.Create(
                        croppedImage,
                        new Rect(0, 0, croppedImage.width, croppedImage.height),
                        new Vector2(0.5f, 0.5f)
                    );

                    if (previewImagen != null)
                    {
                        previewImagen.sprite = sprite;
                        previewImagen.color = Color.white;
                        previewImagen.preserveAspect = true;
                    }

                    // Ocultar texto al cargar imagen
                    if (txtAgregarFoto != null)
                        txtAgregarFoto.gameObject.SetActive(false);

                    if (txtError != null)
                        txtError.text = "Imagen recortada correctamente.";
                },
                settings,
                (ref int width, ref int height) =>
                {
                    width = 512;
                    height = 512;
                }
            );
        }, "Selecciona una imagen", "image/*");
    }

    // =========================
    // GUARDAR CARTA
    // =========================

    /// <summary>
    /// Valida todos los campos de la carta (imagen elegida, nombre no vacío,
    /// límites de caracteres), y si todo está bien, sube la imagen a Cloudinary
    /// y después guarda la carta completa en Firestore.
    /// </summary>
    /// <remarks>
    /// El proceso es en dos pasos encadenados: primero se sube la imagen y se
    /// obtiene su URL, y solo si eso sale bien se guarda la carta (ya con la URL)
    /// en la base de datos.
    /// </remarks>
    void GuardarCartaConCloudinaryYFirestore()
    {
        string nombre = inputNombreCarta != null ? inputNombreCarta.text.Trim() : "";
        string descripcion = inputDescripcionCarta != null ? inputDescripcionCarta.text.Trim() : "";

        if (CartTemp.imagen == null || string.IsNullOrWhiteSpace(selectedImagePath))
        {
            MostrarError("Debes seleccionar una imagen.");
            return;
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MostrarError("Debes escribir el nombre de la carta.");
            return;
        }

        if (nombre.Length > limiteNombre)
        {
            MostrarError("El nombre excede el límite.");
            return;
        }

        if (descripcion.Length > limiteDescripcion)
        {
            MostrarError("La descripción excede el límite.");
            return;
        }

        if (cloudinaryUploader == null)
        {
            MostrarError("Falta asignar CloudinaryUploader.");
            return;
        }

        if (FirestoreLoteriaService.Instance == null || !FirestoreLoteriaService.Instance.EstaListo())
        {
            MostrarError("Firestore no está listo.");
            return;
        }

        string loteriaDocId = PlayerPrefs.GetString(KEY_LOTERIA_DOC_ID, "");
        if (string.IsNullOrWhiteSpace(loteriaDocId))
        {
            MostrarError("No hay lotería guardada. Primero guarda la lotería.");
            return;
        }

        CartTemp.nombre = nombre;
        CartTemp.descripcion = descripcion;

        if (btnGuardarCarta != null)
            btnGuardarCarta.interactable = false;

        if (txtError != null)
            txtError.text = "Subiendo imagen...";

        // Paso 1: subir la imagen a Cloudinary
        StartCoroutine(cloudinaryUploader.UploadTexture(
            CartTemp.imagen,
            url =>
            {
                CartTemp.rutaImagen = url;

                CartaData carta = new CartaData
                {
                    id = GenerarIdCartaLocal(),
                    nombre = CartTemp.nombre,
                    descripcion = CartTemp.descripcion,
                    rutaImagen = CartTemp.rutaImagen
                };

                if (txtError != null)
                    txtError.text = "Guardando carta en Firestore...";

                // Paso 2: ya con la URL de la imagen, guardamos la carta completa en Firestore
                FirestoreLoteriaService.Instance.GuardarCarta(
                    loteriaDocId,
                    carta,
                    () =>
                    {
                        Sprite sprite = Sprite.Create(
                            CartTemp.imagen,
                            new Rect(0, 0, CartTemp.imagen.width, CartTemp.imagen.height),
                            new Vector2(0.5f, 0.5f)
                        );

                        if (cardListManage != null)
                            cardListManage.AgregarCartaVisual(carta.nombre, sprite);

                        LoteriaTemp.totalCartas++;

                        if (createLotery != null)
                            createLotery.RefrescarContadorDesdeFuera();

                        Debug.Log("Carta subida a Cloudinary");
                        Debug.Log("URL Cloudinary: " + carta.rutaImagen);
                        Debug.Log("Carta guardada en Firestore");

                        if (txtError != null)
                            txtError.text = "";

                        if (btnGuardarCarta != null)
                            btnGuardarCarta.interactable = true;

                        LimpiarUI();

                        if (panelAgregarCarta != null) panelAgregarCarta.SetActive(false);
                        if (panelCrearLoteria != null) panelCrearLoteria.SetActive(true);
                    },
                    errorFirestore =>
                    {
                        if (btnGuardarCarta != null)
                            btnGuardarCarta.interactable = true;

                        MostrarError(errorFirestore);
                    }
                );
            },
            error =>
            {
                if (btnGuardarCarta != null)
                    btnGuardarCarta.interactable = true;

                MostrarError(error);
            }
        ));
    }

    /// <summary>
    /// Genera un ID numérico único para una carta nueva, guardando un
    /// contador incremental en PlayerPrefs (cada vez que se llama, sube en 1).
    /// </summary>
    /// <returns>El siguiente ID disponible para una carta.</returns>
    int GenerarIdCartaLocal()
    {
        int ultimo = PlayerPrefs.GetInt("ultimo_id_carta", 0);
        ultimo++;
        PlayerPrefs.SetInt("ultimo_id_carta", ultimo);
        PlayerPrefs.Save();
        return ultimo;
    }

    // =========================
    // CANCELAR / ERRORES / LIMPIEZA
    // =========================

    /// <summary>
    /// Descarta la carta que se estaba armando y regresa al panel de crear lotería.
    /// </summary>
    void Cancelar()
    {
        LimpiarUI();

        if (panelAgregarCarta != null) panelAgregarCarta.SetActive(false);
        if (panelCrearLoteria != null) panelCrearLoteria.SetActive(true);
    }

    /// <summary>
    /// Muestra un mensaje de error en pantalla y también lo registra en la consola.
    /// </summary>
    /// <param name="mensaje">El mensaje de error a mostrar.</param>
    void MostrarError(string mensaje)
    {
        Debug.LogWarning(mensaje);
        if (txtError != null)
            txtError.text = mensaje;
    }

    /// <summary>
    /// Deja el panel en su estado inicial: borra la imagen y los datos
    /// temporales de la carta (<c>CartTemp</c>), limpia los campos de texto,
    /// oculta cualquier error, y vuelve a mostrar el aviso de "agregar foto".
    /// </summary>
    void LimpiarUI()
    {
        CartTemp.Limpiar();
        selectedImagePath = "";

        if (inputNombreCarta != null) inputNombreCarta.text = "";
        if (inputDescripcionCarta != null) inputDescripcionCarta.text = "";

        if (previewImagen != null)
        {
            previewImagen.sprite = null;
            previewImagen.color = new Color(1f, 1f, 1f, 0f);
        }

        // Volver a mostrar el texto al limpiar
        if (txtAgregarFoto != null)
            txtAgregarFoto.gameObject.SetActive(true);

        if (txtError != null) txtError.text = "";

        ActualizarLimiteNombre("");
        ActualizarLimiteDescripcion("");
    }
}
