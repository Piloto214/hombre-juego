using UnityEngine;

public class PropVida : MonoBehaviour
{
    [Header("Vida")]
    public int vida = 3;
    public bool esCarro = false;

    [Header("Visual")]
    public Sprite spriteDanado;
    public Sprite spriteDestruido;

    private int vidaActual;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        vidaActual = vida;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void RecibirGolpe(int danio)
    {
        vidaActual -= danio;

        // Cambiar sprite segun vida restante
        if (vidaActual <= 0)
        {
            if (esCarro)
            {
                AbrirCarro();
            }
            else
            {
                Destruir();
            }
        }
        else if (vidaActual <= vida / 2 && spriteDanado != null)
        {
            spriteRenderer.sprite = spriteDanado;
        }
    }

    void AbrirCarro()
    {
        // El carro se abre pero no se destruye
        if (spriteDestruido != null)
            spriteRenderer.sprite = spriteDestruido;

        // Aqui puedes añadir mas efectos despues
        Debug.Log("Carro abierto!");
    }

    void Destruir()
    {
        // El poste se destruye
        Destroy(gameObject);
    }
}