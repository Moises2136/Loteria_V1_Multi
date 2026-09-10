using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Este script representa un conjunto de cartas de lotería usando prefabs
/// (objetos prediseñados de Unity), y tiene una función para elegir una
/// al azar e instanciarla en la escena.
/// </summary>
public class LoteryItems : MonoBehaviour
{
    // =========================
    // DATOS DE LA CARTA
    // =========================

    /// <summary>ID único de este conjunto o carta.</summary>
    public int id;

    /// <summary>Descripción de la carta.</summary>
    public string description;

    /// <summary>Texto asociado a la carta (por ejemplo, una frase o narración).</summary>
    public string txt;

    /// <summary>Arreglo de prefabs de cartas disponibles para elegir al azar.</summary>
    public GameObject[] cards;

    // =========================
    // INICIALIZACIÓN
    // =========================

    /// <summary>
    /// Se ejecuta al iniciar. Actualmente no hace nada.
    /// </summary>
    void Start()
    {
        // Actualmente no se ejecuta lógica al iniciar
    }

    // =========================
    // LÓGICA DE LOTERÍA
    // =========================

    /// <summary>
    /// Elige al azar uno de los prefabs de <see cref="cards"/> y lo crea
    /// (instancia) en la posición de este objeto.
    /// </summary>
    /// <remarks>
    /// Este método es privado y actualmente nada lo está llamando dentro
    /// de este mismo script (ni <c>Start</c> ni <c>Update</c> lo invocan),
    /// así que hoy no se ejecuta en tiempo real a menos que algo externo
    /// lo dispare de otra forma (por ejemplo un evento de UI).
    /// </remarks>
    void cardslotery()
    {
        // Validar que existan cartas
        if (cards == null || cards.Length == 0)
        {
            Debug.LogWarning("No hay cartas asignadas.");
            return;
        }

        // Seleccionar índice aleatorio
        int randomIndex = Random.Range(0, cards.Length);

        // Obtener carta seleccionada
        GameObject selectedCard = cards[randomIndex];

        // Instanciar la carta en la escena
        Instantiate(selectedCard, transform.position, Quaternion.identity);
    }

    // =========================
    // UPDATE
    // =========================

    /// <summary>
    /// Se ejecuta en cada frame. Actualmente no hace nada.
    /// </summary>
    void Update()
    {
        // No se usa actualmente
    }
}