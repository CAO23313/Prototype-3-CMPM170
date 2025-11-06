using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RedCoin : MonoBehaviour
{
    private bool collected = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        // Only RedCharacter can collect red coins
        if (other.CompareTag("RedCharacter"))
        {
            collected = true;
            Object.FindFirstObjectByType<GameManager>()?.CollectRedCoin();
            Destroy(gameObject);
        }
    }
}