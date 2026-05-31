using System.Collections;
using UnityEngine;

public class EyeFollower : MonoBehaviour
{
    [Header("Refs")]
    public Transform pupil;

    [Header("Pupil Motion")]
    // Maximum distance (world units) the pupil center can move from the eye center.
    // Keep this smaller than (eyeRadius - pupilRadius) so it stays inside the sclera.
    public float maxOffset   = 0.28f;
    public float followSpeed = 5f;

    [Header("Pupil Animation")]
    public Sprite[] frames;
    public float    fps = 8f;

    private Transform      _player;
    private SpriteRenderer _pupilSr;
    private float          _animClock;
    private int            _frameIdx;

    private void Awake()
    {
        if (pupil != null)
            _pupilSr = pupil.GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    private void Update()
    {
        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }

        MovePupil();
        AnimatePupil();
    }

    private void MovePupil()
    {
        if (pupil == null) return;

        // Target localPosition (relative to eye center) in the direction of the player
        Vector2 targetLocal = Vector2.zero;
        if (_player != null)
        {
            Vector2 dir = (Vector2)(_player.position - transform.position);
            if (dir.magnitude > 0.001f)
            {
                // Clamp to circle of radius maxOffset
                targetLocal = dir.normalized * Mathf.Min(dir.magnitude, maxOffset);
            }
        }

        // Smoothly move pupil in LOCAL space so it's always relative to eye center
        Vector2 current = pupil.localPosition;
        Vector2 next    = Vector2.Lerp(current, targetLocal, Time.deltaTime * followSpeed);

        // Hard circular clamp — never exits the boundary
        if (next.magnitude > maxOffset)
            next = next.normalized * maxOffset;

        pupil.localPosition = new Vector3(next.x, next.y, 0f);
    }

    private void AnimatePupil()
    {
        if (_pupilSr == null || frames == null || frames.Length == 0) return;
        _animClock += Time.deltaTime;
        float interval = 1f / Mathf.Max(0.01f, fps);
        while (_animClock >= interval)
        {
            _animClock -= interval;
            _frameIdx = (_frameIdx + 1) % frames.Length;
            _pupilSr.sprite = frames[_frameIdx];
        }
    }
}
