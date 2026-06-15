using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float fuerzaEmpuje = 8f;
    public float tiempoInvencible = 1f;
    public int vidas = 3;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private bool puedeRecibirDaño = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    public void RecibirGolpe(Vector2 posicionEnemigo)
    {
        if (!puedeRecibirDaño) return;

        vidas--;
        Debug.Log("Vidas restantes: " + vidas);

        Vector2 direccionEmpuje = ((Vector2)transform.position - posicionEnemigo).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode2D.Impulse);

        StartCoroutine(InvencibilidadTemporal());

        if (vidas <= 0)
        {
            Debug.Log("GAME OVER");
        }
    }

    private System.Collections.IEnumerator InvencibilidadTemporal()
    {
        puedeRecibirDaño = false;

        for (int i = 0; i < 5; i++)
        {
            sprite.color = new Color(1, 1, 1, 0.3f);
            yield return new WaitForSeconds(0.1f);
            sprite.color = new Color(1, 1, 1, 1f);
            yield return new WaitForSeconds(0.1f);
        }

        puedeRecibirDaño = true;
    }
}
