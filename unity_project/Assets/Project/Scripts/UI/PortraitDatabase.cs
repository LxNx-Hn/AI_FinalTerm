using UnityEngine;

[CreateAssetMenu(menuName = "CodeBlue/PortraitDatabase", fileName = "PortraitDatabase")]
public class PortraitDatabase : ScriptableObject
{
    public static bool UseFinalMergedPortrait { get; set; }

    [Header("SpeakerType별 초상화 (Assets/Project/Profile/)")]
    public Sprite normal;         // portrait_haemin_normal.png
    public Sprite delusion;       // portrait_delusion_00_black.png
    public Sprite nameCrack1;     // portrait_delusion_01_reveal.png
    public Sprite nameCrack2;     // portrait_delusion_02_distorted.png
    public Sprite mergedDelusion; // portrait_delusion_03_merged.png
    public Sprite mergedNormal;   // portrait_haemin_final.png

    private static PortraitDatabase _instance;

    public static PortraitDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<PortraitDatabase>("PortraitDatabase");
            return _instance;
        }
    }

    public Sprite GetPortrait(SpeakerType type)
    {
        switch (type)
        {
            case SpeakerType.Normal:          return normal;
            case SpeakerType.Delusion:        return delusion;
            case SpeakerType.NameCrack1:      return nameCrack1;
            case SpeakerType.NameCrack2:      return nameCrack2;
            case SpeakerType.MergedDelusion:  return mergedDelusion;
            case SpeakerType.MergedNormal:    return UseFinalMergedPortrait ? mergedNormal : normal;
            default:                          return null;
        }
    }
}
