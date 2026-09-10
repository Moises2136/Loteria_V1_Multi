using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// Este script decide qué pantalla mostrar al abrir la app: revisa si ya
/// hay una sesión activa en Firebase y, según eso, manda al usuario al login
/// o directo al menú (cargando su perfil). También se coordina con la
/// pantalla de splash para no cambiar de pantalla antes de tiempo.
/// </summary>
public class SessionManager : MonoBehaviour
{
    // =========================
    // PANELES
    // =========================

    /// <summary>Panel de inicio de sesión.</summary>
    [Header("Paneles")]
    public GameObject panelLogin;

    /// <summary>Panel del menú principal.</summary>
    public GameObject panelMenu;

    /// <summary>Panel de registro de cuenta nueva.</summary>
    public GameObject panelRegistro;

    /// <summary>Panel de perfil del usuario.</summary>
    public GameObject panelPerfil;

    // =========================
    // FIREBASE
    // =========================

    /// <summary>Referencia al servicio de autenticación de Firebase.</summary>
    private FirebaseAuth auth;

    /// <summary>Referencia a la base de datos de Firestore.</summary>
    private FirebaseFirestore db;

    /// <summary>El usuario actualmente detectado como logueado (o null si no hay sesión).</summary>
    private FirebaseUser userActual;

    /// <summary>Indica si Firebase ya terminó de inicializarse.</summary>
    private bool firebaseListo = false;

    // =========================
    // COORDINACIÓN CON EL SPLASH
    // =========================

    /// <summary>Indica si la pantalla de splash (carga inicial) ya terminó.</summary>
    private bool splashTerminado = false;

    /// <summary>
    /// Si se detecta una sesión activa antes de que termine el splash,
    /// se guarda aquí temporalmente para procesarla en cuanto el splash acabe.
    /// </summary>
    private FirebaseUser usuarioPendiente = null;

    /// <summary>
    /// Si se detecta que NO hay sesión activa antes de que termine el splash,
    /// se marca aquí para mostrar el login en cuanto el splash acabe.
    /// </summary>
    private bool sinSesionPendiente = false;

    /// <summary>
    /// Al iniciar el juego, se suscribe al aviso de que el splash terminó
    /// y arranca la conexión con Firebase.
    /// </summary>
    void Start()
    {
        OpenApp.OnSplashTerminado += ManejarSplashTerminado;
        InicializarFirebase();
    }

    /// <summary>
    /// Revisa que todo lo necesario para usar Firebase esté disponible.
    /// Si todo sale bien, deja listos Auth y Firestore, y empieza a escuchar
    /// cambios de sesión. Si falla, decide si mostrar el login de inmediato
    /// o esperar a que el splash termine.
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

                auth.StateChanged += AuthStateChanged;

                // Forzamos una primera revisión al iniciar la app
                AuthStateChanged(this, null);

