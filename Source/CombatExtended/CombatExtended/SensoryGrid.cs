using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Steamworks;
using System.Text;
using System.Runtime.CompilerServices;

namespace CombatExtended
{
    /*
     * -----------------------------
     *
     *
     * ------ Important note -------
     * 
     * when casting update the grid at a regualar intervals for a pawn/Thing or risk exploding value issues.
     */    
    public abstract class SensoryGrid
    {
        [StructLayout(LayoutKind.Auto)]
        protected struct SensoryRecord
        {
            /// <summary>
            /// At what cycle does this record expire.
            /// </summary>
            public int expireAt;
            /// <summary>
            /// Used to prevent set a record twice in a single operation.
            /// </summary>
            public short sig;
            /// <summary>
            /// The number of overlaping casts.
            /// </summary>
            public short signalNum;
            /// <summary>
            /// The previous number of overlaping casts.
            /// </summary>
            public short signalNumPrev;
            /// <summary>
            /// Indicates how much this cell is visible/close to casters.
            /// </summary>
            public float signalStrength;
            /// <summary>
            /// The previous visibility value.
            /// </summary>
            public float signalStrengthPrev;
            /// <summary>
            /// The general direction of casters.
            /// </summary>
            public Vector2 direction;
            /// <summary>
            /// The previous general direction of casters;
            /// </summary>
            public Vector2 directionPrev;
            /// <summary>
            /// A bit map that is used to indicate an pool of potential casters.
            /// </summary>
            public UInt64 sourceFlags;
            /// <summary>
            /// The previous caster flags.
            /// </summary>
            public UInt64 sourceFlagsPrev;
            /// <summary>
            /// Will prepare this record for the next cycle by either reseting prev fields or replacing them with the current values.
            /// </summary>            
            /// <param name="reset">Wether to reset prev</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Next(bool expired)
            {
                if (!expired)
                {
                    directionPrev = direction;
                    signalNumPrev = signalNum;
                    sourceFlagsPrev = sourceFlags;
                    signalStrengthPrev = signalStrength;
                    sourceFlagsPrev = sourceFlags;
                }
                else
                {
                    directionPrev = Vector2.zero; ;
                    signalNumPrev = 0;
                    sourceFlagsPrev = 0;
                    signalStrengthPrev = 0;
                    sourceFlagsPrev = 0;
                }
            }
        }

        //
        // State fields.
        #region Fields
        
        /// <summary>
        /// The signature of the current operation.
        /// </summary>
        private short sig = 13;
        /// <summary>
        /// The flags of the current caster.
        /// </summary>
        private UInt64 currentSourceFlags;
        /// <summary>
        /// Ticks between cycles.
        /// </summary>
        private int cycle = 19;

        #endregion

        //
        // Read only fields.
        #region ReadonlyFields

        /// <summary>
        /// CellIndices of the parent map.
        /// </summary>
        protected readonly CellIndices cellIndices;
        /// <summary>
        /// The sight array with the size of the parent map.
        /// </summary>
        protected readonly SensoryRecord[] sightArray;
        /// <summary>
        /// Parent map.
        /// </summary>
        protected readonly Map map;
        /// <summary>
        /// Number of cells in parent map.
        /// </summary>
        protected readonly int mapCellNum;

        #endregion

        /// <summary>
        /// The current cycle of update.
        /// </summary>
        public int CycleNum
        {
            get => cycle;
        }

        public SensoryGrid(Map map)
        {
            cellIndices = map.cellIndices;
            mapCellNum = cellIndices.NumGridCells;
            sightArray = new SensoryRecord[map.cellIndices.NumGridCells];
            this.map = map;
            for (int i = 0; i < sightArray.Length; i++)
            {
                sightArray[i] = new SensoryRecord()
                {
                    sig = -1,
                    expireAt = -1,
                    direction = Vector3.zero
                };
            }
        }
        
        public virtual float this[IntVec3 cell]
        {
            get => this[cellIndices.CellToIndex(cell)];
        }
        
        public virtual float this[int index]
        {
            get
            {
                if (index >= 0 && index < mapCellNum)
                {
                    SensoryRecord record = sightArray[index];

                    if (record.expireAt - CycleNum > 0)
                        return Math.Max(record.signalNum, record.signalNumPrev);

                    if (record.expireAt - CycleNum == 0)
                        return record.signalNum;
                }
                return 0;
            }
        }

        public void Set(IntVec3 cell, float signal, Vector2 flow) => Set(cellIndices.CellToIndex(cell), 1, signal, flow);
        public void Set(int index, float signal, Vector2 flow) => Set(index, 1, signal, flow);

        public void Set(IntVec3 cell, int num, float signal, Vector2 flow) => Set(cellIndices.CellToIndex(cell), num, signal, flow);
        public virtual void Set(int index, int num, float signal, Vector2 flow)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SensoryRecord record = sightArray[index];
                if (record.sig != sig)
                {
                    IntVec3 cell = cellIndices.IndexToCell(index);
                    float t = record.expireAt - CycleNum;
                    if (t > 0)
                    {
                        record.signalNum += (short)num;                     
                        record.signalStrength += signal * num;
                        record.direction += flow * num;                        
                        record.sourceFlags |= currentSourceFlags;
                    }
                    else
                    {
                        if (t == 0)
                        {
                            record.expireAt = CycleNum + 1;
                            record.Next(expired: false);
                        }
                        else
                        {
                            record.expireAt = CycleNum + 1;
                            record.Next(expired: true);
                        }
                        record.signalNum = (short)num;
                        record.signalStrength = signal * num;
                        record.direction = flow * num;
                        record.sourceFlags = currentSourceFlags;
                    }
                    record.sig = sig;
                    sightArray[index] = record;
                }
            }
        }

        /// <summary>
        /// Prepare the grid for a new casting operation.
        /// </summary>        
        /// <param name="sourceFlags">caster's Flags</param>
        protected void Next(UInt64 sourceFlags)
        {
            sig++;           
            this.currentSourceFlags = sourceFlags;
        }

        public virtual void NextCycle()
        {
            sig++;
            cycle++;            
            this.currentSourceFlags = 0;
        }
        
        public string GetDebugInfoAt(IntVec3 cell) => GetDebugInfoAt(map.cellIndices.CellToIndex(cell));
        public virtual string GetDebugInfoAt(int index)
        {
            return null;
        }
    }    
}

