using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// Este script controla la pantalla de inicio de sesión con nombre de
/// usuario y contraseña. Como Firebase solo permite iniciar sesión con
/// correo (no con username), primero busca en Firestore qué correo le
/// corresponde al username escrito, y con ese correo hace el login real
/// en Firebase Authentication. También permite mostrar/ocultar la
/// contraseña y navegar al panel de registro.
/// </summary>
public class LoginManage : MonoBehaviour
{
    // =========================
    // CAMPOS DE ENTRADA
    // =========================

    /// <summary>Campo donde se escribe el nombre de usuario.</summary>
    [Header("Inputs")]
    public TMP_InputField inputUsuario;

    /// <summary>Campo donde se escribe la contraseña.</summary>
    public TMP_InputField inputContrasena;

    // =========================
    // BOTONES
    // =========================

    /// <summary>Botón para iniciar sesión.</summary>
    [Header("Botones")]
    public Button btnIniciarSesion;

    /// <summary>Botón para ir a la pantalla de registro.</summary>
    public Button btnRegistrarse;

    // =========================
    // TEXTOS
    // =========================

    /// <summary>Texto donde se muestran errores o mensajes de estado.</summary>
    [Header("Textos")]
    public TMP_Text txtError;

    // =========================
    // PANELES
    // =========================

    /// <summary>Panel de inicio de sesión (esta misma pantalla).</summary>
    [Header("Paneles")]
    public GameObject PanelInicioSesion;

    /// <summary>Panel del menú principal, al que se entra tras iniciar sesión con éxito.</summary>
    public GameObject PanelMenu;

    /// <summary>Panel de registro de cuenta nueva.</summary>
    public GameObject PanelRegistro;

    // =========================
    // MOSTRAR CONTRASEÑA
    // =========================

    /// <summary>Botón (ícono de ojo) para mostrar u ocultar la contraseña escrita.</summary>
    [Header("Mostrar contraseña")]
    public Button btnMostrarPassword;

    /// <summary>Imagen del ícono de ojo que cambia según si la contraseña está visible o no.</summary>
    public Image iconoOjo;

    /// <summary>Ícono a mostrar cuando la contraseña está visible.</summary>
    public Sprite ojoAbierto;

    /// <summary>Ícono a mostrar cuando la contraseña está oculta.</summary>
    public Sprite ojoCerrado;

    /// <summary>Indica si la contraseña se está mostrando en texto plano actualmente.</summary>
    private bool mostrarPassword = false;

    // =========================
    // FIREBASE
    // =========================

    /// <summary>Referencia al servicio de autenticación de Firebase.</summary>
    private FirebaseAuth auth;

    /// <summary>Referencia a la base de datos de Firestore.</summary>
    private FirebaseFirestore db;

    /// <summary>Indica si Firebase ya terminó de inicializarse.</summary>
    private bool firebaseListo = false;

    /// <summary>
    /// Al iniciar, prepara el campo de contraseña para que empiece oculto,
    /// conecta todos los botones con sus funciones, hace que el error se
    /// borre en cuanto el usuario empiece a escribir de nuevo, y arranca
    /// la conexión con Firebase.
    /// </summary>
    /// <remarks>
    /// La activación/desactivación inicial de los paneles (login, menú,
    /// registro) se dejó comentada a propósito, porque ahora es
    /// <c>SessionManager</c> quien decide qué panel mostrar al abrir la app.
    /// </remarks>
    void Start()
    {
        Debug.Log("LoginManage Start activo");

        if (inputContrasena != null)
        {
            inputContrasena.contentType = TMP_InputField.ContentType.Password;
            inputContrasena.ForceLabelUpdate();
        }

        if (iconoOjo != null && ojoCerrado != null)
            iconoOjo.sprite = ojoCerrado;

        // El SessionManager decide qué panel mostrar al iniciar.
        // if (PanelInicioSesion != null) PanelInicioSesion.SetActive(true);
        // if (PanelMenu != null) PanelMenu.SetActive(false);
        // if (PanelRegistro != null) PanelRegistro.SetActive(false);

        if (txtError != null)
            txtError.text = "";

        if (btnIniciarSesion != null)
        {
            btnIniciarSesion.onClick.RemoveAllListeners();
            btnIniciarSesion.onClick.AddListener(LoginUsuario);
        }

        if (btnRegistrarse != null)
        {
            btnRegistrarse.onClick.RemoveAllListeners();
            btnRegistrarse.onClick.AddListener(AbrirRegistro);
        }

        if (btnMostrarPassword != null)
        {
            btnMostrarPassword.onClick.RemoveAllListeners();
            btnMostrarPassword.onClick.AddListener(TogglePassword);
        }

        // Cada vez que el usuario escribe en cualquier campo, limpiamos el mensaje de error anterior
        if (inputUsuario != null)
            inputUsuario.onValueChanged.AddListener(delegate { LimpiarError(); });

        if (inputContrasena != null)
            inputContrasena.onValueChanged.AddListener(delegate { LimpiarError(); });

        InicializarFirebase();
    }