                Debug.Log("SessionManager: Firebase listo.");
            }
            else
            {
                firebaseListo = false;
                Debug.LogError("SessionManager: Firebase no disponible: " + task.Result);

                if (splashTerminado)
                {
                    MostrarLogin();
                }
                else
                {
                    sinSesionPendiente = true;
                }
            }
        });
    }

    /// <summary>
    /// Se ejecuta cuando la pantalla de splash termina. Si mientras tanto
    /// ya se había detectado una sesión activa (o la falta de una), aquí
    /// se procesa esa decisión pendiente.
    /// </summary>
    void ManejarSplashTerminado()
    {
        splashTerminado = true;

        if (usuarioPendiente != null)
        {
            CargarPerfilDesdeFirestore(usuarioPendiente);
        }
        else if (sinSesionPendiente)
        {
            MostrarLogin();
        }
    }

    /// <summary>
    /// Se llama automáticamente cada vez que cambia el estado de sesión en Firebase
    /// (el usuario inicia sesión, cierra sesión, etc). Si el splash ya terminó,
    /// actúa de inmediato; si no, guarda la decisión para aplicarla después.
    /// </summary>
    /// <param name="sender">Objeto que dispara el evento (no se usa).</param>
    /// <param name="eventArgs">Datos del evento (no se usan).</param>
    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (!firebaseListo || auth == null)
            return;

        FirebaseUser nuevoUsuario = auth.CurrentUser;

        // Solo actuamos si de verdad cambió el usuario (evita procesar el mismo estado dos veces)
        if (nuevoUsuario != userActual)
        {
            userActual = nuevoUsuario;

            if (userActual != null)
            {
                Debug.Log("Sesión activa detectada: " + userActual.UserId);

                if (!splashTerminado)
                {
                    usuarioPendiente = userActual;
                    return;
                }

                CargarPerfilDesdeFirestore(userActual);
            }
            else
            {
                Debug.Log("No hay sesión activa.");

                if (!splashTerminado)
                {
                    sinSesionPendiente = true;
                    return;
                }
                MostrarLogin();
            }
        }
    }

    /// <summary>
    /// Busca en Firestore los datos del perfil del usuario logueado
    /// y los guarda localmente, para luego mostrar el menú principal.
    /// </summary>
    /// <param name="user">El usuario cuyo perfil se va a cargar.</param>
    /// <remarks>
    /// Si Firestore falla o el documento del usuario no existe, igual se
    /// deja entrar al menú (usando los datos básicos de Firebase Auth),
    /// para que un problema de red no bloquee el acceso a alguien que ya
    /// inició sesión correctamente.
    /// </remarks>
    void CargarPerfilDesdeFirestore(FirebaseUser user)
    {
        if (user == null || db == null)
        {
            MostrarLogin();
            return;
        }

        string uid = user.UserId;

        db.Collection("usuarios")
          .Document(uid)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCanceled || task.IsFaulted)
              {
                  Debug.LogWarning("No se pudo cargar perfil desde Firestore: " + task.Exception);

                  // Aunque falle Firestore, si Firebase Auth tiene sesión,
                  // dejamos entrar al menú.
                  PlayerPrefs.SetString("uid", uid);

                  if (!string.IsNullOrEmpty(user.Email))
                      PlayerPrefs.SetString("correo", user.Email);

                  PlayerPrefs.Save();

                  MostrarMenu();
                  return;
              }

              DocumentSnapshot doc = task.Result;

              if (doc != null && doc.Exists)
              {
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
                      : user.Email;

                  PlayerPrefs.SetString("nombreCompleto", nombreCompleto);
                  PlayerPrefs.SetString("usuario", username);
                  PlayerPrefs.SetString("correo", correo);
                  PlayerPrefs.SetString("uid", uid);
                  PlayerPrefs.Save();

                  Debug.Log("Perfil cargado automáticamente: " + username);
              }
              else
              {
                  Debug.LogWarning("No existe documento usuarios/" + uid);

                  PlayerPrefs.SetString("uid", uid);

                  if (!string.IsNullOrEmpty(user.Email))
                      PlayerPrefs.SetString("correo", user.Email);

                  PlayerPrefs.Save();
              }

              MostrarMenu();
          });
    }

    /// <summary>
    /// Muestra la pantalla de login y oculta las demás pantallas principales.
    /// </summary>
    void MostrarLogin()
    {
        if (panelLogin != null)
            panelLogin.SetActive(true);

        if (panelMenu != null)
            panelMenu.SetActive(false);

        if (panelRegistro != null)
            panelRegistro.SetActive(false);

        if (panelPerfil != null)
            panelPerfil.SetActive(false);
    }

    /// <summary>
    /// Muestra el menú principal y oculta las demás pantallas principales.
    /// </summary>
    void MostrarMenu()
    {
        if (panelLogin != null)
            panelLogin.SetActive(false);

        if (panelRegistro != null)
            panelRegistro.SetActive(false);

        if (panelPerfil != null)
            panelPerfil.SetActive(false);

        if (panelMenu != null)
            panelMenu.SetActive(true);
    }

    /// <summary>
    /// Se ejecuta cuando este objeto se destruye. Se desconecta de los eventos
    /// a los que estaba suscrito, para evitar errores o fugas de memoria.
    /// </summary>
    void OnDestroy()
    {
        OpenApp.OnSplashTerminado -= ManejarSplashTerminado;

        if (auth != null)
            auth.StateChanged -= AuthStateChanged;
    }
}