using System;
using UnityEngine;

public sealed class PlayerCurrency :
    MonoBehaviour
{
    public static PlayerCurrency Instance
    {
        get;
        private set;
    }

    public event Action OnCoinsChanged;

    [Header("Definition")]

    [SerializeField]
    private CurrencyData currencyDefinition;

    [Header("Balance")]

    [SerializeField]
    [Min(0)]
    private int bronzeCoins;

    public CurrencyData CurrencyDefinition =>
        currencyDefinition;

    public int Coins =>
        bronzeCoins;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        if (Instance != this)
        {
            Destroy(
                gameObject
            );
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public int GetCoins()
    {
        return bronzeCoins;
    }

    public bool HasCoins(
        int amount)
    {
        if (amount < 0)
            return false;

        return bronzeCoins >=
               amount;
    }

    public bool AddCoins(
        int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning(
                $"AddCoins kräver ett positivt belopp. " +
                $"Mottaget värde: {amount}.",
                this
            );

            return false;
        }

        bronzeCoins +=
            amount;

        OnCoinsChanged?.Invoke();

        return true;
    }

    public bool TrySpendCoins(
        int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning(
                $"TrySpendCoins kräver ett positivt belopp. " +
                $"Mottaget värde: {amount}.",
                this
            );

            return false;
        }

        if (bronzeCoins <
            amount)
        {
            return false;
        }

        bronzeCoins -=
            amount;

        OnCoinsChanged?.Invoke();

        return true;
    }

    public void SetCoins(
        int amount)
    {
        int clampedAmount =
            Mathf.Max(
                0,
                amount
            );

        if (bronzeCoins ==
            clampedAmount)
        {
            return;
        }

        bronzeCoins =
            clampedAmount;

        OnCoinsChanged?.Invoke();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        bronzeCoins =
            Mathf.Max(
                0,
                bronzeCoins
            );

        if (currencyDefinition == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayerCurrency)} på '{name}' saknar " +
                $"{nameof(CurrencyData)}.",
                this
            );
        }
    }
#endif
}