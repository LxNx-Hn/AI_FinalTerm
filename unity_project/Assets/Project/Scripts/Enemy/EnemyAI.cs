using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridMover))]
[RequireComponent(typeof(GridOccupant))]
public class EnemyAI : MonoBehaviour
{
    [Header("AI")]
    public float moveInterval = 0.35f;
    public int detectRange = 12;
    public bool wakeOnProximity = true;
    public int wakeRange = 18;
    public bool startActive = false;
    public bool usePathfindingFallback = false;
    public int pathfindNodeLimit = 96;

    private bool active;
    private bool spawnScarePlayed;
    private bool stunned;
    private float timer;

    private GridMover mover;
    private GridOccupant occupant;
    private GridOccupant player;

    private void Awake()
    {
        mover = GetComponent<GridMover>();
        occupant = GetComponent<GridOccupant>();
        active = startActive;
    }

    private void Start()
    {
        CachePlayer();
    }

    private void Update()
    {
        if (player == null)
        {
            CachePlayer();
        }

        if (CutsceneFreezeManager.IsFrozen)
        {
            return;
        }

        if (!active && wakeOnProximity && player != null)
        {
            int wakeDistance = GetDistanceToPlayer();
            if (wakeDistance <= wakeRange)
            {
                Activate();
            }
        }

        if (!active || stunned || player == null || mover.IsMoving)
        {
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            StepTowardPlayer();
            timer = moveInterval;
        }
    }

    public void Activate()
    {
        if (!active && !spawnScarePlayed)
        {
            MonsterSpawnScareEffect.Play(gameObject);
            spawnScarePlayed = true;
        }

        active = true;
    }

    public void SetStunned(bool value)
    {
        stunned = value;
    }

    private void CachePlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<GridOccupant>();
        }
    }

    private void StepTowardPlayer()
    {
        Vector2Int enemyCell = occupant.CurrentCell;
        Vector2Int playerCell = player.CurrentCell;

        int distance = GetDistanceToPlayer();
        if (distance > detectRange)
        {
            return;
        }

        if (distance <= 1)
        {
            return;
        }

        Vector2Int diff = playerCell - enemyCell;
        Vector2Int primary;
        Vector2Int secondary;

        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
        {
            primary = diff.x > 0 ? Vector2Int.right : Vector2Int.left;
            secondary = diff.y > 0 ? Vector2Int.up : Vector2Int.down;
        }
        else
        {
            primary = diff.y > 0 ? Vector2Int.up : Vector2Int.down;
            secondary = diff.x > 0 ? Vector2Int.right : Vector2Int.left;
        }

        Vector2Int tertiary = new Vector2Int(-secondary.x, -secondary.y);
        Vector2Int quaternary = new Vector2Int(-primary.x, -primary.y);

        if (TryMove(primary))
        {
            return;
        }

        if (TryMove(secondary))
        {
            return;
        }

        if (usePathfindingFallback && TryMoveAlongPath(enemyCell, playerCell, primary, secondary, tertiary, quaternary))
        {
            return;
        }

        if (TryMove(tertiary))
        {
            return;
        }

        TryMove(quaternary);
    }

    private bool TryMoveAlongPath(
        Vector2Int start,
        Vector2Int playerCell,
        Vector2Int primary,
        Vector2Int secondary,
        Vector2Int tertiary,
        Vector2Int quaternary)
    {
        if (GridManager.Instance == null)
        {
            return false;
        }

        HashSet<Vector2Int> goals = new HashSet<Vector2Int>();
        Vector2Int[] orderedDirections = { primary, secondary, tertiary, quaternary };
        Vector2Int[] cardinalDirections = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        foreach (Vector2Int direction in cardinalDirections)
        {
            Vector2Int goal = playerCell - direction;
            if (goal == start || GridManager.Instance.IsWalkable(goal))
            {
                goals.Add(goal);
            }
        }

        if (goals.Count == 0)
        {
            return false;
        }

        Queue<PathNode> queue = new Queue<PathNode>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int> { start };
        queue.Enqueue(new PathNode(start, Vector2Int.zero));

        int visitedCount = 0;
        while (queue.Count > 0 && visitedCount < Mathf.Max(16, pathfindNodeLimit))
        {
            PathNode node = queue.Dequeue();
            visitedCount++;

            foreach (Vector2Int direction in orderedDirections)
            {
                if (direction == Vector2Int.zero)
                {
                    continue;
                }

                Vector2Int next = node.Cell + direction;
                if (visited.Contains(next) || !GridManager.Instance.IsWalkable(next))
                {
                    continue;
                }

                Vector2Int firstStep = node.FirstStep == Vector2Int.zero ? direction : node.FirstStep;
                if (goals.Contains(next))
                {
                    return TryMove(firstStep);
                }

                visited.Add(next);
                queue.Enqueue(new PathNode(next, firstStep));
            }
        }

        return false;
    }

    private bool TryMove(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return false;
        }

        if (mover.CanMove(direction))
        {
            mover.Move(direction);
            return true;
        }

        return false;
    }

    private int GetDistanceToPlayer()
    {
        if (player == null || occupant == null)
        {
            return int.MaxValue;
        }

        Vector2Int enemyCell = occupant.CurrentCell;
        Vector2Int playerCell = player.CurrentCell;
        return Mathf.Abs(playerCell.x - enemyCell.x) + Mathf.Abs(playerCell.y - enemyCell.y);
    }

    private readonly struct PathNode
    {
        public PathNode(Vector2Int cell, Vector2Int firstStep)
        {
            Cell = cell;
            FirstStep = firstStep;
        }

        public Vector2Int Cell { get; }
        public Vector2Int FirstStep { get; }
    }
}
