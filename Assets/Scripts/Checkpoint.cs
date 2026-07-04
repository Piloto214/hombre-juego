using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private bool activado = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activado) return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth jugadorVida = other.GetComponent<PlayerHealth>();
            if (jugadorVida != null)
            {
                jugadorVida.ActualizarCheckpoint(transform.position);
                activado = true;
                Debug.Log("Checkpoint activado: " + gameObject.name);
            }
        }
    }
}