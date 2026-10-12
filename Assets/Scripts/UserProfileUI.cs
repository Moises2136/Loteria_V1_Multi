using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary> 
/// Controla el panel de perfil.
/// Muestra datos del usuario desde PlayerPrefs y Firestore.
/// Permite cambiar username, correo, contraseña, cerrar sesión y regresar al menú.
/// </summary>
public class UserProfileUI : MonoBehaviour
{
    // =========================
    // FIREBASE
    // =========================

    /// <summary>Referencia al servicio de autenticación de Firebase.</summary>
    private FirebaseAuth auth;

    /// <summary>Referencia a la base de datos de Firestore.</summary>
    private FirebaseFirestore db;

    /// <summary>Indica si Firebase ya terminó de inicializarse y está listo para usarse.</summary>
    private bool firebaseListo = false;

    // =========================
    // TEXTOS PERFIL
    // =========================

    /// <summary>Texto que muestra el nombre completo del usuario.</summary>
    [Header("Textos perfil")]
    public TMP_Text txtNombre;

    /// <summary>Texto que muestra el nombre de usuario (username).</summary>
    public TMP_Text txtUsername;

    /// <summary>Texto que muestra la inicial del nombre (usado como avatar simple).</summary>
    public TMP_Text txtInicial;

    /// <summary>Texto usado para mostrar mensajes de estado (errores, confirmaciones, etc).</summary>
    [Header("Estado")]
    public TMP_Text txtEstadoCuenta;

    // =========================
    // BOTONES PRINCIPALES
    // =========================

    /// <summary>Botón para regresar al menú principal.</summary>
    [Header("Botones perfil")]
    public Button btnRegresar;

    /// <summary>Botón para abrir el panel de cambiar username.</summary>
    public Button btnCambiarUsername;

    /// <summary>Botón para abrir el panel de cambiar correo.</summary>
    public Button btnCambiarCorreo;

    /// <summary>Botón para abrir el panel de cambiar contraseña.</summary>
    public Button btnCambiarPassword;

    /// <summary>Botón para cerrar sesión.</summary>
    public Button btnCerrarSesion;

    // =========================
    // PANEL CAMBIAR USERNAME
    // =========================

    /// <summary>Panel donde el usuario escribe su nuevo username.</summary>
    [Header("Panel cambiar username")]
    public GameObject panelCambiarUsername;

    /// <summary>Campo de texto donde se escribe el nuevo username.</summary>
    public TMP_InputField inputNuevoUsername;

    /// <summary>Botón para guardar el nuevo username.</summary>
    public Button btnGuardarUsername;

    /// <summary>Botón para cancelar el cambio de username.</summary>
    public Button btnCancelarUsername;

    // =========================
    // PANEL CAMBIAR CORREO
    // =========================

    /// <summary>Panel donde el usuario escribe su nuevo correo.</summary>
    [Header("Panel cambiar correo")]
    public GameObject panelCambiarCorreo;

    /// <summary>Campo de texto donde se escribe el nuevo correo.</summary>
    public TMP_InputField inputNuevoCorreo;

    /// <summary>Botón para guardar el nuevo correo.</summary>
    public Button btnGuardarCorreo;

    /// <summary>Botón para cancelar el cambio de correo.</summary>
    public Button btnCancelarCorreo;

    // =========================
    // PANEL CAMBIAR CONTRASEÑA
    // =========================

    /// <summary>Panel donde el usuario cambia su contraseña.</summary>
    [Header("Panel cambiar contraseña")]
    public GameObject panelCambiarPassword;

    /// <summary>Campo donde se escribe la contraseña actual (para confirmar identidad).</summary>
    public TMP_InputField inputPasswordActual;

    /// <summary>Campo donde se escribe la nueva contraseña.</summary>
    public TMP_InputField inputNuevaPassword;

    /// <summary>Campo donde se repite la nueva contraseña, para confirmarla.</summary>
    public TMP_InputField inputConfirmarNuevaPassword;

    /// <summary>Botón para guardar la nueva contraseña.</summary>
    public Button btnGuardarPassword;

    /// <summary>Botón para cancelar el cambio de contraseña.</summary>
    public Button btnCancelarPassword;

    // =========================
    // FONDO Y TRANSICIÓN DE SUBPANELES
    // =========================
     
    /// <summary>Fondo oscuro semitransparente que aparece detrás de los subpaneles de edición.</summary>
    [Header("Fondo y transición de subpaneles")]
    public GameObject fondoOscuro;
    
