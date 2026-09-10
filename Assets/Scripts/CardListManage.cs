using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Este script se encarga de mostrar visualmente la lista de cartas que se
/// van agregando a una lotería: crea una carta nueva en pantalla cada vez
/// que se llama <see cref="AgregarCartaVisual"/>, y permite borrarlas todas
/// de golpe cuando hace falta reiniciar la vista.
/// </summary>
public class CardListManage : MonoBehaviour
{
    /// <summary>
    /// Contenedor donde se van a colocar las cartas (por ejemplo, un objeto
    /// con Grid Layout o Vertical Layout que las acomoda automáticamente).
    /// </summary>
    public Transform contenedor;

    /// <summary>
    /// Prefab de la carta que se va a crear cada vez. Debe tener el script
    /// <c>CardCartaUI</c> para poder configurarse con nombre e imagen.
    /// </summary>
    public GameObject prefabCarta;

    /// <summary>
    /// Lista interna con todas las cartas que se han creado en pantalla,
    /// usada para poder borrarlas todas después.
    /// </summary>
    private List<GameObject> cartasInstanciadas = new List<GameObject>();

    /// <summary>
    /// Crea una carta nueva en pantalla dentro del contenedor, con el
    /// nombre y la imagen indicados.
    /// </summary>
    /// <param name="nombreCarta">Nombre o título de la carta.</param>
    /// <param name="imagenCarta">Imagen que se va a mostrar en la carta.</param>
    public void AgregarCartaVisual(string nombreCarta, Sprite imagenCarta)
    {
        // Validación para evitar errores si no se asignaron referencias en el inspector
        if (contenedor == null || prefabCarta == null)
        {
            Debug.LogWarning("Contenedor o prefabCarta no asignados.");
            return;
        }

        // Instancia una nueva carta como hijo del contenedor
        GameObject nuevaCarta = Instantiate(prefabCarta, contenedor);

        // Obtiene el script que controla la UI de la carta
        CardCartaUI cartaUI = nuevaCarta.GetComponent<CardCartaUI>();

        // Si el script existe, se configuran sus datos
        if (cartaUI != null)
            cartaUI.Configurar(nombreCarta, imagenCarta);

        // Se guarda la referencia de la carta creada
        cartasInstanciadas.Add(nuevaCarta);
    }

    /// <summary>
    /// Borra todas las cartas que se hayan creado en pantalla hasta ahora,
    /// dejando el contenedor vacío.
    /// </summary>
    public void LimpiarCartas()
    {
        // Recorre todas las cartas instanciadas
        foreach (GameObject carta in cartasInstanciadas)
        {
            // Verifica que no sea null antes de destruirla
            if (carta != null)
                Destroy(carta);
        }

        // Limpia la lista para evitar referencias innecesarias
        cartasInstanciadas.Clear();
    }
}