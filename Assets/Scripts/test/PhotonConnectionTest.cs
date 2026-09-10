using UnityEngine;

/// <summary>
/// Script simple para probar que el juego arranca correctamente.
/// Solo muestra un mensaje en la consola al iniciar, útil como prueba
/// antes de conectar la lógica real de Photon (multijugador).
/// </summary>
public class PhotonConnectionTest : MonoBehaviour
{
    /// <summary>
    /// Se ejecuta automáticamente cuando el objeto entra en la escena.
    /// Aquí solo confirma que el script se cargó bien.
    /// </summary>
    private void Start()
    {
        Debug.Log("PhotonConnectionTest iniciado.");
    }
}