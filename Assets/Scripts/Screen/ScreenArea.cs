using UnityEngine;

[ExecuteInEditMode]
public class ScreenArea : MonoBehaviour
{
    private readonly LineRenderer[] lineRenderers = new LineRenderer[4];
    public Vector2 size;

    public Vector3 CurrentCheckpoint;
    
    void Start()
    {
        bool inEditorMode = !Application.isPlaying;
        EnsureLinesExist();

        foreach (var line in lineRenderers)
        {
            if (line != null)
            {
                line.enabled = inEditorMode; // hide in play mode
            }
        }
    }

    void OnValidate()
    {
        EnsureLinesExist();
        UpdateLinePositions();
    }

    void EnsureLinesExist()
    {
        for (int i = 0; i < 4; i++)
        {
            if (lineRenderers[i] == null)
            {
                // Try to find an existing BoundLine_i child
                Transform existingChild = transform.Find("BoundLine_" + i);
                if (existingChild != null)
                {
                    lineRenderers[i] = existingChild.GetComponent<LineRenderer>();
                    if (lineRenderers[i] != null)
                        continue;
                }

                // Otherwise, create a new one
                GameObject lineObj = new GameObject("BoundLine_" + i);
                lineObj.transform.SetParent(this.transform, false);

                var lr = lineObj.AddComponent<LineRenderer>();
                lineRenderers[i] = lr;

                // Configure line renderer
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = UnityEngine.Color.white;
                lr.endColor = UnityEngine.Color.white;

                lr.startWidth = 0.05f;
                lr.endWidth = 0.05f;
                lr.positionCount = 2;
                lr.useWorldSpace = false;
            }
        }
    }

    void UpdateLinePositions()
    {
        if (lineRenderers[0] == null) return;

        // Local space coordinates, bottom-left is (0, 0)
        Vector3 bottomLeft = new(0, 0, 0);
        Vector3 bottomRight = new(size.x, 0, 0);
        Vector3 topRight = new(size.x, size.y, 0);
        Vector3 topLeft = new(0, size.y, 0);

        lineRenderers[0].SetPositions(new Vector3[] { bottomLeft, bottomRight });
        lineRenderers[1].SetPositions(new Vector3[] { bottomRight, topRight });
        lineRenderers[2].SetPositions(new Vector3[] { topRight, topLeft });
        lineRenderers[3].SetPositions(new Vector3[] { topLeft, bottomLeft });
    }
}
