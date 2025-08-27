using Sirenix.OdinInspector;
using TarodevController;
using UnityEngine;

public class Spikes : MonoBehaviour
{
    public enum SpikesDirection
    {
        Up = 0,
        Left = 1,
        Down = 2,
        Right = 3,
    }
    
    [TitleGroup("Input"), Range(1, 20)] public int Count = 1;
    [TitleGroup("Input")] public SpikesDirection Direction = SpikesDirection.Up;

    private BoxCollider2D _Collider;
    private SpriteRenderer _SpriteRenderer;
    private Vector2 _OldColliderSize;
    private Vector2 _OldColliderOffset;

    private readonly Vector2 _DefaultColliderSize = new (0.815382f, 0.1209f);
    private readonly Vector2 _DefaultColliderOffset = new(0.4687891f, 0.06379867f);

    void AdaptSpriteTiling()
    {
        if (_SpriteRenderer == null)
        {
            _SpriteRenderer = GetComponent<SpriteRenderer>(); 
            _SpriteRenderer.drawMode = SpriteDrawMode.Tiled;
        }
        
        _SpriteRenderer.size = Vector2.one + _ExtendDirection * (Count - 1);
    }

    private readonly Vector2 _ExtendDirection = Vector2.right;
    
    void OnValidate()
    {
        if (_Collider == null)
            _Collider = GetComponent<BoxCollider2D>();
        
        // Sprite is rendered as a tile.
        // Extend the tile size depending on the Count.
        AdaptSpriteTiling();
        
        // Rotate based on direction.
        float angle = (int)Direction * 90;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        if (Count == 1)
        {
            // Reset
            _OldColliderSize = _DefaultColliderSize;
            _OldColliderOffset = _DefaultColliderOffset;
            _Collider.size = _DefaultColliderSize;
            _Collider.offset = _DefaultColliderOffset;
            return;
        }
        
        // Extend the box collider depending on the spikes count.
        _Collider.size = _DefaultColliderSize + _ExtendDirection * (Count - 1);
        // Recalculate the offset.
        // This is necessary because the size is based in the center.
        _Collider.offset = _OldColliderOffset + (_Collider.size - _OldColliderSize) / 2f;
        
        // Update "old" values at the next iteration.
        _OldColliderSize = _Collider.size;
        _OldColliderOffset = _Collider.offset;
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name == "Player")
        {
            collision.gameObject.GetComponent<PlayerController>().Die();
        }
    }
}
