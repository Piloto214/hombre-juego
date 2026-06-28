using UnityEngine;
using System.Collections;

public class PropVida : MonoBehaviour
{
    [Header("Vida")]
    public int vida = 3;
    public bool esCarro = false;

    [Header("Visual - Sprites (opcional)")]
    public Sprite spriteDanado;
    public Sprite spriteDestruido;

    [Header("Feedback de Daño")]
    [SerializeField] private float duracionShake = 0.2f;
    [SerializeField] private float intensidadShake = 0.18f;

    private int vidaActual;
    private SpriteRenderer spriteRenderer;
    private Vector3 posicionLocalOriginal;
    private bool enShake = false;

    void Start()
    {
        vidaActual = vida;
        spriteRenderer = GetComponent<SpriteRenderer>();
        posicionLocalOriginal = transform.localPosition;
    }

    public void RecibirGolpe(int danio)
    {
        vidaActual -= danio;

        if (vidaActual <= 0)
        {
            if (esCarro)
                AbrirCarro();
            else
                Destruir();
            return;
        }

        ActualizarOpacidad();

        if (spriteDanado != null && vidaActual <= vida / 2)
            spriteRenderer.sprite = spriteDanado;

        if (!enShake)
            StartCoroutine(EfectoShake());
    }

    private void ActualizarOpacidad()
    {
        // Golpe 1 (vida 3→2): alpha 0.65 — Golpe 2 (vida 2→1): alpha 0.35
        float proporcion = 1f - ((float)vidaActual / vida);
        float nuevoAlpha = Mathf.Lerp(1f, 0.15f, proporcion);

        Color c = spriteRenderer.color;
        c.a = nuevoAlpha;
        spriteRenderer.color = c;
    }

    private IEnumerator EfectoShake()
    {
        enShake = true;
        float tiempo = 0f;

        while (tiempo < duracionShake)
        {
            float offsetX = Random.Range(-intensidadShake, intensidadShake);
            transform.localPosition = posicionLocalOriginal + new Vector3(offsetX, 0f, 0f);
            tiempo += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = posicionLocalOriginal;
        enShake = false;
    }

    private void AbrirCarro()
    {
        if (spriteDestruido != null)
            spriteRenderer.sprite = spriteDestruido;
        Debug.Log("Carro abierto!");
    }

    private void Destruir()
    {
        Destroy(gameObject);
    }
}