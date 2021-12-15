using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.Utilities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public class CompUrgentWeaponPickup : ICompTactics
    {
        private const int BULLETIMPACT_COOLDOWN = 800;
        private const int PRIMARY_OPTIMIZE_COOLDOWN = 1500;

        private int lastBulletImpact = -1;

        public bool BulletImpactedRecently
        {
            get
            {
                return GenTicks.TicksGame - lastBulletImpact < BULLETIMPACT_COOLDOWN;
            }
        }

        private int lastPrimaryOptimization = -1;
        public bool PrimaryOptimizatedRecently
        {
            get
            {
                return GenTicks.TicksGame - lastPrimaryOptimization < PRIMARY_OPTIMIZE_COOLDOWN;
            }
        }

        public override int Priority => 20;

        public CompUrgentWeaponPickup()
        {
        }

        public override void Notify_BulletImpactNearBy()
        {
            base.Notify_BulletImpactNearBy();
            if (!BulletImpactedRecently && !PrimaryOptimizatedRecently)
            {
                lastBulletImpact = GenTicks.TicksGame;
                CheckPrimaryEquipment();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastBulletImpact, "lastBulletImpact", -1);
            Scribe_Values.Look(ref lastPrimaryOptimization, "lastPrimaryOptimization", -1);
        }

        private void CheckPrimaryEquipment()
        {
            if (selPawn.Faction.IsPlayerSafe())
                return;
            if (selPawn.RaceProps.IsMechanoid)
                return;
            if (!selPawn.RaceProps.Humanlike)
                return;
            if (selPawn.equipment == null)
                return;
            if (selPawn.equipment.Primary != null)
                return;
            if (selPawn.Downed || selPawn.InMentalState)
                return;
            if (CompInventory?.SwitchToNextViableWeapon(false, true, false) ?? true)
                return;
            if (CompInventory.rangedWeaponList == null)
                return;
            if (selPawn.story != null && selPawn.WorkTagIsDisabled(WorkTags.Violent))
                return;
            foreach (ThingWithComps thing in CompInventory.rangedWeaponList)
            {
                CompAmmoUser compAmmo = thing.TryGetComp<CompAmmoUser>();
                if (compAmmo == null)
                    continue;
                if (compAmmo.TryPickupAmmo())
                {
                    lastPrimaryOptimization = GenTicks.TicksGame;

                    selPawn.equipment.equipment.TryAddOrTransfer(thing);
                    return;
                }
            }
            IEnumerable<AmmoThing> ammos = selPawn.Position.AmmoInRange(Map, 15).Where(t => t is AmmoThing) ?? new List<AmmoThing>();
            foreach (Thing thing in selPawn.Position.WeaponsInRange(Map, 15).OrderBy(t => t.Position.DistanceTo(selPawn.Position)))
            {
                // TODO need more tunning
                if (thing is ThingWithComps weapon)
                {
                    CompAmmoUser compAmmo = weapon.TryGetComp<CompAmmoUser>();
                    if (!selPawn.CanReach(thing, PathEndMode.InteractionCell, Danger.Unspecified, false, false))
                        continue;
                    if (!selPawn.CanReserve(weapon))
                        continue;
                    if (compAmmo == null)
                        continue;
                    IEnumerable<AmmoDef> supportedAmmo = compAmmo.Props?.ammoSet?.ammoTypes?.Select(a => a.ammo) ?? null;
                    if (supportedAmmo == null)
                        continue;

                    foreach (AmmoThing ammo in ammos)
                    {
                        if (!supportedAmmo.Contains(ammo.AmmoDef))
                            continue;
                        if (!selPawn.CanReach(ammo, PathEndMode.InteractionCell, Danger.Unspecified, false, false))
                            continue;
                        if (!selPawn.CanReserve(ammo))
                            continue;

                        if (CompInventory.CanFitInInventory(ammo, out int count))
                        {
                            lastPrimaryOptimization = GenTicks.TicksGame;

                            Job pickup = JobMaker.MakeJob(JobDefOf.TakeInventory, ammo);
                            pickup.count = count;
                            selPawn.jobs.StartJob(pickup, JobCondition.InterruptForced, resumeCurJobAfterwards: false);

                            Job equip = JobMaker.MakeJob(JobDefOf.Equip, weapon);
                            selPawn.jobs.jobQueue.EnqueueFirst(equip);
                            return;
                        }
                    }
                }
            }
            foreach (Thing thing in selPawn.Position.WeaponsInRange(Map, 15))
            {
                if (!selPawn.CanReach(thing, PathEndMode.InteractionCell, Danger.Unspecified, false, false))
                    continue;
                if (!selPawn.CanReserve(thing))
                    continue;
                if (!thing.def.IsRangedWeapon)
                {
                    lastPrimaryOptimization = GenTicks.TicksGame;

                    Job job = JobMaker.MakeJob(JobDefOf.Equip, thing);
                    selPawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true);
                    return;
                }
            }
        }
    }
}
