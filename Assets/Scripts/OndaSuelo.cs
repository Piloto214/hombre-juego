using UnityEngine;

public class OndaSuelo : MonoBehaviour
{
    public int danio = 1;
    public float velocidad = 6f;
    public float alcance = 4f;

    [Header("Visual")]
    [SerializeField] private float anchoOnda = 1.2f;
    [SerializeField] private float grosorLinea = 0.15f;
    [SerializeField] private Color colorOnda = new Color(1f, 0.5f, 0f, 1f);

    private Vector2 origen;
    private Vector2 direccion = Vector2.right;
    private LineRenderer lr;

    private void Awake()
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.startWidth = grosorLinea;
        lr.endWidth = grosorLinea;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = colorOnda;
        lr.endColor = colorOnda;

        lr.SetPosition(0, new Vector3(-anchoOnda / 2f, 0f, 0f));
        lr.SetPosition(1, new Vector3(anchoOnda / 2f, 0f, 0f));
    }

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