    /// <summary>
    /// Revisa que todo lo necesario para usar Firebase esté disponible,
    /// y si es así, deja listos Auth y Firestore.
    /// </summary>
    void InicializarFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;
                firebaseListo = true;
                Debug.Log("LoginManage: Firebase listo");
            }
            else
            {
                firebaseListo = false;
                Debug.LogError("LoginManage: Firebase no disponible: " + task.Result);

                if (txtError != null)
                    txtError.text = "Firebase no está disponible.";
            }
        });
    }

    // =========================
    // LOGIN
    // =========================

    /// <summary>
    /// Valida que los campos estén completos, busca en Firestore el correo
    /// asociado al username escrito, y con ese correo intenta iniciar
    /// sesión en Firebase Authentication con la contraseña escrita. Si
    /// todo sale bien, guarda los datos localmente y pasa al menú principal.
    /// </summary>
    public void LoginUsuario()
    {
        LimpiarError();

        if (!firebaseListo || auth == null || db == null)
        {
            if (txtError != null)
                txtError.text = "Firebase aún no está listo.";
            return;
        }

        string username = inputUsuario != null ? inputUsuario.text.Trim() : "";
        string password = inputContrasena != null ? inputContrasena.text : "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            if (txtError != null)
                txtError.text = "Completa todos los campos.";
            return;
        }

        if (txtError != null)
            txtError.text = "Iniciando sesión...";

        // Paso 1: buscar en Firestore qué correo le corresponde a este username
        db.Collection("usuarios")
          .WhereEqualTo("username", username)
          .Limit(1)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCanceled || task.IsFaulted)
              {
                  Debug.LogError("Error buscando usuario: " + task.Exception);
                  if (txtError != null)
                      txtError.text = "No se pudo buscar el usuario.";
                  return;
              }

              var snapshot = task.Result;
              if (snapshot == null || snapshot.Count == 0)
              {
                  if (txtError != null)
                      txtError.text = "Usuario no encontrado.";
                  return;
              }

              string correo = "";
              string nombreCompleto = "";

              foreach (var doc in snapshot.Documents)
              {
                  if (doc.ContainsField("correo"))
                      correo = doc.GetValue<string>("correo");

                  if (doc.ContainsField("nombreCompleto"))
                      nombreCompleto = doc.GetValue<string>("nombreCompleto");

                  break;
              }

              if (string.IsNullOrEmpty(correo))
              {
                  if (txtError != null)
                      txtError.text = "El usuario no tiene correo registrado.";
                  return;
              }

              // Paso 2: ya con el correo real, hacemos el login de verdad en Firebase Auth
              auth.SignInWithEmailAndPasswordAsync(correo, password)
                  .ContinueWithOnMainThread(loginTask =>
                  {
                      if (loginTask.IsCanceled || loginTask.IsFaulted)
                      {
                          Debug.LogError("Error login Auth: " + loginTask.Exception);
                          if (txtError != null)
                              txtError.text = "Usuario o contraseña incorrectos.";
                          return;
                      }

                      FirebaseUser user = loginTask.Result.User;

                      PlayerPrefs.SetString("usuario", username);
                      PlayerPrefs.SetString("correo", correo);
                      PlayerPrefs.SetString("nombreCompleto", nombreCompleto);
                      PlayerPrefs.SetString("uid", user.UserId);
                      PlayerPrefs.Save();

                      if (txtError != null)
                          txtError.text = "Bienvenido " + username;

                      if (PanelInicioSesion != null) PanelInicioSesion.SetActive(false);
                      if (PanelRegistro != null) PanelRegistro.SetActive(false);
                      if (PanelMenu != null) PanelMenu.SetActive(true);

                      Debug.Log("Login exitoso: " + username);
                  });
          });
    }

    // =========================
    // NAVEGACIÓN
    // =========================

    /// <summary>
    /// Cierra el panel de login y abre el panel de registro.
    /// </summary>
    public void AbrirRegistro()
    {
        LimpiarError();

        if (PanelInicioSesion != null) PanelInicioSesion.SetActive(false);
        if (PanelRegistro != null) PanelRegistro.SetActive(true);
        if (PanelMenu != null) PanelMenu.SetActive(false);
    }

    /// <summary>
    /// Borra cualquier mensaje de error mostrado en pantalla.
    /// </summary>
    void LimpiarError()
    {
        if (txtError != null)
            txtError.text = "";
    }

    // =========================
    // MOSTRAR/OCULTAR CONTRASEÑA
    // =========================

    /// <summary>
    /// Cambia entre mostrar la contraseña en texto plano u ocultarla con
    /// puntos, y actualiza el ícono de ojo correspondiente.
    /// </summary>
    public void TogglePassword()
    {
        if (inputContrasena == null) return;

        mostrarPassword = !mostrarPassword;

        inputContrasena.contentType = mostrarPassword
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        if (iconoOjo != null)
            iconoOjo.sprite = mostrarPassword ? ojoAbierto : ojoCerrado;

        inputContrasena.ForceLabelUpdate();
    }
}