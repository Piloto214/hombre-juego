using UnityEngine;

public class TazaCafe : MonoBehaviour
{
    [SerializeField] private float tiempoVida = 3f;
    [SerializeField] private float radioMancha = 1.5f;
    [SerializeField] private float duracionMancha = 2f;

    public int danio = 1;

    private void Awake()
    {
        Destroy(gameObject, tiempoVida);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth jugador = other.GetComponent<PlayerHealth>();
            if (jugador != null)
            {
                jugador.RecibirGolpe(transform.position, danio);
            }
            Destroy(gameObject);
        }
        else if (other.CompareTag("Ground"))
        {
            CrearMancha();
            Destroy(gameObject);
        }
    }

    private void CrearMancha()
    {
        GameObject mancha = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mancha.name = "ManchaCafe";
        mancha.transform.position = new Vector3(transform.position.x, transform.position.y, 0);
        mancha.transform.localScale = new Vector3(radioMancha, radioMancha, 1);

        Renderer rend = mancha.GetComponent<Renderer>();
        rend.material.color = new Color(0.4f, 0.2f, 0.1f, 0.6f);

        Destroy(mancha, duracionMancha);
    }
}