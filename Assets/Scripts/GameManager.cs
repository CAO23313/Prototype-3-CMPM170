using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Coin Settings")]
    [Tooltip("Total number of red coins required")]
    [SerializeField] private int totalRedCoins = 5;

    [Tooltip("Total number of green coins required")]
    [SerializeField] private int totalGreenCoins = 5;

    private int remainingRedCoins;
    private int remainingGreenCoins;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI winText;        // Assign in Inspector
    [SerializeField] private TextMeshProUGUI coinCounterText; // Optional coin counter text

    void Start()
    {
        remainingRedCoins = totalRedCoins;
        remainingGreenCoins = totalGreenCoins;

        if (winText != null)
            winText.gameObject.SetActive(false);

        UpdateUI();
    }

    // Called by RedCoin.cs
    public void CollectRedCoin()
    {
        remainingRedCoins = Mathf.Max(remainingRedCoins - 1, 0);
        Debug.Log($"[GameManager] Red coin collected. Remaining: {remainingRedCoins}/{totalRedCoins}");
        CheckWinCondition();
        UpdateUI();
    }

    // Called by GreenCoin.cs
    public void CollectGreenCoin()
    {
        remainingGreenCoins = Mathf.Max(remainingGreenCoins - 1, 0);
        Debug.Log($"[GameManager] Green coin collected. Remaining: {remainingGreenCoins}/{totalGreenCoins}");
        CheckWinCondition();
        UpdateUI();
    }

    private void CheckWinCondition()
    {
        if (remainingRedCoins <= 0 && remainingGreenCoins <= 0)
        {
            WinGame();
        }
    }

    private void WinGame()
    {
        Debug.Log("[GameManager] You Win!");

        if (winText != null)
        {
            winText.text = "You Win!";
            winText.gameObject.SetActive(true);
        }
    }

    private void UpdateUI()
    {
        if (coinCounterText != null)
        {
            coinCounterText.text = $"Red: {remainingRedCoins}/{totalRedCoins}  |  Green: {remainingGreenCoins}/{totalGreenCoins}";
        }
    }
}

