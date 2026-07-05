using UnityEngine;
using System.Collections;

public class PropVida : MonoBehaviour
{
    [Header("Vida")]
    public int vida = 3;
    public bool esCarro = false;

    [Header("Rejilla (Pared Falsa)")]
    public bool esRejilla = false;
    [Tooltip("El Collider2D que define el límite de cámara de la SIGUIENTE zona. Se activa cuando esta rejilla se rompe.")]
    public Collider2D limiteCamaraNuevo;

    [Header("Alcantarilla (Piso Rompible)")]
    public bool esAlcantarilla = false;
    [Tooltip("El Collider2D que define el límite de cámara de la alcantarilla. Se activa cuando este piso se rompe.")]
    public Collider2D limiteCamaraAlcantarilla;

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

    // Evento estático para rejillas
    public static event System.Action<Collider2D> OnRejillaDestruida;
    // Evento estático para alcantarillas
    public static event System.Action<Collider2D> OnAlcantarillaDestruida;

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
            else if (esRejilla)
                DestruirRejilla();
            else if (esAlcantarilla)
                DestruirAlcantarilla();
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

    private void DestruirRejilla()
    {
        OnRejillaDestruida?.Invoke(limiteCamaraNuevo);
        Destroy(gameObject);
    }

    private void DestruirAlcantarilla()
    {
        OnAlcantarillaDestruida?.Invoke(limiteCamaraAlcantarilla);
        Destroy(gameObject);
    }

    private void Destruir()
    {
        Destroy(gameObject);
    }
}