using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class InicioSesion : MonoBehaviour
{
    public InputField User_name;      // Campo de texto para ingresar nombre
    public Text Player_name_txt;     // Texto donde se muestra el nombre
    public GameObject InicioSesionPanel;   // Panel que se activa al iniciar sesión

    void Start()
    {
        InicioSesionPanel.SetActive(false); // Ocultamos el panel al inicio
    }

    public void IniciarSesion()
    {
        string nombre = User_name.text;

        if (!string.IsNullOrEmpty(nombre))
        {
            Player_name_txt.text = "¡Bienvenido, " + nombre + "!";
            InicioSesionPanel.SetActive(true);
        }
        else
        {
            Player_name_txt.text = "Por favor, ingresa tu nombre.";
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
