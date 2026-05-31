using UnityEngine;

namespace DreamNoms.HeartSystem.Effect
{
    /// <summary>
    /// A base class for applying an effect to a Heart
    /// </summary>
    public abstract class HeartEffectSO : ScriptableObject
    {
        [Tooltip("How many seconds to wait in-between repeated effect calls. Inverse of effect speed.")]
        public float repeatRate=1;

        /// <summary>
        /// Called after effect ends. Restore default color or other property to the object
        /// </summary>
        public abstract void RestorePreviousState(Heart heart);

        /// <summary>
        /// Apply an effect to the heart a single time
        /// </summary>
        /// <param name="heart">The heart to apply the effect to</param>
        /// <param name="iterations">The number of times this effect has been applied to the heart since the effect began</param>
        public abstract void DoEffectOnHeart(Heart heart, int iterations);

    }


}
