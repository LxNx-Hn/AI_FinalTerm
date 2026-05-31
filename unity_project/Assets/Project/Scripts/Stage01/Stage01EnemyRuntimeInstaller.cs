using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage01EnemyRuntimeInstaller
{
    private const string SceneName = "Stage01_ParkingToHospital";
    private const string VisualChildName = "Stage01_Visual";
    private const string RuntimeEnemyContainerName = "Stage01_RuntimeExtraEnemies";
    private const string BossHitSfxResourcePath = "Boss/BOSSASSET/BOSS/SFX/hit/HIT";
    private const float ContactAttackDistance = 1.01f;
    private const float ContactAttackWindup = 0.28f;
    private const int EnemySpawnSearchRadius = 8;

    private static readonly string[] Variants = { "iv", "patient", "hood" };

    private static readonly Vector2Int[] ExtraEnemyCells =
    {
        new Vector2Int(34, 21),
        new Vector2Int(45, 29),
        new Vector2Int(22, 32),
        new Vector2Int(50, 15),
        new Vector2Int(14, 20),
        new Vector2Int(39, 34),
        new Vector2Int(27, 39)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneName)
        {
            return;
        }

        RunnerHost host = new GameObject("Stage01EnemyRuntimeInstallerHost").AddComponent<RunnerHost>();
        host.StartCoroutine(ApplyAfterSceneStarts(host));
    }

    private static IEnumerator ApplyAfterSceneStarts(RunnerHost host)
    {
        yield return null;
        yield return null;

        if (SceneManager.GetActiveScene().name != SceneName || GridManager.Instance == null)
        {
            Object.Destroy(host.gameObject);
            yield break;
        }

        GameObject template = FindEnemyTemplate();
        NormalizeExistingEnemies();
        SpawnExtraEnemies(template);
        RelocatePinnedEnemies();

        Object.Destroy(host.gameObject);
    }

    private static void NormalizeExistingEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (!IsStage01Enemy(enemy))
            {
                continue;
            }

            int index = ParseEnemyIndex(enemy.name);
            ConfigureEnemy(enemy, VariantForIndex(index), IsFastIndex(index), ShouldUseDirectionalVisual(index));
        }
    }

    private static void SpawnExtraEnemies(GameObject template)
    {
        if (template == null || GameObject.Find(RuntimeEnemyContainerName) != null)
        {
            return;
        }

        GameObject container = new GameObject(RuntimeEnemyContainerName);
        for (int i = 0; i < ExtraEnemyCells.Length; i++)
        {
            Vector2Int cell = FindNearestWalkableCell(ExtraEnemyCells[i]);
            GameObject enemy = Object.Instantiate(template, GridManager.Instance.CellToWorld(cell), Quaternion.identity, container.transform);
            int enemyIndex = 14 + i;
            enemy.name = $"ParkingEnemy_{enemyIndex:00}";

            GridOccupant occupant = enemy.GetComponent<GridOccupant>();
            if (occupant != null)
            {
                occupant.Release();
                occupant.SnapToGridAndRegister();
            }

            ConfigureEnemy(enemy, VariantForIndex(enemyIndex), IsFastIndex(enemyIndex), true);
        }
    }

    private static void ConfigureEnemy(GameObject enemy, string variant, bool fast, bool useDirectionalVisual)
    {
        enemy.transform.localScale = Vector3.one;

        SpriteRenderer rootRenderer = enemy.GetComponent<SpriteRenderer>();
        Animator rootAnimator = enemy.GetComponent<Animator>();

        BoxCollider2D collider = enemy.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size = new Vector2(0.78f, 0.78f);
            collider.offset = new Vector2(0f, 0.39f);
        }

        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.detectRange = Mathf.Max(ai.detectRange, 18);
            ai.wakeRange = Mathf.Max(ai.wakeRange, 24);
            ai.moveInterval = fast ? 0.216f : 0.24f;
            ai.usePathfindingFallback = true;
            ai.pathfindNodeLimit = Mathf.Max(ai.pathfindNodeLimit, 128);
        }

        EnemyAttack attack = enemy.GetComponent<EnemyAttack>();
        if (attack != null)
        {
            attack.maxWorldAttackDistance = ContactAttackDistance;
            attack.contactWindup = Mathf.Max(attack.contactWindup, ContactAttackWindup);
            attack.attackCooldown = Mathf.Max(attack.attackCooldown, 0.9f);
        }

        SpriteRenderer hitFeedbackRenderer = rootRenderer;
        if (useDirectionalVisual)
        {
            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            if (rootAnimator != null)
            {
                rootAnimator.enabled = false;
            }

            SpriteRenderer visualRenderer = EnsureVisualRenderer(enemy, rootRenderer, variant);
            Stage01DirectionalEnemyAnimator animator = visualRenderer.GetComponent<Stage01DirectionalEnemyAnimator>();
            if (animator == null)
            {
                animator = visualRenderer.gameObject.AddComponent<Stage01DirectionalEnemyAnimator>();
            }
            animator.Configure(variant, enemy.transform);
            hitFeedbackRenderer = visualRenderer;
        }
        else
        {
            DisableDirectionalVisual(enemy);
            if (rootRenderer != null)
            {
                rootRenderer.enabled = true;
                rootRenderer.color = Color.white;
            }

            if (rootAnimator != null)
            {
                rootAnimator.enabled = true;
            }
        }

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.spriteRenderer = hitFeedbackRenderer;
            if (health.hitSfx == null)
            {
                health.hitSfx = Resources.Load<AudioClip>(BossHitSfxResourcePath);
            }
            health.hitSfxVolume = Mathf.Min(health.hitSfxVolume, 0.72f);
            health.minHitSfxInterval = Mathf.Max(health.minHitSfxInterval, 0.08f);
        }
    }

    private static SpriteRenderer EnsureVisualRenderer(GameObject enemy, SpriteRenderer rootRenderer, string variant)
    {
        Transform visualTransform = enemy.transform.Find(VisualChildName);
        if (visualTransform == null)
        {
            GameObject visual = new GameObject(VisualChildName);
            visualTransform = visual.transform;
            visualTransform.SetParent(enemy.transform, false);
        }

        visualTransform.localPosition = Vector3.zero;
        visualTransform.localRotation = Quaternion.identity;
        visualTransform.localScale = VisualScale(variant);

        SpriteRenderer visualRenderer = visualTransform.GetComponent<SpriteRenderer>();
        if (visualRenderer == null)
        {
            visualRenderer = visualTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        if (rootRenderer != null)
        {
            visualRenderer.sharedMaterial = rootRenderer.sharedMaterial;
            visualRenderer.sortingLayerID = rootRenderer.sortingLayerID;
            visualRenderer.sortingOrder = rootRenderer.sortingOrder;
            visualRenderer.maskInteraction = rootRenderer.maskInteraction;
        }

        visualRenderer.enabled = true;
        visualRenderer.flipX = false;
        visualRenderer.color = VisualTint(variant);
        return visualRenderer;
    }

    private static void DisableDirectionalVisual(GameObject enemy)
    {
        Transform visualTransform = enemy.transform.Find(VisualChildName);
        if (visualTransform == null)
        {
            return;
        }

        SpriteRenderer visualRenderer = visualTransform.GetComponent<SpriteRenderer>();
        if (visualRenderer != null)
        {
            visualRenderer.enabled = false;
        }

        Stage01DirectionalEnemyAnimator animator = visualTransform.GetComponent<Stage01DirectionalEnemyAnimator>();
        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    private static GameObject FindEnemyTemplate()
    {
        GameObject template = GameObject.Find("ParkingEnemy_03");
        if (template != null)
        {
            return template;
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (IsStage01Enemy(enemy))
            {
                return enemy;
            }
        }

        return null;
    }

    private static bool IsStage01Enemy(GameObject enemy)
    {
        return enemy != null && enemy.name.StartsWith("ParkingEnemy_", System.StringComparison.Ordinal);
    }

    private static bool ShouldUseDirectionalVisual(int index)
    {
        return index > 0;
    }

    private static string VariantForIndex(int index)
    {
        if (index <= 0)
        {
            index = 1;
        }

        return Variants[(index - 1) % Variants.Length];
    }

    private static bool IsFastIndex(int index)
    {
        return index % 4 == 0 || index % 7 == 0;
    }

    private static Vector3 VisualScale(string variant)
    {
        switch (variant)
        {
            case "hood":
                return new Vector3(1.15f, 1.48f, 1f);
            case "iv":
                return new Vector3(1.08f, 1.42f, 1f);
            default:
                return new Vector3(1.05f, 1.36f, 1f);
        }
    }

    private static Color VisualTint(string variant)
    {
        switch (variant)
        {
            case "hood":
                return new Color(0.86f, 0.9f, 1f, 1f);
            case "iv":
                return new Color(1f, 0.9f, 0.9f, 1f);
            default:
                return new Color(0.88f, 1f, 0.9f, 1f);
        }
    }

    private static int ParseEnemyIndex(string name)
    {
        int underscore = name.LastIndexOf('_');
        if (underscore < 0 || underscore >= name.Length - 1)
        {
            return 0;
        }

        return int.TryParse(name.Substring(underscore + 1), out int result) ? result : 0;
    }

    private static Vector2Int FindNearestWalkableCell(Vector2Int preferred)
    {
        if (IsSafeEnemyCell(preferred, null))
        {
            return preferred;
        }

        for (int radius = 1; radius <= EnemySpawnSearchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector2Int candidate = new Vector2Int(preferred.x + x, preferred.y + y);
                    if (IsSafeEnemyCell(candidate, null))
                    {
                        return candidate;
                    }
                }
            }
        }

        return preferred;
    }

    private static void RelocatePinnedEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (!IsStage01Enemy(enemy))
            {
                continue;
            }

            GridOccupant occupant = enemy.GetComponent<GridOccupant>();
            if (occupant == null)
            {
                continue;
            }

            if (CountWalkableNeighbors(occupant.CurrentCell) > 1)
            {
                continue;
            }

            Vector2Int safeCell = FindNearestSafeCell(occupant.CurrentCell, occupant);
            if (safeCell == occupant.CurrentCell)
            {
                continue;
            }

            occupant.Release();
            enemy.transform.position = GridManager.Instance.CellToWorld(safeCell);
            occupant.SnapToGridAndRegister();
        }
    }

    private static Vector2Int FindNearestSafeCell(Vector2Int preferred, GridOccupant occupantToIgnore)
    {
        if (IsSafeEnemyCell(preferred, occupantToIgnore))
        {
            return preferred;
        }

        for (int radius = 1; radius <= EnemySpawnSearchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector2Int candidate = new Vector2Int(preferred.x + x, preferred.y + y);
                    if (IsSafeEnemyCell(candidate, occupantToIgnore))
                    {
                        return candidate;
                    }
                }
            }
        }

        return preferred;
    }

    private static bool IsSafeEnemyCell(Vector2Int cell, GridOccupant occupantToIgnore)
    {
        if (GridManager.Instance == null || GridManager.Instance.IsBlocked(cell))
        {
            return false;
        }

        GridOccupant occupant = GridManager.Instance.GetOccupant(cell);
        if (occupant != null && occupant != occupantToIgnore)
        {
            return false;
        }

        return CountWalkableNeighbors(cell) > 1;
    }

    private static int CountWalkableNeighbors(Vector2Int cell)
    {
        if (GridManager.Instance == null)
        {
            return 0;
        }

        int count = 0;
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Vector2Int direction in directions)
        {
            if (GridManager.Instance.IsWalkable(cell + direction, true))
            {
                count++;
            }
        }

        return count;
    }

    private sealed class RunnerHost : MonoBehaviour
    {
        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}
