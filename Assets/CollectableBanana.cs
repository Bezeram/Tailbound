using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CollectableBanana : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        foreach (Vector3 elem in Checkpoint.bananaPositions)
        {
            if (elem == gameObject.transform.position)
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "Player")
        {
            Checkpoint.bananaPositions.Add(new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z));
            Checkpoint.score++;
            Destroy(gameObject);
        }
    }
}
