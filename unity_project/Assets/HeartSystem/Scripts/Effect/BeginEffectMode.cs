namespace DreamNoms.HeartSystem.Effect
{
    /// <summary>
    /// Single only allows one effect on the hearts
    /// Additive will stack the effects.
    /// Additive has unpredictable results when combining ColorLerp effect or effect types 
    /// </summary>
    public enum BeginEffectMode
    {
        Single,
        Additive
    }
}