    /// <summary>Duración del fade al abrir/cerrar subpaneles (0 = instantáneo).</summary>
    [Min(0f)] public float duracionFade = 0.2f;
    
    /// <summary>Animación en curso, para cortarla si se abre/cierra otra.</summary>
    private Coroutine rutinaFade;
    /// <summary> Escala inicial de los subpaneles al abrirlos (0.5 = mitad de tamaño, 1 = tamaño normal). </summary>
    [Range(0.5f, 1f)] public float escalaInicial = 0.85f;
    /// </summary>  <summary>
    Dictionary<GameObject, Vector3> escalasOriginales = new Dictionary<GameObject, Vector3>();

    // =========================
    // PANELES PRINCIPALES
    // =========================

    /// <summary>Panel de inicio de sesión (se muestra al cerrar sesión).</summary>
    [Header("Paneles principales")]
    public GameObject panelLogin;

    // =========================
    // INICIALIZACIÓN
    // =========================

    /// <summary>
    /// Al iniciar el juego, prepara los campos de contraseña y conecta con Firebase.
    /// </summary>
    void Start()
    {
        ConfigurarInputsPassword();
        InicializarFirebase();
    }

    /// <summary>
    /// Cada vez que se activa este panel, conecta los botones,
    /// carga los datos del usuario, oculta los paneles secundarios
    /// y limpia cualquier mensaje de estado anterior.
    /// </summary>
    void OnEnable()
    {
        ConfigurarBotones();
        CargarDatosUsuario();
        OcultarPanelesInternos();
        SetEstado("");
    }

    void Awake()
    {
        foreach (GameObject p in Subpaneles())
            if (p != null) escalasOriginales[p] = p.transform.localScale;

        if (fondoOscuro != null)
            escalasOriginales[fondoOscuro] = fondoOscuro.transform.localScale;
    }

    Vector3 EscalaBase(GameObject obj) =>
        escalasOriginales.TryGetValue(obj, out Vector3 s) ? s : Vector3.one;

    /// <summary>
    /// Revisa que todo lo necesario para usar Firebase esté disponible.
    /// Si todo sale bien, deja listos Auth y Firestore, y aprovecha para
    /// refrescar los datos del usuario. Si falla, avisa en pantalla.
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

                Debug.Log("UserProfileUI: Firebase listo");

