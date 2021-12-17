using System;
using Verse;

namespace CombatExtended
{
    public class WallGrid : MapComponent
    {
        private readonly CellIndices cellIndices;
        private readonly float[] grid;

        public WallGrid(Map map) : base(map)
        {
            cellIndices = map.cellIndices;
            grid = new float[cellIndices.NumGridCells];            
        }

        public FillCategory GetFillCategory(IntVec3 cell) => GetFillCategory(cellIndices.CellToIndex(cell));
        public FillCategory GetFillCategory(int index)
        {
            float f = grid[index];
            if (f == 0)
                return FillCategory.None;
            else if (f < 1f)
                return FillCategory.Partial;
            else
                return FillCategory.Full;
        }

        public bool CanBeSeenOver(IntVec3 cell) => CanBeSeenOver(cellIndices.CellToIndex(cell));
        public bool CanBeSeenOver(int index)
        {
            return grid[index] < 0.998f;
        }

        public float this[IntVec3 cell]
        {
            get => this[cellIndices.CellToIndex(cell)];
            set => this[cellIndices.CellToIndex(cell)] = value;
        }

        public float this[int index]
        {
            get => grid[index];
            set
            {    
                grid[index] = value;                
            }
        }
    }
}

