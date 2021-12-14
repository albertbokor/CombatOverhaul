using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class AvoidanceTracker : MapComponent
    {
        public class AvoidanceReader
        {
            public PartiableGrid danger;
            public PartiableGrid smoke;
            public PartiableGrid pathing;
            public PartiableGrid proximity;
            public PartiableGrid bullet;

            private readonly Map map;
            private readonly CellIndices indices;
            private readonly AvoidanceTracker tacker;

            public AvoidanceReader(AvoidanceTracker tracker)
            {
                this.tacker = tracker;
                this.map = tracker.map;
                this.indices = tracker.map.cellIndices;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetDanger(IntVec3 cell) => GetDanger(indices.CellToIndex(cell));
            public float GetDanger(int index) =>
                Mathf.Min(danger[index] + (AnySmoke(index) ? 3f : 0) + bullet[index] / 2f, 8f);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetPathing(IntVec3 cell) => GetPathing(indices.CellToIndex(cell));
            public float GetPathing(int index) =>
                Mathf.Min(pathing[index] + GetDanger(index) / 2f, 8f);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetProximity(IntVec3 cell) => GetProximity(indices.CellToIndex(cell));
            public float GetProximity(int index) =>
                proximity != null ? Mathf.Min(pathing[index] + (AnyBullets(index) ? 2f : 0f), 8f) : 0f;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AnyBullets(IntVec3 cell) => AnyBullets(indices.CellToIndex(cell));
            public bool AnyBullets(int index) =>
                bullet != null ? bullet[index] > 0f : false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AnySmoke(IntVec3 cell) => AnySmoke(indices.CellToIndex(cell));
            public bool AnySmoke(int index) =>
                smoke != null ? smoke[index] > 0 : false;
        }

        public PartiableManager danger;
        public PartiableManager smoke;
        public PartiableManager bullets;       
        public PartiableManager[] pathing = new PartiableManager[2];
        public PartiableManager[] proximity = new PartiableManager[2];        

        public AvoidanceTracker(Map map) : base(map)
        {
            danger = new PartiableManager(map);
            bullets = new PartiableManager(map);
            smoke = new PartiableManager(map);
            pathing[0] = new PartiableManager(map);
            proximity[0] = new PartiableManager(map);
            pathing[1] = new PartiableManager(map);            
            proximity[1] = new PartiableManager(map);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            // tick bullet grid
            bullets.Tick();
            // tick smoke grid
            smoke.Tick();
            // tick danger grid
            danger.Tick();
            // update path avoidence grid
            pathing[0].Tick();
            pathing[1].Tick();
            // update formation avoidence grid
            proximity[0].Tick();
            proximity[1].Tick();

            if (Controller.settings.DebugDrawAvoidance && GenTicks.TicksGame % 15 == 0)
            {                
                IntVec3 center = UI.MouseMapPosition().ToIntVec3();
                if (center.InBounds(map))
                {
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 64, true))
                    {
                        if (cell.InBounds(map))
                        {
                            var value = danger.grid[cell] + pathing[1].grid[cell] + proximity[1].grid[cell] + smoke.grid[cell] + bullets.grid[cell];
                            if (value > 0)
                                map.debugDrawer.FlashCell(cell, (float)value / 10f, $"{Math.Round(value, 1)} {Math.Round(pathing[1].grid[cell], 1)} {Math.Round(proximity[1].grid[cell], 1)}", 15);
                        }
                    }
                }
            }
        }

        public bool TryGetAvoidanceReader(Pawn pawn, out AvoidanceReader reader)
        {
            reader = null;
            if (pawn.Faction == null
                || (!pawn.RaceProps.Humanlike && !pawn.RaceProps.IsMechanoid)
                || map.ParentFaction == null)                            
                return false;            
            reader = new AvoidanceReader(this);
            reader.danger = danger.grid;
            reader.bullet = bullets.grid;
            if (!pawn.RaceProps.IsMechanoid)
                reader.smoke = smoke.grid;
            if (!pawn.Faction.HostileTo(map.ParentFaction))
            {
                reader.proximity = !pawn.RaceProps.IsMechanoid ? proximity[0].grid : null;
                reader.pathing = pathing[0].grid;
            }
            else
            {
                reader.proximity = !pawn.RaceProps.IsMechanoid ? proximity[1].grid : null;
                reader.pathing = pathing[1].grid;
            }            
            return true;
        }

        public void Notify_Bullet(IntVec3 cell)
        {
            if (cell.InBounds(map))
                bullets.Set(cell, 0.25f, 2);
        }

        public void Notify_BulletImpact(IntVec3 cell)
        {
            if (cell.InBounds(map))
                danger.Set(cell, 4, 3);
        }

        public void Notify_Smoke(IntVec3 cell)
        {
            if (cell.InBounds(map))
                smoke.Set(cell, 0.5f, 3);
        }        

        public void Notify_PathFound(Pawn pawn, PawnPath path)
        {
            if ((!pawn.RaceProps.Humanlike && !pawn.RaceProps.IsMechanoid)
                || pawn.Faction == null
                || map.ParentFaction == null)
                return;
            PartiableManager manager = !pawn.Faction.HostileTo(map.ParentFaction) ? pathing[0] : pathing[1];
            for (int i = 3; i < path.nodes.Count; i += 7)
                manager.Set(path.nodes[i], 3, 3);
            for (int i = 1; i < path.nodes.Count; i += 3)
                manager.Set(path.nodes[i], 2, 1);
        }

        public void Notify_CoverPositionSelected(Pawn pawn, IntVec3 cell)
        {
            if (!pawn.RaceProps.Humanlike
                || pawn.Faction == null
                || map.ParentFaction == null)
                return;
            PartiableManager manager = !pawn.Faction.HostileTo(map.ParentFaction) ? proximity[0] : proximity[1];
            manager.Set(cell, 8f, 2);
            manager.Set(cell, 2f, 4);
        }

        public void Notify_Injury(Pawn pawn, IntVec3 cell)
        {
        }

        public void Notify_Death(Pawn pawn, IntVec3 cell)
        {
        }
    }
}