                CargarDatosUsuarioDesdeFirestore();
                ActualizarCorreoLocalDesdeAuthSilencioso();
            }
            else
            {
                firebaseListo = false;

                Debug.LogError("UserProfileUI: Firebase no disponible: " + task.Result);
                SetEstado("Firebase no está disponible.");
            }
        });
    }

    /// <summary>
    /// Configura los campos de contraseña para que oculten lo que se escribe
    /// (mostrando puntos o asteriscos en vez del texto real).
    /// </summary>
    void ConfigurarInputsPassword()
    {
        if (inputPasswordActual != null)
        {
            inputPasswordActual.contentType = TMP_InputField.ContentType.Password;
            inputPasswordActual.ForceLabelUpdate();
        }

        if (inputNuevaPassword != null)
        {
            inputNuevaPassword.contentType = TMP_InputField.ContentType.Password;
            inputNuevaPassword.ForceLabelUpdate();
        }

        if (inputConfirmarNuevaPassword != null)
        {
            inputConfirmarNuevaPassword.contentType = TMP_InputField.ContentType.Password;
            inputConfirmarNuevaPassword.ForceLabelUpdate();
        }
    }

    // =========================
    // CONFIGURAR BOTONES
    // =========================

    /// <summary>
    /// Conecta cada botón de la pantalla con la función que le corresponde.
    /// Se quita cualquier conexión anterior antes de asignar la nueva,
    /// para evitar que un botón dispare la misma acción varias veces.
    /// </summary>
    void ConfigurarBotones()
    {

        if (btnRegresar != null)
        {
            btnRegresar.onClick.RemoveAllListeners();
            btnRegresar.onClick.AddListener(RegresarMenu);
        }

        if (btnCambiarUsername != null)
        {
            btnCambiarUsername.onClick.RemoveAllListeners();
            btnCambiarUsername.onClick.AddListener(AbrirPanelCambiarUsername);
        }

        if (btnCambiarCorreo != null)
        {
            btnCambiarCorreo.onClick.RemoveAllListeners();
            btnCambiarCorreo.onClick.AddListener(AbrirPanelCambiarCorreo);
        }

        if (btnCambiarPassword != null)
        {
            btnCambiarPassword.onClick.RemoveAllListeners();
            btnCambiarPassword.onClick.AddListener(AbrirPanelCambiarPassword);
        }

        if (btnCerrarSesion != null)
        {
            btnCerrarSesion.onClick.RemoveAllListeners();
            btnCerrarSesion.onClick.AddListener(CerrarSesionDirecto);
        }

        if (btnGuardarUsername != null)
        {
            btnGuardarUsername.onClick.RemoveAllListeners();
            btnGuardarUsername.onClick.AddListener(GuardarNuevoUsername);
        }

        if (btnCancelarUsername != null)
        {
            btnCancelarUsername.onClick.RemoveAllListeners();
            btnCancelarUsername.onClick.AddListener(CerrarPanelCambiarUsername);
        }

        if (btnGuardarCorreo != null)
        {
            btnGuardarCorreo.onClick.RemoveAllListeners();
            btnGuardarCorreo.onClick.AddListener(GuardarNuevoCorreo);
        }

        if (btnCancelarCorreo != null)
        {
            btnCancelarCorreo.onClick.RemoveAllListeners();
            btnCancelarCorreo.onClick.AddListener(CerrarPanelCambiarCorreo);
        }

        if (btnGuardarPassword != null)
        {
            btnGuardarPassword.onClick.RemoveAllListeners();
            btnGuardarPassword.onClick.AddListener(GuardarNuevaPassword);
        }

        if (btnCancelarPassword != null)
        {
            btnCancelarPassword.onClick.RemoveAllListeners();
            btnCancelarPassword.onClick.AddListener(CerrarPanelCambiarPassword);
        }
    }

    // =========================
    // CARGAR PERFIL
    // =========================

    /// <summary>
    /// Muestra primero los datos guardados localmente (PlayerPrefs) para que
    /// la pantalla no se sienta vacía, y de inmediato pide los datos más
    /// actualizados desde Firestore.
    /// </summary>
    void CargarDatosUsuario()
    {
        string nombre = PlayerPrefs.GetString("nombreCompleto", "Usuario");
        string username = PlayerPrefs.GetString("usuario", "username");

        PintarDatosPerfil(nombre, username);

        CargarDatosUsuarioDesdeFirestore();
    }

    /// <summary>
    /// Busca en Firestore los datos más recientes del usuario (nombre, username, correo),
    /// los guarda localmente en PlayerPrefs, y actualiza lo que se ve en pantalla.
    /// </summary>
    void CargarDatosUsuarioDesdeFirestore()
    {
        if (!firebaseListo || auth == null || db == null)
            return;

        FirebaseUser user = auth.CurrentUser;

        string uid = "";

        if (user != null)
            uid = user.UserId;
        else
            uid = PlayerPrefs.GetString("uid", "");

        if (string.IsNullOrEmpty(uid))
            return;

        db.Collection("usuarios")
          .Document(uid)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCanceled || task.IsFaulted)
              {
                  Debug.LogWarning("No se pudieron cargar datos del perfil desde Firestore: " + task.Exception);
                  return;
              }

              DocumentSnapshot doc = task.Result;

              if (doc == null || !doc.Exists)
              {
                  Debug.LogWarning("No existe documento de usuario en Firestore.");
                  return;
              }

              // Solo llenamos los campos que sí vienen en el documento,
              // usando valores por defecto si algo falta
              string nombreCompleto = doc.ContainsField("nombreCompleto")
                  ? doc.GetValue<string>("nombreCompleto")
                  : "Usuario";

              string username = doc.ContainsField("username")
                  ? doc.GetValue<string>("username")
                  : "username";

              string correo = doc.ContainsField("correo")
                  ? doc.GetValue<string>("correo")
                  : "";

              PlayerPrefs.SetString("nombreCompleto", nombreCompleto);
              PlayerPrefs.SetString("usuario", username);
              PlayerPrefs.SetString("uid", uid);

              if (!string.IsNullOrEmpty(correo))
                  PlayerPrefs.SetString("correo", correo);

              PlayerPrefs.Save();

              PintarDatosPerfil(nombreCompleto, username);
          });
    }

    /// <summary>
    /// Actualiza lo que se ve en pantalla: el nombre completo, el username,
    /// y la inicial usada como avatar.
    /// </summary>
    /// <param name="nombre">Nombre completo del usuario.</param>
    /// <param name="username">Nombre de usuario (username).</param>
    void PintarDatosPerfil(string nombre, string username)
    {
        if (txtNombre != null)
            txtNombre.text = nombre;

        if (txtUsername != null)
            txtUsername.text = "@" + username;

        if (txtInicial != null && !string.IsNullOrEmpty(nombre))
            txtInicial.text = nombre.Substring(0, 1).ToUpper();
    }

    // =========================
    // REGRESAR
    // =========================

    /// <summary>
    /// Cierra el panel de perfil y muestra el menú principal.
    /// </summary>
    void RegresarMenu()
    {
        OcultarPanelesInternos();
        
        if (ScreensManage.Instance != null)
        {
            ScreensManage.Instance.ShowMenu();
        }

    }

    // =========================
    // CAMBIAR USERNAME
    // =========================

    /// <summary>
    /// Abre el panel para cambiar el username, precargando el username actual,
    /// y se asegura de que los otros paneles secundarios estén cerrados.
    /// </summary>
    void AbrirPanelCambiarUsername()
    {
        SetEstado("");

        if (inputNuevoUsername != null)
            inputNuevoUsername.text = PlayerPrefs.GetString("usuario", "");

        MostrarSubpanel(panelCambiarUsername);
    }

    /// <summary>
    /// Cierra el panel de cambiar username sin guardar cambios.
    /// </summary>
    void CerrarPanelCambiarUsername()
    {
        CerrarSubpaneles();
        SetEstado("");
    }

    /// <summary>
    /// Valida el nuevo username escrito por el usuario (que no esté vacío,
    /// que tenga al menos 3 caracteres, que sea diferente al actual, y que
    /// nadie más lo esté usando) y, si todo está bien, lo guarda en Firestore.
    /// </summary>
    void GuardarNuevoUsername()
    {
        if (!FirebaseDisponible())
            return;

        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            SetEstado("Vuelve a iniciar sesión para cambiar tu usuario.");
            return;
        }

        string nuevoUsername = inputNuevoUsername != null ? inputNuevoUsername.text.Trim() : "";

        if (string.IsNullOrEmpty(nuevoUsername))
        {
            SetEstado("Escribe un nombre de usuario.");
            return;
        }

        if (nuevoUsername.Length < 3)
        {
            SetEstado("El usuario debe tener al menos 3 caracteres.");
            return;
        }

        string usernameActual = PlayerPrefs.GetString("usuario", "");

        if (nuevoUsername == usernameActual)
        {
            SetEstado("Ese ya es tu nombre de usuario actual.");
            return;
        }

        SetEstado("Validando usuario...");

        if (btnGuardarUsername != null)
            btnGuardarUsername.interactable = false;

        // Antes de guardar, revisamos que ningún otro usuario ya tenga este username
        db.Collection("usuarios")
          .WhereEqualTo("username", nuevoUsername)
          .Limit(1)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCanceled || task.IsFaulted)
              {
                  Debug.LogError("Error validando username: " + task.Exception);
                  SetEstado("No se pudo validar el usuario.");
                  ReactivarBotonUsername();
                  return;
              }

              QuerySnapshot snapshot = task.Result;

              if (snapshot != null && snapshot.Count > 0)
              {
                  SetEstado("Ese nombre de usuario ya está en uso.");
                  ReactivarBotonUsername();
                  return;
              }

              ActualizarUsernameEnFirestore(user.UserId, nuevoUsername);
          });
    }

    /// <summary>
    /// Guarda el nuevo username en Firestore y actualiza los datos locales
    /// y en pantalla si todo sale bien.
    /// </summary>
    /// <param name="uid">ID del usuario en Firebase.</param>
    /// <param name="nuevoUsername">El nuevo username ya validado.</param>
    void ActualizarUsernameEnFirestore(string uid, string nuevoUsername)
    {
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "username", nuevoUsername },
            { "ultimaActualizacionPerfil", Timestamp.GetCurrentTimestamp() }
        };

        db.Collection("usuarios")
          .Document(uid)
          .UpdateAsync(updates)
          .ContinueWithOnMainThread(task =>
          {
              ReactivarBotonUsername();

              if (task.IsCanceled || task.IsFaulted)
              {
                  Debug.LogError("Error actualizando username: " + task.Exception);
                  SetEstado("No se pudo actualizar el usuario.");
                  return;
              }

              PlayerPrefs.SetString("usuario", nuevoUsername);
              PlayerPrefs.Save();

              PintarDatosPerfil(PlayerPrefs.GetString("nombreCompleto", "Usuario"), nuevoUsername);

              if (panelCambiarUsername != null)
                  panelCambiarUsername.SetActive(false);

              SetEstado("Nombre de usuario actualizado.");
          });
    }

    /// <summary>
    /// Vuelve a activar el botón de guardar username (por ejemplo después de un error).
    /// </summary>
    void ReactivarBotonUsername()
    {
        if (btnGuardarUsername != null)
            btnGuardarUsername.interactable = true;
    }

    // =========================
    // CAMBIAR CORREO
    // =========================

    /// <summary>
    /// Abre el panel para cambiar el correo, precargando el correo actual,
    /// y se asegura de que los otros paneles secundarios estén cerrados.
    /// </summary>
    void AbrirPanelCambiarCorreo()
    {
        SetEstado("");

        string correoActual = "";

        if (auth != null && auth.CurrentUser != null && !string.IsNullOrWhiteSpace(auth.CurrentUser.Email))
            correoActual = auth.CurrentUser.Email;
        else
            correoActual = PlayerPrefs.GetString("correo", "");

        if (inputNuevoCorreo != null)
            inputNuevoCorreo.text = correoActual;

        MostrarSubpanel(panelCambiarCorreo);
    }

    /// <summary>
    /// Cierra el panel de cambiar correo sin guardar cambios.
    /// </summary>
    void CerrarPanelCambiarCorreo()
    {
        CerrarSubpaneles();
        SetEstado("");
    }

    /// <summary>
    /// Valida el nuevo correo escrito por el usuario (que no esté vacío, que tenga
    /// formato de correo válido, y que sea diferente al actual) y, si todo está bien,
    /// le manda un correo de verificación antes de aplicar el cambio.
    /// </summary>
    /// <remarks>
    /// Firebase no cambia el correo de inmediato: primero manda un correo de
    /// verificación al nuevo correo, y el cambio se completa cuando el usuario
    /// confirma desde ahí.
    /// </remarks>
    void GuardarNuevoCorreo()
    {
        if (!FirebaseDisponible())
            return;

        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            SetEstado("Vuelve a iniciar sesión para cambiar tu correo.");
            return;
        }

        string nuevoCorreo = inputNuevoCorreo != null ? inputNuevoCorreo.text.Trim() : "";

        if (string.IsNullOrEmpty(nuevoCorreo))
        {
            SetEstado("Escribe tu nuevo correo.");
            return;
        }

        if (!nuevoCorreo.Contains("@") || !nuevoCorreo.Contains("."))
        {
            SetEstado("Correo inválido.");
            return;
        }

        string correoActual = user.Email;

        if (nuevoCorreo == correoActual)
        {
            SetEstado("Ese ya es tu correo actual.");
            return;
        }

        if (btnGuardarCorreo != null)
            btnGuardarCorreo.interactable = false;

        SetEstado("Enviando verificación al nuevo correo...");

        user.SendEmailVerificationBeforeUpdatingEmailAsync(nuevoCorreo)
            .ContinueWithOnMainThread(task =>
            {
                ReactivarBotonCorreo();

                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Error enviando verificación de nuevo correo: " + task.Exception);
                    SetEstado("No se pudo enviar la verificación. Tal vez necesitas iniciar sesión de nuevo.");
                    return;
                }

                if (panelCambiarCorreo != null)
                    panelCambiarCorreo.SetActive(false);

                SetEstado("Listo. Revisa tu nuevo correo y confirma el cambio.");
            });
    }

    /// <summary>
    /// Revisa en segundo plano (sin mostrar mensajes en pantalla) si el correo
    /// registrado en Firebase Auth cambió respecto al que tenemos guardado
    /// localmente, y si es así, actualiza Firestore para mantenerlos sincronizados.
    /// </summary>
    /// <remarks>
    /// Esto cubre el caso en que el usuario confirmó un cambio de correo desde
    /// su bandeja de entrada, y el juego necesita enterarse de ese cambio.
    /// </remarks>
    void ActualizarCorreoLocalDesdeAuthSilencioso()
    {
        if (!firebaseListo || auth == null || db == null)
            return;

        FirebaseUser user = auth.CurrentUser;

        if (user == null)
            return;

        user.ReloadAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogWarning("No se pudo recargar usuario para revisar correo: " + task.Exception);
                return;
            }

            FirebaseUser usuarioActualizado = auth.CurrentUser;

            if (usuarioActualizado == null)
                return;

            string correoAuth = usuarioActualizado.Email;
            string correoLocal = PlayerPrefs.GetString("correo", "");

            if (string.IsNullOrEmpty(correoAuth))
                return;

            if (correoAuth == correoLocal)
                return;

            ActualizarCorreoEnFirestore(usuarioActualizado.UserId, correoAuth, true);
        });
    }

    /// <summary>
    /// Guarda el correo actualizado en Firestore y en los datos locales.
    /// </summary>
    /// <param name="uid">ID del usuario en Firebase.</param>
    /// <param name="nuevoCorreo">El correo ya actualizado.</param>
    /// <param name="silencioso">
    /// Si es verdadero, no muestra mensajes en pantalla (se usa para la
    /// sincronización automática en segundo plano).
    /// </param>
    void ActualizarCorreoEnFirestore(string uid, string nuevoCorreo, bool silencioso = false)
    {
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "correo", nuevoCorreo },
            { "ultimaActualizacionPerfil", Timestamp.GetCurrentTimestamp() }
        };

        db.Collection("usuarios")
          .Document(uid)
          .UpdateAsync(updates)
          .ContinueWithOnMainThread(task =>
          {
              ReactivarBotonCorreo();

              if (task.IsCanceled || task.IsFaulted)
              {
                  Debug.LogError("Error actualizando correo en Firestore: " + task.Exception);

                  if (!silencioso)
                      SetEstado("Correo cambiado, pero no se actualizó el perfil local.");

                  return;
              }

              PlayerPrefs.SetString("correo", nuevoCorreo);
              PlayerPrefs.Save();

              if (!silencioso)
              {
                  if (panelCambiarCorreo != null)
                      panelCambiarCorreo.SetActive(false);

                  SetEstado("Correo actualizado correctamente.");
              }
          });
    }

    /// <summary>
    /// Vuelve a activar el botón de guardar correo (por ejemplo después de un error).
    /// </summary>
    void ReactivarBotonCorreo()
    {
        if (btnGuardarCorreo != null)
            btnGuardarCorreo.interactable = true;
    }

    // =========================
    // CAMBIAR CONTRASEÑA
    // =========================

    /// <summary>
    /// Abre el panel para cambiar la contraseña, limpiando los campos,
    /// y se asegura de que los otros paneles secundarios estén cerrados.
    /// </summary>
    void AbrirPanelCambiarPassword()
    {
        SetEstado("");
        LimpiarInputsPassword();

        MostrarSubpanel(panelCambiarPassword);
    }

    /// <summary>
    /// Cierra el panel de cambiar contraseña sin guardar cambios,
    /// y limpia lo que se haya escrito en los campos.
    /// </summary>
    void CerrarPanelCambiarPassword()
    {
        CerrarSubpaneles();
        LimpiarInputsPassword();
        SetEstado("");
    }

    /// <summary>
    /// Valida los campos de contraseña (que estén completos, que la nueva tenga
    /// al menos 6 caracteres, que coincida con su confirmación, y que sea diferente
    /// a la actual). Si todo está bien, primero confirma la identidad del usuario
    /// con su contraseña actual, y luego aplica el cambio.
    /// </summary>
    /// <remarks>
    /// Por seguridad, Firebase exige "reautenticar" al usuario (confirmar su
    /// contraseña actual) antes de permitir cambiarla.
    /// </remarks>
    void GuardarNuevaPassword()
    {
        if (!FirebaseDisponible())
            return;

        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            SetEstado("Vuelve a iniciar sesión para cambiar tu contraseña.");
            return;
        }

        string correoActual = user.Email;
        string passwordActual = inputPasswordActual != null ? inputPasswordActual.text : "";
        string nuevaPassword = inputNuevaPassword != null ? inputNuevaPassword.text : "";
        string confirmarPassword = inputConfirmarNuevaPassword != null ? inputConfirmarNuevaPassword.text : "";

        if (string.IsNullOrEmpty(correoActual))
        {
            SetEstado("No se encontró el correo actual del usuario.");
            return;
        }

        if (string.IsNullOrEmpty(passwordActual) ||
            string.IsNullOrEmpty(nuevaPassword) ||
            string.IsNullOrEmpty(confirmarPassword))
        {
            SetEstado("Completa todos los campos de contraseña.");
            return;
        }

        if (nuevaPassword.Length < 6)
        {
            SetEstado("La nueva contraseña debe tener al menos 6 caracteres.");
            return;
        }

        if (nuevaPassword != confirmarPassword)
        {
            SetEstado("Las contraseñas nuevas no coinciden.");
            return;
        }

        if (passwordActual == nuevaPassword)
        {
            SetEstado("La nueva contraseña debe ser diferente a la actual.");
            return;
        }

        if (btnGuardarPassword != null)
            btnGuardarPassword.interactable = false;

        SetEstado("Validando contraseña actual...");

        // Confirmamos la identidad del usuario con su contraseña actual
        Credential credencial = EmailAuthProvider.GetCredential(correoActual, passwordActual);

        user.ReauthenticateAsync(credencial).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                ReactivarBotonPassword();
                Debug.LogError("Error reautenticando usuario: " + task.Exception);
                SetEstado("La contraseña actual no es correcta o necesitas iniciar sesión de nuevo.");
                return;
            }

            SetEstado("Actualizando contraseña...");

            // Ya confirmada la identidad, aplicamos el cambio de contraseña
            user.UpdatePasswordAsync(nuevaPassword).ContinueWithOnMainThread(taskPassword =>
            {
                ReactivarBotonPassword();

                if (taskPassword.IsCanceled || taskPassword.IsFaulted)
                {
                    Debug.LogError("Error actualizando contraseña: " + taskPassword.Exception);
                    SetEstado("No se pudo actualizar la contraseña.");
                    return;
                }

                LimpiarInputsPassword();

                if (panelCambiarPassword != null)
                    panelCambiarPassword.SetActive(false);

                SetEstado("Contraseña actualizada correctamente.");
            });
        });
    }

    /// <summary>
    /// Vuelve a activar el botón de guardar contraseña (por ejemplo después de un error).
    /// </summary>
    void ReactivarBotonPassword()
    {
        if (btnGuardarPassword != null)
            btnGuardarPassword.interactable = true;
    }

    /// <summary>
    /// Borra lo escrito en los tres campos de contraseña.
    /// </summary>
    void LimpiarInputsPassword()
    {
        if (inputPasswordActual != null)
            inputPasswordActual.text = "";

        if (inputNuevaPassword != null)
            inputNuevaPassword.text = "";

        if (inputConfirmarNuevaPassword != null)
            inputConfirmarNuevaPassword.text = "";
    }

    // =========================
    // CERRAR SESIÓN
    // =========================

    /// <summary>
    /// Cierra la sesión del usuario, borra sus datos guardados localmente,
    /// oculta todos los paneles del perfil, y muestra la pantalla de login.
    /// </summary>
    void CerrarSesionDirecto()
    {
        if (auth == null)
            auth = FirebaseAuth.DefaultInstance;

        auth.SignOut();

        LimpiarDatosLocalesSesion();
        OcultarPanelesInternos();

        if (ScreensManage.Instance != null)
            ScreensManage.Instance.OcultarTodos();
        
        if (panelLogin != null)
            panelLogin.SetActive(true);

    }

    /// <summary>
    /// Borra de PlayerPrefs los datos del usuario que cerró sesión
    /// (nombre, username, correo y uid).
    /// </summary>
    void LimpiarDatosLocalesSesion()
    {
        PlayerPrefs.DeleteKey("nombreCompleto");
        PlayerPrefs.DeleteKey("usuario");
        PlayerPrefs.DeleteKey("correo");
        PlayerPrefs.DeleteKey("uid");

        PlayerPrefs.Save();
    }

    // =========================
    // HELPERS
    // =========================

    /// <summary>
    /// Revisa si Firebase ya está listo para usarse. Si no lo está,
    /// muestra un mensaje de estado avisándolo.
    /// </summary>
    /// <returns>Verdadero si Firebase está listo para usarse.</returns>
    bool FirebaseDisponible()
    {
        if (!firebaseListo || auth == null || db == null)
        {
            SetEstado("Firebase aún no está listo.");
            return false;
        }

        return true;
    }

    /// <summary>
