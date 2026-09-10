using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using TMPro;

/// <summary>
/// Este script maneja el registro, inicio de sesión y cierre de sesión
/// de los usuarios usando Firebase Authentication. También se encarga
/// de mostrar mensajes de estado en la pantalla (por ejemplo "Login exitoso").
/// </summary>
public class FirebaseAuthManager : MonoBehaviour
{
    // =========================
    // CAMPOS DE ENTRADA (UI)
    // =========================

    /// <summary>Campo de texto donde el usuario escribe su correo.</summary>
    [Header("Inputs")]
    public TMP_InputField inputEmail;

    /// <summary>Campo de texto donde el usuario escribe su contraseña.</summary>
    public TMP_InputField inputPassword;

    /// <summary>Texto en pantalla usado para mostrar mensajes de estado (éxito, error, etc).</summary>
    [Header("Debug UI")]
    public TMP_Text textoEstado;

    /// <summary>Referencia al servicio de autenticación de Firebase.</summary>
    private FirebaseAuth auth;

    /// <summary>El usuario actualmente registrado o logueado.</summary>
    private FirebaseUser user;

    /// <summary>
    /// Al iniciar el juego, se prepara la conexión con Firebase.
    /// </summary>
    void Start()
    {
        InicializarFirebase();
    }

    /// <summary>
    /// Revisa que todo lo necesario para usar Firebase esté disponible,
    /// y si es así, deja listo el sistema de autenticación.
    /// </summary>
    void InicializarFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                Debug.Log("Firebase listo ✅");
            }
            else
            {
                Debug.LogError("Firebase NO disponible ❌");
            }
        });
    }

    // =========================
    // REGISTRO
    // =========================

    /// <summary>
    /// Crea una cuenta nueva usando el correo y contraseña escritos en los campos de texto.
    /// </summary>
    public void RegistrarUsuario()
    {
        string email = inputEmail.text;
        string password = inputPassword.text;

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                MostrarEstado("Registro cancelado");
                return;
            }
            if (task.IsFaulted)
            {
                MostrarEstado("Error registro: " + task.Exception);
                return;
            }

            // Si todo salió bien, guardamos al usuario recién creado
            user = task.Result.User;
            MostrarEstado("Usuario registrado ✅");
            Debug.Log("Usuario creado: " + user.Email);
        });
    }

    // =========================
    // LOGIN
    // =========================

    /// <summary>
    /// Intenta iniciar sesión con el correo y contraseña escritos en los campos de texto.
    /// </summary>
    public void LoginUsuario()
    {
        string email = inputEmail.text;
        string password = inputPassword.text;

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                MostrarEstado("Login cancelado");
                return;
            }
            if (task.IsFaulted)
            {
                MostrarEstado("Error login: " + task.Exception);
                return;
            }

            // Si todo salió bien, guardamos al usuario que inició sesión
            user = task.Result.User;
            MostrarEstado("Login exitoso 🎉");
            Debug.Log("Logeado: " + user.Email);
        });
    }

    // =========================
    // LOGOUT
    // =========================

    /// <summary>
    /// Cierra la sesión del usuario actual.
    /// </summary>
    public void Logout()
    {
        auth.SignOut();
        MostrarEstado("Sesión cerrada");
    }

    // =========================
    // UI THREAD SAFE
    // =========================

    /// <summary>
    /// Muestra un mensaje de estado tanto en la consola como en pantalla.
    /// </summary>
    /// <param name="mensaje">El mensaje que se va a mostrar.</param>
    /// <remarks>
    /// Firebase a veces responde desde un hilo distinto al principal de Unity,
    /// y Unity no permite actualizar la UI fuera de su hilo principal.
    /// Por eso se usa <c>UnityMainThreadDispatcher</c>: para asegurarnos de que
    /// el cambio de texto en pantalla se haga en el hilo correcto y no truene el juego.
    /// </remarks>
    void MostrarEstado(string mensaje)
    {
        Debug.Log(mensaje);

        // IMPORTANTE: volver al hilo principal
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (textoEstado != null)
                textoEstado.text = mensaje;
        });
    }
}