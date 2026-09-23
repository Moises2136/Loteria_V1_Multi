using UnityEngine;
using System.Collections.Generic;
using Google;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// Este script controla el inicio de sesión con Google. Al presionar el
/// botón, abre el selector de cuenta de Google (nativo en Android/iOS a
/// través de Firebase), y si el login sale bien, crea el perfil del
/// usuario en Firestore (solo si es su primera vez) y lo lleva directo
/// al menú principal.
/// </summary>
public class LoginGoogle : MonoBehaviour
{
    /// <summary>Botón para iniciar sesión con Google.</summary>
    [Header("UI")]
    public Button btnGoogle;

    /// <summary>Texto usado para mostrar mensajes de estado (cargando, error, bienvenida, etc).</summary>
    public TMP_Text txtEstado;


    /// <summary>Web Client ID (auth_client con client_type 3 en google-services.json).</summary>
    [Header("Google")]
    public string webClientId;

    /// <summary>Panel de inicio de sesión.</summary>
    [Header("Paneles")]
    public GameObject panelInicioSesion;

    /// <summary>Panel del menú principal, al que se entra tras iniciar sesión con éxito.</summary>
    public GameObject panelMenu;

    /// <summary>Referencia al servicio de autenticación de Firebase.</summary>
    private FirebaseAuth auth;

    /// <summary>Referencia a la base de datos de Firestore.</summary>
    private FirebaseFirestore db;

    /// <summary>Indica si Firebase ya terminó de inicializarse.</summary>
    private bool firebaseListo = false;

    /// <summary>
    /// Al iniciar, conecta el botón de Google con su función, y revisa que
    /// todo lo necesario para usar Firebase esté disponible.
    /// </summary>
    void Start()
    {
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestEmail = true,
            RequestIdToken = true
        };
        
        if (btnGoogle != null)
        {
            btnGoogle.onClick.RemoveAllListeners();
            btnGoogle.onClick.AddListener(IniciarFlujoGoogle);
        }

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;
                firebaseListo = true;
            }
            else
            {
                SetEstado("Firebase no disponible.");
            }
        });
    }

    /// <summary>
    /// Abre el selector de cuenta de Google y espera a que el usuario
    /// elija una cuenta e inicie sesión. Si algo falla o se cancela,
    /// muestra el mensaje correspondiente; si sale bien, continúa
    /// guardando los datos del usuario.
    /// </summary>
        void IniciarFlujoGoogle()
    {
        if (!firebaseListo) { SetEstado("Firebase no listo."); return; }

        SetEstado("Abriendo Google...");

        // Paso 1: Google nos da el token de la cuenta elegida
        GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(signInTask =>
        {
            if (signInTask.IsCanceled) { SetEstado("Cancelado."); return; }
            if (signInTask.IsFaulted)
            {
                Debug.LogError(signInTask.Exception);
                SetEstado("Error: " + ObtenerMensaje(signInTask.Exception?.ToString()));
                return;
            }

            // Paso 2: con ese token iniciamos sesión en Firebase
            Credential credential = GoogleAuthProvider.GetCredential(signInTask.Result.IdToken, null);

            auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(authTask =>
            {
                if (authTask.IsCanceled) { SetEstado("Cancelado."); return; }
                if (authTask.IsFaulted)
                {
                    Debug.LogError(authTask.Exception);
                    SetEstado("Error: " + ObtenerMensaje(authTask.Exception?.ToString()));
                    return;
                }

                GuardarYNavegar(authTask.Result);
            });
        });
    }

    /// <summary> No olvidar 1059175276629-226f7v1iflofktvt8nir5qvuuuavsk63.apps.googleusercontent.com
    /// Revisa si el usuario ya tiene un documento en Firestore; si no
    /// existe (es su primer login con Google), lo crea. Después guarda
    /// los datos localmente y pasa al menú principal.
    /// </summary>
    /// <param name="user">El usuario que acaba de iniciar sesión con Google.</param>
    void GuardarYNavegar(FirebaseUser user)
    {
        string correo = user.Email ?? "";
        string nombre = user.DisplayName ?? "";
        string uid = user.UserId ?? "";

        // Crear documento en Firestore si no existe
        db.Collection("usuarios").Document(uid)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(snap =>
          {
              if (!snap.IsFaulted && !snap.Result.Exists)
              {
                  db.Collection("usuarios").Document(uid).SetAsync(
                      new System.Collections.Generic.Dictionary<string, object>
                      {
                          { "correo", correo },
                          { "nombreCompleto", nombre },
                          { "username", nombre },
                          { "uid", uid },
                          { "loginTipo", "google" }
                      });
              }

              PlayerPrefs.SetString("correo", correo);
              PlayerPrefs.SetString("nombreCompleto", nombre);
              PlayerPrefs.SetString("uid", uid);
              PlayerPrefs.SetString("usuario", nombre);
              PlayerPrefs.Save();

              SetEstado("¡Bienvenido, " + nombre + "!");

              if (panelInicioSesion != null) panelInicioSesion.SetActive(false);
              if (panelMenu != null) panelMenu.SetActive(true);
          });
    }

    /// <summary>
    /// Traduce un mensaje de error técnico de Firebase a un mensaje
    /// entendible para el usuario.
    /// </summary>
    /// <param name="err">El mensaje de error crudo devuelto por Firebase.</param>
    /// <returns>Un mensaje de error corto y claro para mostrar en pantalla.</returns>
    string ObtenerMensaje(string err)
    {
        if (err == null) return "Error desconocido.";
        if (err.Contains("account-exists-with-different-credential"))
            return "Ya existe una cuenta con ese correo.";
        if (err.Contains("network-request-failed"))
            return "Sin conexión a internet.";
        if (err.Contains("user-disabled"))
            return "Cuenta deshabilitada.";
        if (err.Contains("canceled") || err.Contains("12501"))
            return "Login cancelado.";
        return "Error al iniciar sesión.";
    }

    /// <summary>
    /// Muestra un mensaje en el texto de estado.
    /// </summary>
    /// <param name="msg">El mensaje a mostrar.</param>
    void SetEstado(string msg)
    {
        if (txtEstado != null) txtEstado.text = msg;
    }
}