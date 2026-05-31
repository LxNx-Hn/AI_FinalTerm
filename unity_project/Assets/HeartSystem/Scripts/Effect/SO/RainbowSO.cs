using UnityEngine;

namespace DreamNoms.HeartSystem.Effect
{
    [CreateAssetMenu(menuName = "Heart Effects/Rainbow")]

    public class RainbowSO : HeartEffectSO
    {
        [Tooltip("The number of iterations to complete a full rainbow cycle")]
        public int smoothness = 10;

        [Tooltip("Posive means rainbow moves right. Negative means rainbow moves left. 0 means all hearts are the same color and there is no wave.")]
        [Range(-1, 1)]
        public float waveEffect = 1;

        public override void DoEffectOnHeart(Heart targetHeart, int iterations)
        {
            int siblingIndex=targetHeart.gameObject.transform.GetSiblingIndex();
            float wave = -siblingIndex * waveEffect;

            //make sure wave is positive
            wave += targetHeart.gameObject.transform.parent.childCount;

            // Variables to store the resulting HSV values
            float H, S, V;

            // Convert the RGB color to HSV
            Color.RGBToHSV(targetHeart.CurrentColor, out _, out S, out V);

            //Set the correct hue to shift the color
            H = ((iterations + wave) % smoothness)/(float)smoothness;

            targetHeart.SetFillColor(Color.HSVToRGB(H, S, V));
        }


        public override void RestorePreviousState(Heart targetHeart)
        {
            targetHeart.ResetFillColor();
        }
    }

}

