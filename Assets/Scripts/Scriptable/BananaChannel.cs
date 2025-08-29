using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BananaChannel", menuName = "Scriptable Objects/BananaChannel")]
public class BananaChannel : ScriptableObject
{
    public event Action<CollectableBanana> OnRaised;

    public void Raise(CollectableBanana banana)
    {
        OnRaised?.Invoke(banana);
    }
}
