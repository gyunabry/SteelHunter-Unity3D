using System;
using UnityEngine;

/// <summary>Temporary ammo source for the combat slice, replaceable by an inventory adapter.</summary>
public sealed class CharacterAmmoPouch : MonoBehaviour, IAmmoSource
{
    [SerializeField, Min(0)] private int reserveRounds = 60;

    public event Action<int> Changed;
    public int AvailableRounds => reserveRounds;

    public int ConsumeUpTo(int requestedRounds)
    {
        if (requestedRounds <= 0) return 0;

        int consumed = Mathf.Min(requestedRounds, reserveRounds);
        if (consumed <= 0) return 0;

        reserveRounds -= consumed;
        Changed?.Invoke(reserveRounds);
        return consumed;
    }

    public void Add(int rounds)
    {
        if (rounds <= 0) return;

        reserveRounds = rounds > int.MaxValue - reserveRounds
            ? int.MaxValue
            : reserveRounds + rounds;
        Changed?.Invoke(reserveRounds);
    }

    public int GetQuantity(AmmoData ammo)
    {
        throw new NotImplementedException();
    }

    public int ConsumeUpTo(AmmoData ammo, int requestedRounds)
    {
        throw new NotImplementedException();
    }
}
