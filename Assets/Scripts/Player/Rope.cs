using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class Rope : MonoBehaviour
{
    private readonly List<RopeSegment> _RopeSegments = new();
    [SerializeField] [Range(1, 50)] private int _SegmentsCount = 35;
    [SerializeField] private float _LineWidth = 0.1f;
    [SerializeField] Vector2 _ForceGravity = new(0f, -1.5f);

    [TitleGroup("Input")] public float Length = 3.5f;
    [TitleGroup("Info"), ReadOnly, SerializeField] public Vector2 EndSegment;
    [TitleGroup("Info"), ReadOnly, SerializeField] private float _RopeSegLen;
    [TitleGroup("Info"), ReadOnly, Required, SerializeField] private LineRenderer _LineRenderer;
    [TitleGroup("Info"), ReadOnly, Required, SerializeField] private bool _Enabled;

    private Transform _AttachTransform;

    public bool Enable() => _Enabled = true; 
    public bool Disable() => _Enabled = false; 
    public bool Toggle() => _Enabled = !_Enabled;

    void Reset(Vector2 direction)
    {
        Vector2 startPoint;
        if (_AttachTransform != null)
            startPoint = _AttachTransform.position;
        else
            startPoint = transform.position;
        
        _RopeSegLen = Length / _SegmentsCount;
        
        Vector2 currentPoint = startPoint;
        for (int i = 0; i < _SegmentsCount; i++)
        {
            _RopeSegments.Add(new RopeSegment(currentPoint));
            currentPoint += direction * _RopeSegLen;
        }
    }

    public void Reset(Transform attachTransform, Vector2 direction, float length)
    {
        Length = length;
        _AttachTransform = attachTransform;
        
        Reset(direction);
    }

    void OnValidate()
    {
        _LineRenderer = GetComponent<LineRenderer>();
        Reset(Vector2.zero);
    }

    void LateUpdate()
    {
        if (!_Enabled)
            return;
        DrawRope();
    }

    void FixedUpdate()
    {
        if (!_Enabled)
            return;
        Simulate();
    }

    void Simulate()
    {
        // SIMULATION
        for (int i = 1; i < _SegmentsCount; i++)
        {
            RopeSegment firstSegment = _RopeSegments[i];
            Vector2 velocity = firstSegment.PosNow - firstSegment.PosOld;
            firstSegment.PosOld = firstSegment.PosNow;
            firstSegment.PosNow += velocity;
            firstSegment.PosNow += _ForceGravity * Time.fixedDeltaTime;
            _RopeSegments[i] = firstSegment;
        }

        // CONSTRAINTS
        for (int i = 0; i < 50; i++)
            ApplyConstraint();
        
        // Info
        EndSegment = _RopeSegments[_SegmentsCount - 1].PosNow;
    }

    void ApplyConstraint()
    {
        // Constraint to given attacher
        RopeSegment firstSegment = _RopeSegments[0];
        firstSegment.PosNow = _AttachTransform?.position ?? new Vector2(0, 0);
        _RopeSegments[0] = firstSegment;
        
        for (int i = 0; i < _SegmentsCount - 1; i++)
        {
            RopeSegment firstSeg = _RopeSegments[i];
            RopeSegment secondSeg = _RopeSegments[i + 1];

            float dist = (firstSeg.PosNow - secondSeg.PosNow).magnitude;
            float error = Mathf.Abs(dist - _RopeSegLen);
            Vector2 changeDir = Vector2.zero;

            if (dist > _RopeSegLen)
            {
                changeDir = (firstSeg.PosNow - secondSeg.PosNow).normalized;
            } else if (dist < _RopeSegLen)
            {
                changeDir = (secondSeg.PosNow - firstSeg.PosNow).normalized;
            }

            Vector2 changeAmount = changeDir * error;
            if (i != 0)
            {
                firstSeg.PosNow -= changeAmount * 0.5f;
                _RopeSegments[i] = firstSeg;
                secondSeg.PosNow += changeAmount * 0.5f;
                _RopeSegments[i + 1] = secondSeg;
            }
            else
            {
                secondSeg.PosNow += changeAmount;
                _RopeSegments[i + 1] = secondSeg;
            }
        }
    }
    
    // TODO: add interpolation
    void DrawRope()
    {
        float lineWidth = _LineWidth;
        _LineRenderer.startWidth = lineWidth;
        _LineRenderer.endWidth = lineWidth;

        Vector3[] ropePositions = new Vector3[_SegmentsCount];
        for (int i = 0; i < _SegmentsCount; i++)
        {
            ropePositions[i] = _RopeSegments[i].PosNow;
        }

        _LineRenderer.positionCount = ropePositions.Length;
        _LineRenderer.SetPositions(ropePositions);
    }

    public struct RopeSegment
    {
        public Vector2 PosNow;
        public Vector2 PosOld;

        public RopeSegment(Vector2 pos)
        {
            PosNow = pos;
            PosOld = pos;
        }
    }
}
