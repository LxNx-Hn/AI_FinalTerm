using UnityEngine;

namespace DreamNoms.HeartSystem.Effect
{
    /// <summary>
    /// An effect that alternates between default color and a blink color every 1 second
    /// </summary>
    [CreateAssetMenu(menuName = "Heart Effects/Blink")]
    public class BlinkSO : HeartEffectSO
    {
        [SerializeField]
        private Color blinkColor = Color.lightPink;

        public override void DoEffectOnHeart(Heart targetHeart, int iterations)
        {
            if (iterations%2 == 1)
            {
                targetHeart.SetFillColor(blinkColor);
            }
            else
            {
                targetHeart.ResetFillColor();
            }
        }

        public override void RestorePreviousState(Heart targetHeart)
        {
            targetHeart.ResetFillColor();
        }
    }


}
