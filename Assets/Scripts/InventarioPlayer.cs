using UnityEngine;

public class InventarioPlayer : MonoBehaviour
{

    [SerializeField] private bool tieneTarjeta = false;

    public bool TieneTarjeta => tieneTarjeta;

    public void RecogerTarjeta()
    {
        tieneTarjeta = true;
        Debug.Log("Tarjeta recogida.");
    }

    
}
