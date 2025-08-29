using Sirenix.OdinInspector;
using Unity.Mathematics.Geometry;
using UnityEngine;

public class CollectableBanana : MonoBehaviour
{
    [TitleGroup("References")]
    public LayerMask PlayerLayer;
    public BananaChannel BananaChannel;
    
    [TitleGroup("Input")] 
    public float BobOffset = 0.15f;
    public float BobTime = 1f;
    
    [ReadOnly] public int ID = -1;

    private Vector3 _InitialPosition;
    private float _Timer = 0.5f;
    private int _BobDirection = 1;

    void Start()
    {
        if (BananaChannel == null)
            Debug.LogWarning("BananaChannel has not been set!", context: this);
        _InitialPosition = transform.position;
    }

    void Update()
    {
        _Timer += _BobDirection * Time.deltaTime;
        if (_Timer >= BobTime || _Timer < 0)
        {
            _Timer = Mathf.Clamp(_Timer, 0, BobTime);
            _BobDirection *= -1;
        }
        
        // Bob animation
        float top = BobOffset;
        float bottom = -BobOffset;
        float t = _Timer / BobTime;
        
        transform.position = _InitialPosition + Vector3.up * Mathf.Lerp(bottom, top, t);
    }

    void OnTriggerEnter2D(Collider2D collision)
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
