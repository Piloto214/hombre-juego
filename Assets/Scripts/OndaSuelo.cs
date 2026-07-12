using UnityEngine;

public class OndaSuelo : MonoBehaviour
{
    public int danio = 1;
    public float velocidad = 6f;
    public float alcance = 4f;

    private Vector2 origen;
    private Vector2 direccion = Vector2.right;

    private void Start()
    {
        origen = transform.position;
    }

    public void Configurar(Vector2 direccionMovimiento)
    {
        direccion = direccionMovimiento;
    }

    private void Update()
    {
        transform.position += (Vector3)(direccion * velocidad * Time.deltaTime);

        if (Vector2.Distance(origen, transform.position) >= alcance)
        {
            Destroy(gameObject);
        }
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
            Destroy(gameObject);
        }
    }
}