using UnityEngine;

namespace DreamNoms.HeartSystem.Effect
{
    [CreateAssetMenu(menuName = "Heart Effects/Color Lerp")]
    public class ColorLerpSO : HeartEffectSO
    {
        [Tooltip("The number of iterations to complete a full cycle")]
        public int smoothness = 10;

        [Tooltip("Posive means colors moves right. Negative means colors moves left. 0 means all hearts are the same color and there is no wave.")]
        [Range(-1, 1)]
        public float waveEffect = 1;

        // This public Gradient variable will show a gradient editor in the Inspector
        public Gradient colorGradient;

        public override void DoEffectOnHeart(Heart targetHeart, int iterations)
        {
            int siblingIndex = targetHeart.gameObject.transform.GetSiblingIndex();
            float wave = -siblingIndex * waveEffect;

            //make sure wave is positive
            wave += targetHeart.gameObject.transform.parent.childCount;

            //Set the correct hue to shift the color
            float t = ((iterations + wave) % smoothness) / (float)smoothness;

            targetHeart.SetFillColor(colorGradient.Evaluate(t));
        }


        public override void RestorePreviousState(Heart targetHeart)
        {
            targetHeart.ResetFillColor();
        }
    }

}

