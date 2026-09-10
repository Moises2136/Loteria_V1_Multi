using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Este script sirve para poder ejecutar código en el hilo principal de Unity
/// desde otros hilos (como cuando responde Firebase o cualquier otra librería
/// que trabaja en segundo plano). Unity no permite tocar la UI ni ciertas cosas
/// fuera de su hilo principal, así que esta clase guarda esas acciones en una
/// fila y las ejecuta en el momento correcto.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    /// <summary>
    /// Fila donde se guardan las acciones pendientes de ejecutar
    /// en el hilo principal.
    /// </summary>
    private static readonly Queue<Action> executionQueue = new Queue<Action>();

    /// <summary>
    /// Devuelve la única instancia de este dispatcher. Si todavía no existe,
    /// la crea automáticamente.
    /// </summary>
    /// <returns>La instancia activa del dispatcher, lista para usarse.</returns>
    public static UnityMainThreadDispatcher Instance()
    {
        if (!_instance)
        {
            _instance = new GameObject("MainThreadDispatcher")
                .AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(_instance.gameObject);
        }
        return _instance;
    }

    /// <summary>
    /// La única instancia de este dispatcher que existe en el juego (patrón singleton).
    /// </summary>
    private static UnityMainThreadDispatcher _instance;

    /// <summary>
    /// Agrega una acción a la fila para que se ejecute más adelante,
    /// en el hilo principal de Unity.
    /// </summary>
    /// <param name="action">La función que se quiere ejecutar en el hilo principal.</param>
    /// <remarks>
    /// Se usa <c>lock</c> porque esta función puede llamarse desde otros hilos
    /// al mismo tiempo, y así evitamos que dos llamadas choquen entre sí.
    /// </remarks>
    public void Enqueue(Action action)
    {
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// Se ejecuta en cada frame del juego. Revisa si hay acciones pendientes
    /// en la fila y las ejecuta todas, una por una, en el hilo principal.
    /// </summary>
    void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                executionQueue.Dequeue().Invoke();
            }
        }
    }
}