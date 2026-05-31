using System.Collections;
using UnityEngine;

public class PatternTile : MonoBehaviour
{
    public float lifeTime = 1f;

    private float fillDelay;
    private float fillDuration;
    private Color targetColor;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// BossPatternCaster가 Instantiate 직후 호출. 원형 파동 채우기 애니메이션을 시작한다.
    /// 호출하지 않으면 기존 동작(즉시 표시)이 유지된다.
    /// </summary>
    public void InitFill(float delay, float totalWarningTime, Color finalColor)
    {
        fillDelay = delay;
        fillDuration = Mathf.Max(0.01f, totalWarningTime - delay);
        targetColor = finalColor;

        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = new Color(finalColor.r, finalColor.g, finalColor.b, 0f);

        StartCoroutine(FillAnim(sr));
    }

    private IEnumerator FillAnim(SpriteRenderer sr)
    {
        yield return new WaitForSeconds(fillDelay);
        float t = 0f;
        Color start = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
        while (t < 1f && sr != null)
        {
            t += Time.deltaTime / fillDuration;
            sr.color = Color.Lerp(start, targetColor, Mathf.Clamp01(t));
            yield return null;
        }
        if (sr != null) sr.color = targetColor;
    }
}
