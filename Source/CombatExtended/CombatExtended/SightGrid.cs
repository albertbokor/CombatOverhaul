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
    public class SightGrid : SensoryGrid
    {
        private float range;
        private IntVec3 center;

        public SightGrid(Map map) : base(map)
        {
        }

        public void Set(int index, int num, int dist) => Set(cellIndices.IndexToCell(index), num, dist);
        public void Set(IntVec3 cell, int num, int dist)
        {
            base.Set(cellIndices.CellToIndex(cell), num, (range - dist) / range, new Vector2(cell.x - center.x, cell.z - center.z));
        }

        public void Next(IntVec3 center, float range, UInt64 casterFlags)
        {
            base.Next(casterFlags);
            this.range = range;
            this.center = center;
        }

        public override void NextCycle()
        {
            base.NextCycle();
            this.range = 0;
            this.center = IntVec3.Invalid;
        }

        public float GetVisibility(int index) => GetVisibility(index, out _);
        public float GetVisibility(IntVec3 cell) => GetVisibility(cellIndices.CellToIndex(cell), out _);

        public float GetVisibility(IntVec3 cell, out int enemies) => GetVisibility(cellIndices.CellToIndex(cell), out enemies);
        public float GetVisibility(int index, out int enemies)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SensoryRecord record = sightArray[index];
                if (record.expireAt - CycleNum > 0)
                {
                    enemies = record.signalNumPrev;
                    return Mathf.Max((record.signalStrengthPrev + enemies) / 2f, (record.signalStrength + enemies) / 2f, 0f);
                }
                else if (record.expireAt - CycleNum == 0)
                {
                    enemies = record.signalNum;
                    return Mathf.Max((record.signalStrength + enemies) / 2f, 0f);
                }
            }
            enemies = 0;
            return 0f;
        }

        public Vector2 GetDirectionAt(IntVec3 cell) => GetDirectionAt(cellIndices.CellToIndex(cell));
        public Vector2 GetDirectionAt(int index)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SensoryRecord record = sightArray[index];
                if (record.expireAt - CycleNum > 0)
                {
                    if (record.signalNum > record.signalNumPrev)
                        return record.direction / (record.signalNum + 0.01f);
                    else
                        return record.directionPrev / (record.signalNumPrev + 0.01f);
                }
                if (record.expireAt - CycleNum == 0)
                    return record.direction / (record.signalNum + 0.01f);
            }
            return Vector2.zero;
        }

        public UInt64 GetFlagsAt(IntVec3 cell) => GetFlagsAt(cellIndices.CellToIndex(cell));
        public UInt64 GetFlagsAt(int index)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SensoryRecord record = sightArray[index];
                if (record.expireAt - CycleNum > 0)
                    return record.sourceFlagsPrev | record.sourceFlags;

                if (record.expireAt - CycleNum == 0)
                    return record.sourceFlags;
            }
            return 0;
        }

        public Vector2 GetDirectionAt(IntVec3 cell, out float enemies) => GetDirectionAt(cellIndices.CellToIndex(cell), out enemies);
        public Vector2 GetDirectionAt(int index, out float enemies)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SensoryRecord record = sightArray[index];
                if (record.expireAt - CycleNum > 0)
                {
                    if (record.signalNum > record.signalNumPrev)
                    {
                        enemies = record.signalNum;
                        return record.direction / (enemies + 0.01f);
                    }
                    else
                    {
                        enemies = record.signalNumPrev;
                        return record.directionPrev / (enemies + 0.01f);
                    }
                }
                else if (record.expireAt - CycleNum == 0)
                {
                    enemies = record.signalNum;
                    return record.direction / (enemies + 0.01f);
                }
            }
            enemies = 0;
            return Vector2.zero;
        }

        public bool HasCover(int index) => HasCover(cellIndices.IndexToCell(index));
        public bool HasCover(IntVec3 cell)
        {
            if (cell.InBounds(map))
            {
                SensoryRecord record = sightArray[cellIndices.CellToIndex(cell)];
                if (record.expireAt - CycleNum >= 0)
                {
                    Vector2 direction = record.direction.normalized * -1f;
                    IntVec3 endPos = cell + new Vector3(direction.x * 5, 0, direction.y * 5).ToIntVec3();
                    bool result = false;
                    GenSight.PointsOnLineOfSight(cell, endPos, (cell) =>
                    {
                        if (!result && cell.InBounds(map))
                        {
                            Thing cover = cell.GetCover(map);
                            if (cover != null && cover.def.Fillage == FillCategory.Partial && cover.def.category != ThingCategory.Plant)
                                result = true;
                        }
                    });
                    return result;
                }
            }
            return false;
        }

        public float GetCellSightCoverRating(int index) => GetCellSightCoverRatingInternel(cellIndices.IndexToCell(index), out _);
        public float GetCellSightCoverRating(IntVec3 cell) => GetCellSightCoverRatingInternel(cell, out _);

        public float GetCellSightCoverRating(int index, out bool hasCover) => GetCellSightCoverRatingInternel(cellIndices.IndexToCell(index), out hasCover);
        public float GetCellSightCoverRating(IntVec3 cell, out bool hasCover) => GetCellSightCoverRatingInternel(cell, out hasCover);

        private float GetCellSightCoverRatingInternel(IntVec3 cell, out bool hasCover)
        {
            if (!cell.InBounds(map))
            {
                hasCover = false;
                return 0f;
            }
            hasCover = HasCover(cell);
            if (hasCover)
                return GetVisibility(cell) * 0.5f;
            else
                return GetVisibility(cell);
        }

        private static StringBuilder _builder = new StringBuilder();
        public override string GetDebugInfoAt(int index)
        {
            if (index >= 0 && index < mapCellNum)
            {
                SensoryRecord record = sightArray[index];
                _builder.Clear();
                _builder.AppendFormat("<color=grey>{0}</color> {1}\n", "Partially expired ", record.expireAt - CycleNum == 0);
                _builder.AppendFormat("<color=grey>{0}</color> {1}", "Expired           ", record.expireAt - CycleNum < 0);
                _builder.AppendLine();
                _builder.AppendFormat("<color=orange>{0}</color> {1}\n" +
                    "<color=grey>cur</color>  {2}\t" +
                    "<color=grey>prev</color> {3}", "Enemies", this[index], record.signalNum, record.signalNumPrev);
                _builder.AppendLine();
                _builder.AppendFormat("<color=orange>{0}</color> {1}\n " +
                    "<color=grey>cur</color>  {2}\t" +
                    "<color=grey>prev</color> {3}", "Visibility", GetVisibility(index), Math.Round(record.signalStrength, 2), Math.Round(record.signalStrength, 2));
                _builder.AppendLine();
                _builder.AppendFormat("<color=orange>{0}</color> {1}\n" +
                    "<color=grey>cur</color>  {2} " +
                    "<color=grey>prev</color> {3}", "Direction", GetDirectionAt(index), record.direction, record.directionPrev);
                _builder.AppendLine();
                _builder.AppendFormat("<color=orange>{0}</color> {1}\n" +
                    "<color=grey>cur</color>\n{2}\n" +
                    "<color=grey>prev</color>\n{3}", "Flags", Convert.ToString((long)GetFlagsAt(index), 2).Replace("1", "<color=green>1</color>"), Convert.ToString((long)record.sourceFlags, 2).Replace("1", "<color=green>1</color>"), Convert.ToString((long)record.sourceFlagsPrev, 2).Replace("1", "<color=green>1</color>"));
                return _builder.ToString();
            }
            return "<color=red>Out of bounds</color>";
        }
    }
}

