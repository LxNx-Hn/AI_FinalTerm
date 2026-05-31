public class EnemyActivateTrigger : GridZoneTriggerBase
{
    public EnemyAI[] enemies;

    protected override bool OnTriggered()
    {
        foreach (EnemyAI enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.Activate();
            }
        }

        return true;
    }
}