/// Abre un subpanel de edición con el fondo oscuro, ambos con fade.
/// Los otros subpaneles se apagan al instante.
/// </summary>
/// <param name="panel">Subpanel a mostrar.</param>
void MostrarSubpanel(GameObject panel)
{
    if (panel == null) return;

    foreach (GameObject p in Subpaneles())
    {
        if (p != null && p != panel)
        {
            RestaurarAlpha(p);
            p.SetActive(false);
        }
    }

    if (duracionFade <= 0f || !isActiveAndEnabled)
    {
        if (fondoOscuro != null) fondoOscuro.SetActive(true);
        panel.SetActive(true);
        return;
    }

    if (rutinaFade != null) StopCoroutine(rutinaFade);
    rutinaFade = StartCoroutine(Fade(true, panel));
}

/// <summary>Cierra con fade el subpanel abierto y el fondo oscuro.</summary>
void CerrarSubpaneles()
{
    if (duracionFade <= 0f || !isActiveAndEnabled)
    {
        OcultarPanelesInternos();
        return;
    }

    if (rutinaFade != null) StopCoroutine(rutinaFade);
    rutinaFade = StartCoroutine(Fade(false, null));
}

/// <summary>
/// Anima el alpha del fondo y del subpanel. Al mostrar: de transparente a visible.
/// Al cerrar: de visible a transparente y luego los apaga.
/// </summary>
IEnumerator Fade(bool mostrar, GameObject panel)
{
    var objetos = new List<GameObject>();
    if (fondoOscuro != null) objetos.Add(fondoOscuro);

    if (mostrar)
        objetos.Add(panel);
    else
        foreach (GameObject p in Subpaneles())
            if (p != null && p.activeSelf) objetos.Add(p);

    var grupos = new List<CanvasGroup>();
    foreach (GameObject obj in objetos)
    {
        CanvasGroup cg = ObtenerCanvasGroup(obj);

        if (mostrar && !obj.activeSelf)
        {
            cg.alpha = 0f;
            obj.SetActive(true);
        }

        cg.blocksRaycasts = false;
        grupos.Add(cg);
    }

    var inicio = new List<float>();
    foreach (CanvasGroup cg in grupos) inicio.Add(cg.alpha);

    float destino = mostrar ? 1f : 0f;

    for (float t = 0f; t < duracionFade; t += Time.unscaledDeltaTime)
    {
        float n = t / duracionFade;
        float p = Mathf.SmoothStep(0f, 1f, n);

        // Pop al abrir (con pequeño rebote), encogerse al cerrar
        float escala = mostrar
            ? Mathf.LerpUnclamped(escalaInicial, 1f, EaseOutBack(n))
            : Mathf.Lerp(1f, escalaInicial, p);

        for (int i = 0; i < grupos.Count; i++)
        {
            grupos[i].alpha = Mathf.Lerp(inicio[i], destino, p);

            if (objetos[i] != fondoOscuro)
                objetos[i].transform.localScale = EscalaBase(objetos[i]) * escala;
        }

        yield return null;
    }

    for (int i = 0; i < grupos.Count; i++)
    {
        grupos[i].alpha = 1f;
        grupos[i].blocksRaycasts = true;
        objetos[i].transform.localScale = EscalaBase(objetos[i]);

        if (!mostrar)
            objetos[i].SetActive(false);
    }

    rutinaFade = null;
}

