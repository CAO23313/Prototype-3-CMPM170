using UnityEngine;
using UnityEngine.UI; 
using TMPro;

public class GameManager : MonoBehaviour
{
    private int totalCoins; // Total number of coins in the scene
    private int remainingCoins; // Number of coins left in the scene
    [SerializeField] private TextMeshProUGUI winText; // UI Text to show win message

    void Start()
    {
        CoinCollection[] coins = FindObjectsOfType<CoinCollection>();
        totalCoins = coins.Length;
        remainingCoins = totalCoins;

        if (winText != null)
        {
            winText.enabled = false;
        }
    }

    public void CollectCoin()
    {
        remainingCoins--;

        if (remainingCoins <= 0)
        {
            WinGame();
        }
    }

    private void WinGame()
    {
        if (winText != null)
        {
            winText.text = "You Win!";
            winText.enabled = true;
        }
    }
}
