using UnityEngine;

public class PickupTarjeta : MonoBehaviour
{
    [SerializeField] private float tiempoParpadeo = 0.3f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        InventarioPlayer inventario = other.GetComponent<InventarioPlayer>();
        if (inventario == null) return;

        inventario.RecogerTarjeta();

        StartCoroutine(DestruirConParpadeo());
    }

    private System.Collections.IEnumerator DestruirConParpadeo()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.color = Color.white;
        yield return new WaitForSeconds(tiempoParpadeo / 2f);
        sr.color = Color.yellow;
        yield return new WaitForSeconds(tiempoParpadeo / 2f);

        Destroy(gameObject);
    }
}
