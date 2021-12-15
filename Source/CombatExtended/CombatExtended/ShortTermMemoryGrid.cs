using System;
using System.Runtime.CompilerServices;
using Verse;

namespace CombatExtended
{
    public class ShortTermMemoryGrid
    {
        private struct ShortTermRecord
        {
            public int expireAt;
            public float Value
            {
                get
                {
                    int ticks = GenTicks.TicksGame;
                    if (expireAt >= ticks)
                    {
                        return (float)(expireAt - ticks) / 60f;
                    }
                    return 0f;
                }
                set
                {
                    expireAt = Math.Min((int)(value * 60), 1200) + GenTicks.TicksGame;
                }
            }
        }

        public Map map;
        public CellIndices cellIndices;

        private float alpha;
        private ShortTermRecord[] records;

        public ShortTermMemoryGrid(Map map, int unitTicks)
        {
            this.alpha = unitTicks / 60f;
            this.map = map;
            this.cellIndices = map.cellIndices;
            this.records = new ShortTermRecord[this.cellIndices.NumGridCells];
        }

        public float this[IntVec3 cell]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this[cellIndices.CellToIndex(cell)];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this[cellIndices.CellToIndex(cell)] = value;
        }

        public float this[int index]
        {
            get
            {
                if (index >= 0 && index < map.cellIndices.NumGridCells)
                {
                    return records[index].Value;
                }
                return 0f;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (index >= 0 && index < map.cellIndices.NumGridCells)
                {
                    records[index].Value = value;
                }
            }
        }
    }
}

