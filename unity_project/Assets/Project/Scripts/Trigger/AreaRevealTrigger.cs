using UnityEngine;

public class AreaRevealTrigger : GridZoneTriggerBase
{
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;

    protected override bool OnTriggered()
    {
        foreach (GameObject obj in objectsToEnable)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        return true;
    }
}
