using UnityEngine;

public class MiniBossVida : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 5;
    private int vidaActual;

    [Header("Visual Placeholder")]
    [SerializeField] private Color colorNormal = new Color(0.5f, 0f, 0.5f); // Púrpura
    [SerializeField] private Color colorDanio = Color.white;
    [SerializeField] private float tiempoFlash = 0.1f;

    [Header("Drop / Muerte")]
    [SerializeField] private GameObject prefabDrop; // Opcional: objeto que suelta al morir

    private SpriteRenderer spriteRenderer;

    // Evento que otros scripts escuchan cuando el boss muere
    public delegate void MuerteMiniBoss();
    public static event MuerteMiniBoss OnMuerte;

    private void Awake()
    {
        vidaActual = vidaMaxima;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = colorNormal;
    }

    public void RecibirDanio(int cantidad)
    {
        vidaActual -= cantidad;

        // Flash blanco al recibir daño
        StartCoroutine(FlashDanio());

        Debug.Log("Mini-Boss recibió daño. Vida: " + vidaActual + "/" + vidaMaxima);

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private System.Collections.IEnumerator FlashDanio()
    {
        spriteRenderer.color = colorDanio;
        yield return new WaitForSeconds(tiempoFlash);
        spriteRenderer.color = colorNormal;
    }

    private void Morir()
    {
        Debug.Log("MINI-BOSS DERROTADO.");

        // Notificar a todos los que escuchan (Spawn Manager, UI, etc.)
        OnMuerte?.Invoke();

        // Efecto visual placeholder: parpadeo antes de morir
        StartCoroutine(MuerteConEfecto());
    }

    private System.Collections.IEnumerator MuerteConEfecto()
    {
        // Parpadeo rápido
        for (int i = 0; i < 5; i++)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            spriteRenderer.color = colorNormal;
            yield return new WaitForSeconds(0.05f);
        }

        // Si hay drop, instanciarlo
        if (prefabDrop != null)
        {
            Instantiate(prefabDrop, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}