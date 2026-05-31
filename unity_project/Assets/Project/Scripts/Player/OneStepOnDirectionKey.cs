using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class OneStepOnDirectionKey : MonoBehaviour
{
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float minimumInterval = 0.05f;
    [SerializeField] private float volume = 0.55f;
    [SerializeField] private float holdIntervalMultiplierMin = 2.2f;
    [SerializeField] private float holdIntervalMultiplierMax = 4f;

    private AudioSource audioSource;
    private float lastPlayTime = -999f;
    private float nextHeldStepTime = -999f;
    private GridMover mover;
    private PlayerHealth health;

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Max(0f, newVolume);
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        mover = GetComponent<GridMover>();
        health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            return;
        }

        if (footstepClips == null || footstepClips.Length == 0)
        {
            return;
        }

        bool pressedThisFrame = PressedDirectionKeyThisFrame();
        bool heldDirection = HeldDirectionKey();

        if (pressedThisFrame)
        {
            TryPlayFootstep();
            return;
        }

        if (!heldDirection)
        {
            nextHeldStepTime = -999f;
            return;
        }

        if (mover != null && mover.IsMoving && Time.unscaledTime >= nextHeldStepTime)
        {
            TryPlayFootstep();
        }
    }

    private void TryPlayFootstep()
    {
        if (Time.unscaledTime - lastPlayTime < minimumInterval)
        {
            return;
        }

        if (mover != null && !mover.IsMoving && !PressedDirectionKeyThisFrame())
        {
            return;
        }

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, volume);
        lastPlayTime = Time.unscaledTime;
        nextHeldStepTime = Time.unscaledTime + GetNextHoldInterval();
    }

    private float GetNextHoldInterval()
    {
        float moveDuration = mover != null ? mover.moveDuration : 0.23f;
        float interval = moveDuration * Random.Range(holdIntervalMultiplierMin, holdIntervalMultiplierMax);
        return Mathf.Max(minimumInterval, interval);
    }

    private static bool PressedDirectionKeyThisFrame()
    {
        return Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.A)
            || Input.GetKeyDown(KeyCode.S)
            || Input.GetKeyDown(KeyCode.D)
            || Input.GetKeyDown(KeyCode.UpArrow)
            || Input.GetKeyDown(KeyCode.LeftArrow)
            || Input.GetKeyDown(KeyCode.DownArrow)
            || Input.GetKeyDown(KeyCode.RightArrow);
    }

    private static bool HeldDirectionKey()
    {
        return Input.GetKey(KeyCode.W)
            || Input.GetKey(KeyCode.A)
            || Input.GetKey(KeyCode.S)
            || Input.GetKey(KeyCode.D)
            || Input.GetKey(KeyCode.UpArrow)
            || Input.GetKey(KeyCode.LeftArrow)
            || Input.GetKey(KeyCode.DownArrow)
            || Input.GetKey(KeyCode.RightArrow);
    }
}
