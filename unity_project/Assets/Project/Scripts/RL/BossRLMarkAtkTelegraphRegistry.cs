using System.Collections.Generic;
using UnityEngine;

public static class BossRLMarkAtkTelegraphRegistry
{
    private sealed class Entry
    {
        public GameObject owner;
        public readonly List<Vector2Int> cells = new List<Vector2Int>();
        public bool isFake;
        public float expireTime;
    }

    private static readonly List<Entry> entries = new List<Entry>();

    public static void Register(GameObject owner, IReadOnlyList<Vector2Int> visibleCells, bool isFake, float lifetimeSeconds)
    {
        if (owner == null || visibleCells == null || visibleCells.Count == 0)
            return;

        Entry entry = new Entry
        {
            owner = owner,
            isFake = isFake,
            expireTime = Time.time + Mathf.Max(0.01f, lifetimeSeconds)
        };

        foreach (Vector2Int cell in visibleCells)
        {
            if (BossRLStateExtractor.IsArenaCell(cell) && !entry.cells.Contains(cell))
                entry.cells.Add(cell);
        }

        if (entry.cells.Count > 0)
            entries.Add(entry);
    }

    public static Snapshot BuildSnapshot(float[] realMask, float[] fakeMask)
    {
        if (realMask != null)
            System.Array.Clear(realMask, 0, realMask.Length);
        if (fakeMask != null)
            System.Array.Clear(fakeMask, 0, fakeMask.Length);

        Snapshot snapshot = new Snapshot();
        float now = Time.time;

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry entry = entries[i];
            if (entry == null || entry.owner == null || now >= entry.expireTime)
            {
                entries.RemoveAt(i);
                snapshot.staleCount++;
                continue;
            }

            snapshot.activeCount++;
            if (entry.isFake) snapshot.fakeVisibleCount++;
            else snapshot.realVisibleCount++;

            foreach (Vector2Int cell in entry.cells)
            {
                int index = BossRLStateExtractor.ToArenaMaskIndex(cell);
                if (entry.isFake)
                {
                    if (fakeMask != null) fakeMask[index] = 1f;
                    snapshot.fakeVisibleCellCount++;
                }
                else
                {
                    if (realMask != null) realMask[index] = 1f;
                    snapshot.realVisibleCellCount++;
                }
            }
        }

        return snapshot;
    }

    public static void Clear()
    {
        entries.Clear();
    }

    public struct Snapshot
    {
        public int activeCount;
        public int staleCount;
        public int realVisibleCount;
        public int fakeVisibleCount;
        public int realVisibleCellCount;
        public int fakeVisibleCellCount;
    }
}
