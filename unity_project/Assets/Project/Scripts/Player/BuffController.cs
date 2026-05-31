using System.Collections;
using UnityEngine;

public class BuffController : MonoBehaviour
{
    public bool AttackBuffActive { get; private set; }
    public bool SpeedBuffActive { get; private set; }

    /// <summary>Remaining seconds for the attack buff.</summary>
    public float AttackBuffRemaining { get; private set; }
    /// <summary>Remaining seconds for the speed buff.</summary>
    public float SpeedBuffRemaining { get; private set; }

    private GridMover mover;
    private float baseMoveDuration;

    private Coroutine attackRoutine;
    private Coroutine speedRoutine;

    private void Awake()
    {
        mover = GetComponent<GridMover>();
        if (mover != null)
        {
            baseMoveDuration = mover.moveDuration;
        }
    }

    private void Update()
    {
        if (AttackBuffActive)
        {
            AttackBuffRemaining = Mathf.Max(0f, AttackBuffRemaining - Time.deltaTime);
        }

        if (SpeedBuffActive)
        {
            SpeedBuffRemaining = Mathf.Max(0f, SpeedBuffRemaining - Time.deltaTime);
        }
    }

    public void ApplyAttackBuff(float duration)
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }

        AttackBuffRemaining = duration;
        attackRoutine = StartCoroutine(AttackBuffRoutine(duration));
    }

    public void ApplySpeedBuff(float duration)
    {
        if (speedRoutine != null)
        {
            StopCoroutine(speedRoutine);
        }

        SpeedBuffRemaining = duration;
        speedRoutine = StartCoroutine(SpeedBuffRoutine(duration));
    }

    private IEnumerator AttackBuffRoutine(float duration)
    {
        AttackBuffActive = true;
        yield return new WaitForSeconds(duration);
        AttackBuffActive = false;
        AttackBuffRemaining = 0f;
        attackRoutine = null;
    }

    private IEnumerator SpeedBuffRoutine(float duration)
    {
        SpeedBuffActive = true;
        if (mover != null)
        {
            mover.moveDuration = baseMoveDuration * 0.5f;
        }

        yield return new WaitForSeconds(duration);

        if (mover != null)
        {
            mover.moveDuration = baseMoveDuration;
        }

        SpeedBuffActive = false;
        SpeedBuffRemaining = 0f;
        speedRoutine = null;
    }
}
