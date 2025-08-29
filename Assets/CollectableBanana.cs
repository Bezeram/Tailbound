using UnityEngine;

public class CollectableBanana : MonoBehaviour
{
    public LayerMask PlayerLayer;
    public BananaChannel BananaChannel; 
    public int ID = -1;

    void Start()
    {
        if (ID == -1)
            Debug.LogWarning("ID has not been set!", context: this);
        if (BananaChannel == null)
            Debug.LogWarning("BananaChannel has not been set!", context: this);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (ID == -1)
            Debug.LogWarning("ID has not been set!", context: this);
        
        if (Utils.IsInMask(collision.gameObject.layer, PlayerLayer))
        {
            BananaChannel?.Raise(this);
            
            Destroy(gameObject);
        }
    }
}
