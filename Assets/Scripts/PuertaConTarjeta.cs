using UnityEngine;

public class PuertaConTarjeta : MonoBehaviour
{
    [Header("Visual Placeholder")]
    [SerializeField] private Color colorCerrado = new Color(0.2f, 0.2f, 0.2f); // Gris oscuro
    [SerializeField] private Color colorDenegado = Color.red;                  // Rojo flash
    [SerializeField] private Color colorAbierto = new Color(0.2f, 0.8f, 0.2f); // Verde

    private SpriteRenderer spriteRenderer;
    private bool estaAbierta = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = colorCerrado;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (estaAbierta) return;
        if (!other.CompareTag("Player")) return;

        InventarioPlayer inventario = other.GetComponent<InventarioPlayer>();

        if (inventario != null && inventario.TieneTarjeta)
        {
            AbrirPuerta();
        }
        else
        {
            StartCoroutine(FlashDenegado());
        }
    }

    private void AbrirPuerta()
    {
        estaAbierta = true;
        spriteRenderer.color = colorAbierto;
        Debug.Log("Puerta ABIERTA. Acceso permitido.");
    }

    private System.Collections.IEnumerator FlashDenegado()
    {
        spriteRenderer.color = colorDenegado;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = colorCerrado;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = colorDenegado;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = colorCerrado;

        Debug.Log("Puerta CERRADA. Necesitas tarjeta de acceso.");
    }
}