using UnityEngine;

public class PickupTarjeta : MonoBehaviour
{
    [SerializeField] private float tiempoParpadeo = 0.3f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player")) return;

        InventarioPlayer inventario = collision.collider.GetComponent<InventarioPlayer>();
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