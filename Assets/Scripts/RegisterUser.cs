using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// Este script controla la pantalla de registro de una cuenta nueva:
/// valida los datos que escribe el usuario, revisa que el nombre de
/// usuario no esté repetido, crea la cuenta en Firebase Authentication,
/// y guarda el perfil completo en Firestore.
/// </summary>
public class RegisterUser : MonoBehaviour
{
    // =========================
    // CAMPOS DE ENTRADA
    // =========================

    /// <summary>Campo donde se escribe el nombre completo.</summary>
    [Header("Inputs")]
    public TMP_InputField inputNombreCompleto;

    /// <summary>Campo donde se escribe el nombre de usuario (username).</summary>
    public TMP_InputField inputNombreUsuario;

    /// <summary>Campo donde se escribe el correo electrónico.</summary>
    public TMP_InputField inputCorreo;

    /// <summary>Campo donde se escribe la contraseña.</summary>
    public TMP_InputField inputContrasena;

    /// <summary>Campo donde se repite la contraseña, para confirmarla.</summary>
    public TMP_InputField inputConfirmarContrasena;

    // =========================
    // UI
    // =========================

    /// <summary>Botón para enviar el registro.</summary>
    [Header("UI")]
    public Button btnRegistro;

    /// <summary>Botón para volver a la pantalla de login.</summary>
    public Button btnVolver;

    /// <summary>Texto donde se muestran errores o mensajes de estado.</summary>
    public TMP_Text txtError;

    // =========================
    // PANELES
    // =========================

    /// <summary>Panel de registro (esta misma pantalla).</summary>
    [Header("Paneles")]
    public GameObject PanelRegistro;

    /// <summary>Panel del menú principal, al que se entra tras registrarse con éxito.</summary>
    public GameObject PanelMenu;

    /// <summary>Panel de inicio de sesión.</summary>
    public GameObject PanelInicioSesion;

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
    /// Al iniciar, conecta los botones con sus funciones, hace que el mensaje
    /// de error se borre en cuanto el usuario empiece a corregir un campo,
    /// y arranca la conexión con Firebase.
    /// </summary>
    void Start()
    {
        if (btnRegistro != null)
        {
            btnRegistro.onClick.RemoveAllListeners();
            btnRegistro.onClick.AddListener(ValidarRegistro);
        }

        if (btnVolver != null)
        {
            btnVolver.onClick.RemoveAllListeners();
            btnVolver.onClick.AddListener(VolverAlLogin);
        }

        // Cada vez que el usuario escribe en cualquier campo, limpiamos el mensaje de error anterior
        if (inputNombreCompleto != null)
            inputNombreCompleto.onValueChanged.AddListener(delegate { LimpiarError(); });

        if (inputNombreUsuario != null)
            inputNombreUsuario.onValueChanged.AddListener(delegate { LimpiarError(); });

        if (inputCorreo != null)
            inputCorreo.onValueChanged.AddListener(delegate { LimpiarError(); });

        if (inputContrasena != null)
            inputContrasena.onValueChanged.AddListener(delegate { LimpiarError(); });

        if (inputConfirmarContrasena != null)
            inputConfirmarContrasena.onValueChanged.AddListener(delegate { LimpiarError(); });

        if (txtError != null)
            txtError.text = "";

        InicializarFirebase();
    }

