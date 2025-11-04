using UnityEngine;

public class CoinCollection : MonoBehaviour
{
    public string coinColor;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (coinColor == "Green" && other.CompareTag("GreenCharacter"))
        {
            CollectCoin();
        }
        else if (coinColor == "Red" && other.CompareTag("RedCharacter"))
        {
            CollectCoin();
        }
    }

    void CollectCoin()
    {
        Destroy(gameObject);
    }
}

