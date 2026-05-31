using UnityEngine;

public class WaveTrigger : GridZoneTriggerBase
{
    public GameObject[] enemiesToEnable;

    protected override bool OnTriggered()
    {
        foreach (GameObject enemy in enemiesToEnable)
        {
            if (enemy == null)
            {
                continue;
            }

            enemy.SetActive(true);
            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.Activate();
            }
        }

        return true;
    }
}