    /// <summary>
    /// Revisa que todo lo necesario para usar Firebase esté disponible,
    /// y si es así, deja listos Auth y Firestore para poder registrar usuarios.
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
                Debug.Log("RegisterUser: Firebase listo");
            }
            else
            {
                firebaseListo = false;
                Debug.LogError("RegisterUser: Firebase no disponible: " + task.Result);

                if (txtError != null)
                    txtError.text = "Firebase no está disponible.";
            }
        });
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
    // VALIDACIÓN Y REGISTRO
    // =========================

    /// <summary>
    /// Revisa que todos los campos del formulario estén completos y sean válidos
    /// (correo con formato correcto, contraseña de al menos 6 caracteres, y que
    /// coincida con su confirmación). Si todo está bien, continúa con la validación
    /// del nombre de usuario.
    /// </summary>
    void ValidarRegistro()
    {
        if (!firebaseListo || auth == null || db == null)
        {
            if (txtError != null)
                txtError.text = "Firebase aún no está listo.";
            return;
        }

        string nombreCompleto = inputNombreCompleto != null ? inputNombreCompleto.text.Trim() : "";
        string nombreUsuario = inputNombreUsuario != null ? inputNombreUsuario.text.Trim() : "";
        string correo = inputCorreo != null ? inputCorreo.text.Trim() : "";
        string contrasena = inputContrasena != null ? inputContrasena.text : "";
        string confirmarContrasena = inputConfirmarContrasena != null ? inputConfirmarContrasena.text : "";

        if (string.IsNullOrEmpty(nombreCompleto) ||
            string.IsNullOrEmpty(nombreUsuario) ||
            string.IsNullOrEmpty(correo) ||
            string.IsNullOrEmpty(contrasena) ||
            string.IsNullOrEmpty(confirmarContrasena))
        {
            if (txtError != null) txtError.text = "Por favor completa todos los campos.";
            return;
        }

        if (!correo.Contains("@") || !correo.Contains("."))
        {
            if (txtError != null) txtError.text = "Correo inválido.";
            return;
        }

        if (contrasena.Length < 6)
        {
            if (txtError != null) txtError.text = "La contraseña debe tener al menos 6 caracteres.";
            return;
        }

        if (contrasena != confirmarContrasena)
        {
            if (txtError != null) txtError.text = "Las contraseñas no coinciden.";
            return;
        }

        btnRegistro.interactable = false;
        if (txtError != null) txtError.text = "Registrando usuario...";

        ValidarUsernameYRegistrar(nombreCompleto, nombreUsuario, correo, contrasena);
    }

    /// <summary>
    /// Revisa en Firestore que nadie más esté usando el nombre de usuario elegido.
    /// Si está libre, continúa con el registro en Firebase Authentication.
    /// </summary>
    /// <param name="nombreCompleto">Nombre completo del usuario.</param>
    /// <param name="nombreUsuario">Nombre de usuario elegido.</param>
    /// <param name="correo">Correo del usuario.</param>
    /// <param name="contrasena">Contraseña elegida.</param>
    void ValidarUsernameYRegistrar(string nombreCompleto, string nombreUsuario, string correo, string contrasena)
    {
        db.Collection("usuarios")
          .WhereEqualTo("username", nombreUsuario)
          .Limit(1)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCanceled || task.IsFaulted)
              {
                  Debug.LogError("Error validando username: " + task.Exception);
                  if (txtError != null) txtError.text = "No se pudo validar el nombre de usuario.";
                  btnRegistro.interactable = true;
                  return;
              }

              QuerySnapshot snapshot = task.Result;
              if (snapshot != null && snapshot.Count > 0)
              {
                  if (txtError != null) txtError.text = "Ese nombre de usuario ya está en uso.";
                  btnRegistro.interactable = true;
                  return;
              }

              RegistrarEnAuth(nombreCompleto, nombreUsuario, correo, contrasena);
          });
    }

    /// <summary>
    /// Crea la cuenta en Firebase Authentication usando el correo y contraseña.
    /// Si algo falla, traduce el error técnico de Firebase a un mensaje entendible
    /// para el usuario. Si todo sale bien, continúa guardando el perfil en Firestore.
    /// </summary>
    /// <param name="nombreCompleto">Nombre completo del usuario.</param>
    /// <param name="nombreUsuario">Nombre de usuario elegido.</param>
    /// <param name="correo">Correo del usuario.</param>
    /// <param name="contrasena">Contraseña elegida.</param>
    void RegistrarEnAuth(string nombreCompleto, string nombreUsuario, string correo, string contrasena)
    {
        auth.CreateUserWithEmailAndPasswordAsync(correo, contrasena)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    string mensaje = "No se pudo registrar el usuario.";

                    // Revisamos el mensaje de error real de Firebase para dar un mensaje más claro
                    if (task.Exception != null && task.Exception.InnerExceptions.Count > 0)
                    {
                        string errorRaw = task.Exception.InnerExceptions[0].Message.ToLower();

                        if (errorRaw.Contains("already in use"))
                            mensaje = "Ese correo ya está registrado.";
                        else if (errorRaw.Contains("badly formatted"))
                            mensaje = "El correo no tiene un formato válido.";
                        else if (errorRaw.Contains("password"))
                            mensaje = "La contraseña no es válida.";
                    }

                    Debug.LogError("Error registro Auth: " + task.Exception);
                    if (txtError != null) txtError.text = mensaje;
                    btnRegistro.interactable = true;
                    return;
                }

                FirebaseUser user = task.Result.User;
                GuardarEnFirestore(user.UserId, nombreCompleto, nombreUsuario, correo);
            });
    }

    /// <summary>
    /// Guarda el perfil completo del nuevo usuario en Firestore, y si todo sale bien,
    /// también lo guarda localmente y lleva al jugador directo al menú principal.
    /// </summary>
    /// <param name="uid">ID que Firebase le asignó al nuevo usuario.</param>
    /// <param name="nombreCompleto">Nombre completo del usuario.</param>
    /// <param name="nombreUsuario">Nombre de usuario elegido.</param>
    /// <param name="correo">Correo del usuario.</param>
    void GuardarEnFirestore(string uid, string nombreCompleto, string nombreUsuario, string correo)
    {
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "uid", uid },
            { "nombreCompleto", nombreCompleto },
            { "username", nombreUsuario },
            { "correo", correo },
            { "fechaRegistro", Timestamp.GetCurrentTimestamp() },
            { "ultimoLogin", Timestamp.GetCurrentTimestamp() }
        };

        db.Collection("usuarios").Document(uid).SetAsync(data)
            .ContinueWithOnMainThread(task =>
            {
                btnRegistro.interactable = true;

                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Error guardando en Firestore: " + task.Exception);
                    if (txtError != null) txtError.text = "Usuario creado, pero no se pudo guardar el perfil.";
                    return;
                }

                PlayerPrefs.SetString("usuario", nombreUsuario);
                PlayerPrefs.SetString("nombreCompleto", nombreCompleto);
                PlayerPrefs.SetString("correo", correo);
                PlayerPrefs.SetString("uid", uid);
                PlayerPrefs.Save();

                if (txtError != null) txtError.text = "Registro exitoso.";

                if (PanelRegistro != null) PanelRegistro.SetActive(false);
                if (PanelInicioSesion != null) PanelInicioSesion.SetActive(false);
                if (PanelMenu != null) PanelMenu.SetActive(true);

                Debug.Log("Usuario registrado y guardado en Firestore");
            });
    }

    // =========================
    // NAVEGACIÓN
    // =========================

    /// <summary>
    /// Cierra el panel de registro y regresa a la pantalla de login.
    /// </summary>
    void VolverAlLogin()
    {
        LimpiarError();

        if (PanelRegistro != null) PanelRegistro.SetActive(false);
        if (PanelInicioSesion != null) PanelInicioSesion.SetActive(true);
        if (PanelMenu != null) PanelMenu.SetActive(false);
    }
}
