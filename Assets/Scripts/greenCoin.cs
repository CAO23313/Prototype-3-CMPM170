using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GreenCoin : MonoBehaviour
{
    private bool collected = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        // Only GreenCharacter can collect green coins
        if (other.CompareTag("GreenCharacter"))
        {
            collected = true;
            Object.FindFirstObjectByType<GameManager>()?.CollectGreenCoin();
            Destroy(gameObject);
        }
    }
}