float EaseOutBack(float x)
{
    x = Mathf.Clamp01(x);
    const float c1 = 1.70158f;
    const float c3 = c1 + 1f;
    return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
}

GameObject[] Subpaneles() =>
    new[] { panelCambiarUsername, panelCambiarCorreo, panelCambiarPassword };

CanvasGroup ObtenerCanvasGroup(GameObject obj) =>
    obj.TryGetComponent(out CanvasGroup cg) ? cg : obj.AddComponent<CanvasGroup>();

void RestaurarAlpha(GameObject obj)
{
    if (obj != null && obj.TryGetComponent(out CanvasGroup cg))
    {
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        obj.transform.localScale = EscalaBase(obj);
    }
}

    /// <summary>
    /// Apaga al instante los tres subpaneles y el fondo oscuro,
    /// dejando visible solo el perfil principal.
    /// </summary>
    void OcultarPanelesInternos()
    {
            if (rutinaFade != null)
            {
                StopCoroutine(rutinaFade);
                rutinaFade = null;
            }

        foreach (GameObject p in Subpaneles())
        {
            RestaurarAlpha(p);
            if (p != null) p.SetActive(false);
        }

        RestaurarAlpha(fondoOscuro);
        if (fondoOscuro != null) fondoOscuro.SetActive(false);
    }

    /// <summary>
    /// Muestra un mensaje de estado en pantalla (por ejemplo un error o una confirmación).
    /// </summary>
    /// <param name="mensaje">El mensaje que se va a mostrar.</param>
    void SetEstado(string mensaje)
    {
        if (txtEstadoCuenta != null)
            txtEstadoCuenta.text = mensaje;
    }
}
