using UnityEngine;

public class Guajolota : MonoBehaviour
{
    [SerializeField] private float tiempoVida = 3f;

    public int danio = 2;
    public float duracionLentitud = 3f;
    public float multiplicadorLentitud = 0.5f;

    private void Awake()
    {
        Destroy(gameObject, tiempoVida);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth jugadorVida = other.GetComponent<PlayerHealth>();
            if (jugadorVida != null)
            {
                jugadorVida.RecibirGolpe(transform.position, danio);
            }

            PlayerController jugadorControl = other.GetComponent<PlayerController>();
            if (jugadorControl != null)
            {
                jugadorControl.AplicarLentitud(duracionLentitud, multiplicadorLentitud);
            }

            Destroy(gameObject);
        }
        else if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}