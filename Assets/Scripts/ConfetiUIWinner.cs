using UnityEngine;
using UnityEngine.UI;

// Codigo Recuperado de Graficación a computadora que estaba hecho con javascript y lo adapte a C# para Unity xd no me fune maestra jajajaja xd
public class ConfetiUIWinner : MonoBehaviour
{
    public RectTransform confettiPrefab;
    public RectTransform canvasArea;
    public int cantidad = 40;
    public float velocidadCaidaMin = 100f;
    public float velocidadCaidaMax = 250f;

    public Color[] coloresConfeti = new Color[]
    {
        new Color(1f, 0.2f, 0.3f),   // rojo
        new Color(1f, 0.8f, 0.1f),   // amarillo
        new Color(0.2f, 0.7f, 1f),   // azul
        new Color(0.3f, 1f, 0.4f),   // verde
        new Color(1f, 0.4f, 0.8f),   // rosa
    };

    private RectTransform[] piezas;
    private float[] velocidades;
    private float[] rotVelocidades;
    private float ancho, alto;

    void OnEnable()
    {
        ancho = canvasArea.rect.width;
        alto = canvasArea.rect.height;

        piezas = new RectTransform[cantidad];
        velocidades = new float[cantidad];
        rotVelocidades = new float[cantidad];

        for (int i = 0; i < cantidad; i++)
        {
            piezas[i] = Instantiate(confettiPrefab, canvasArea);
            piezas[i].GetComponent<Image>().color =
                coloresConfeti[Random.Range(0, coloresConfeti.Length)];
            ResetPieza(i, true);
        }
    }

    void OnDisable()
    {
        if (piezas == null) return;
        foreach (var p in piezas)
            if (p != null) Destroy(p.gameObject);
        piezas = null;
    }

    void Update()
    {
        if (piezas == null) return;

        for (int i = 0; i < piezas.Length; i++)
        {
            RectTransform p = piezas[i];
            Vector2 pos = p.anchoredPosition;
            pos.y -= velocidades[i] * Time.deltaTime;
            pos.x += Mathf.Sin(Time.time * 2f + i) * 15f * Time.deltaTime;
            p.anchoredPosition = pos;
            p.Rotate(0, 0, rotVelocidades[i] * Time.deltaTime);

            if (pos.y < -alto / 2 - 50f)
                ResetPieza(i, false);
        }
    }

    void ResetPieza(int i, bool posicionAleatoria)
    {
        float x = Random.Range(-ancho / 2, ancho / 2);
        float y = posicionAleatoria
            ? Random.Range(-alto / 2, alto / 2 + 50f)
            : alto / 2 + 50f;

        piezas[i].anchoredPosition = new Vector2(x, y);
        velocidades[i] = Random.Range(velocidadCaidaMin, velocidadCaidaMax);
        rotVelocidades[i] = Random.Range(-180f, 180f);
    }
}