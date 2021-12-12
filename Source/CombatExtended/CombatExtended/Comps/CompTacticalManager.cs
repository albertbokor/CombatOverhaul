using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using UnityEngine;
using CombatExtended.AI;
using MonoMod.Utils;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine.UIElements;

namespace CombatExtended
{
    public class CompTacticalManager : ThingComp
    {
        private Thing lastEnemyTarget;        

        private Pawn _pawn = null;
        public Pawn SelPawn
        {
            get
            {
                return _pawn ?? (_pawn = parent as Pawn);
            }
        }
        
        private List<ICompTactics> _tacticalComps = new List<ICompTactics>();
        public List<ICompTactics> TacticalComps
        {
            get
            {
                if ((_tacticalComps?.Count ?? 0) == 0)
                    ValidateComps();
                return _tacticalComps;
            }
        }

        private CompSuppressable _compSuppressable = null;
        public virtual CompSuppressable CompSuppressable
        {
            get
            {
                if (_compSuppressable == null)
                    _compSuppressable = SelPawn.TryGetComp<CompSuppressable>();
                return _compSuppressable;
            }
        }
        
        private CombatReservationManager _reservationManager = null;
        public CombatReservationManager MapCombatReservationManager
        {
            get
            {
                if (!parent.Spawned)
                    return _reservationManager = null;
                if (_reservationManager == null || _reservationManager.map != parent.Map)
                    return _reservationManager = SelPawn.Map.GetComponent<CombatReservationManager>();
                return _reservationManager;
            }
        }

        public virtual ThingWithComps CurrentWeapon
        {
            get
            {
                return SelPawn.equipment.Primary;
            }
        }

        private ThingWithComps _currentWeapon = null;
        private CompEquippable _compEquippable = null;
        public virtual CompEquippable CurrentWeaponEq
        {
            get
            {
                if (_currentWeapon != CurrentWeapon)
                {
                    _currentWeapon = CurrentWeapon;
                    if (_currentWeapon != null)
                        _compEquippable = _currentWeapon.GetComp<CompEquippable>();
                }
                return _compEquippable;
            }
        }       

        private CompInventory _compInventory = null;
        public virtual CompInventory CompInventory
        {
            get
            {
                if (_compInventory == null) _compInventory = SelPawn.TryGetComp<CompInventory>();
                return _compInventory;
            }
        }

        private CompAmmoUser _AmmoUser_CompAmmoUser = null;
        private ThingWithComps _AmmoUser_ThingWithComps = null;
        public virtual CompAmmoUser CurrentWeaponCompAmmo
        {
            get
            {
                if (_AmmoUser_ThingWithComps == CurrentWeapon)
                    return _AmmoUser_CompAmmoUser;

                _AmmoUser_ThingWithComps = CurrentWeapon;

                if (_AmmoUser_ThingWithComps == null)
                    return _AmmoUser_CompAmmoUser = null;

                return _AmmoUser_CompAmmoUser = _AmmoUser_ThingWithComps.TryGetComp<CompAmmoUser>();
            }
        }

        private Map _sightReaderMap = null;
        private Faction _sightGridFaction = null;
        private SightTracker.SightReader _sightReader = null;
        public SightTracker.SightReader MapSightReader
        {
            get
            {
                if (!SelPawn.Spawned || SelPawn.Faction == null)
                {
                    _sightReaderMap = null;
                    _sightReader = null;
                    return null;
                }
                if (_sightReaderMap != SelPawn.Map || _sightGridFaction != SelPawn.Faction)
                {
                    _sightGridFaction = SelPawn.Faction;
                    _sightReaderMap = SelPawn.Map;
                    SelPawn.GetSightReader(out _sightReader);
                }
                return _sightReader;
            }
        }

        private Map _turretTrackerMap = null;
        private Faction _sturretTrackerFaction = null;
        private TurretTracker _turretTracker = null;
        public TurretTracker MapTurretTracker
        {
            get
            {
                if (!SelPawn.Spawned || SelPawn.Faction == null || SelPawn.Faction == SelPawn.Map.ParentFaction)
                {
                    _turretTracker = null;
                    _turretTrackerMap = null;
                    return null;
                }
                if (_turretTrackerMap != SelPawn.Map || _sturretTrackerFaction != SelPawn.Faction)
                {
                    _sturretTrackerFaction = SelPawn.Faction;
                    _turretTrackerMap = SelPawn.Map;
                    _turretTracker = _turretTrackerMap.GetComponent<TurretTracker>();
                }
                return _turretTracker;
            }
        }

        public Verb CurrentPrimaryVerb
        {
            get
            {
                Verb verb;
                CompEquippable equippable = CurrentWeaponEq;
                if (equippable != null && (verb = equippable.PrimaryVerb) != null && verb.Available())
                    return verb;
                if (SelPawn.verbTracker != null && (verb = SelPawn.verbTracker?.PrimaryVerb ?? null) != null && verb.Available())
                    return verb;
                if ((verb = SelPawn.CurrentEffectiveVerb)?.Available() ?? false)
                    return verb;
                return null;
            }
        }

        public bool DraftedColonist
        {
            get
            {
                return (SelPawn.Faction?.IsPlayer ?? false) && SelPawn.Drafted;
            }
        }

        private readonly TargetIndex[] _targetIndices = new TargetIndex[]
        {
            TargetIndex.A,
            TargetIndex.B,
            TargetIndex.C,
        };

        public override void CompTick()
        {            
            base.CompTick();
            if (parent.IsHashIntervalTick(20)) TickShort();
            if (parent.IsHashIntervalTick(5) && parent.Spawned)
            {
                Thing curTarget = SelPawn.mindState?.enemyTarget ?? null;
                if (curTarget != lastEnemyTarget)
                {
                    if (curTarget != null)                    
                        MapCombatReservationManager.Reserve(SelPawn, curTarget);                    
                    lastEnemyTarget = curTarget;
                }
            }            
        }

        private int _counter = 0;

        public override void CompTickRare()
        {
            base.CompTickRare();
            TryGiveTacticalJobs();
            if (_counter++ % 2 == 0) TickRarer();
        }       

        public bool TryStartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if (CompSuppressable == null || SelPawn.MentalState != null || CompSuppressable.IsHunkering)
                return true;

            bool AllChecksPassed(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg, out ICompTactics failedComp)
            {
                foreach (ICompTactics comp in TacticalComps)
                {
                    if (!comp.StartCastChecks(verb, castTarg, destTarg))
                    {
                        failedComp = comp;
                        return false;
                    }
                }
                failedComp = null;
                return true;
            }

            ICompTactics failedComp = null;

            if (!CompSuppressable.IsHunkering && (SelPawn.jobs.curDriver is IJobDriver_Tactical || AllChecksPassed(verb, castTarg, destTarg, out failedComp)))
            {
                foreach (ICompTactics comp in TacticalComps)
                    comp.Notify_StartCastChecksSuccess(verb);
                return true;
            }
            else
            {
                foreach (ICompTactics comp in TacticalComps)
                    comp.Notify_StartCastChecksFailed(failedComp);
                return false;
            }
        }

        public void Notify_BulletImpactNearby()
        {
            foreach (ICompTactics comp in TacticalComps)
            {
                try
                {
                    comp.Notify_BulletImpactNearBy();
                }
                catch (Exception er)
                {
                    Log.Error($"CE: Error running Notify_BulletImpactNearBy {comp.GetType()} with error {er}");
                }
            }
        }

        public T GetTacticalComp<T>() where T : ICompTactics
        {
            return (T)TacticalComps.FirstOrFallback(c => c is T, null);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            try
            {                
                Scribe_Collections.Look(ref _tacticalComps, "tacticalComps", LookMode.Deep);
                this.ValidateComps();
            }
            catch (Exception er)
            {
                Log.Error($"CE: Error scribing {parent} {er}");
                this._tacticalComps.Clear();
                this.ValidateComps();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if(SelPawn.mindState != null)
            {
                lastEnemyTarget = SelPawn.mindState.enemyTarget;
                if (lastEnemyTarget != null)
                    MapCombatReservationManager.Reserve(SelPawn, lastEnemyTarget);
            }
        }

        private void TickRarer()
        {
            List<ICompTactics> comps = TacticalComps;
            for (int i = 0; i < comps.Count; i++)
            {
                try
                {
                    comps[i].TickRarer();
                }
                catch (Exception er)
                {
                    Log.Error($"CE: Error ticking comp {comps[i].GetType()} with error {er}");
                }
            }
        }

        private void TickShort()
        {            
            List<ICompTactics> comps = TacticalComps;
            for(int i =0;i < comps.Count; i++)
            {
                try
                {
                    comps[i].TickShort();
                }
                catch (Exception er)
                {
                    Log.Error($"CE: Error ticking short comp {comps[i].GetType()} with error {er}");
                }
            }
            if (parent.Spawned)
            {
                Job job;
                Pawn pawn = SelPawn;
                Thing curEnemyTarget = null;
                if ((pawn.mindState == null || pawn.mindState.enemyTarget == null) && (job = pawn.jobs.curJob) != null)
                {
                    if (job.def == JobDefOf.AttackMelee)
                    {
                        curEnemyTarget = job.targetA.Thing;
                    }
                    else if (job.def == JobDefOf.AttackStatic)
                    {
                        curEnemyTarget = job.targetA.Thing;
                    }
                    if (curEnemyTarget != lastEnemyTarget)
                    {
                        lastEnemyTarget = curEnemyTarget;
                        if (curEnemyTarget != null)
                            MapCombatReservationManager.Reserve(SelPawn, curEnemyTarget);
                    }
                }
            }            
        } 

        private void TryGiveTacticalJobs()
        {
            if (CompSuppressable == null || CompSuppressable.IsHunkering || !SelPawn.Spawned || SelPawn.Downed)
            {
                return;
            }
            List<ICompTactics> comps = TacticalComps;
            for (int i = 0; i < comps.Count; i++)
            {                
                Job job = comps[i].TryGiveTacticalJob();
                if (job != null)
                {
                    SelPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                    return;
                }          
            }           
        }

        private void ValidateComps()
        {
            if (_tacticalComps == null)
                _tacticalComps = new List<ICompTactics>();
            foreach (Type type in typeof(ICompTactics).AllSubclassesNonAbstract())
            {
                ICompTactics comp;
                if ((comp = _tacticalComps.FirstOrFallback(t => t.GetType() == type)) == null)
                    _tacticalComps.Add(comp = (ICompTactics)Activator.CreateInstance(type, new object[0]));
                comp.Initialize(this);
            }
            _tacticalComps.SortBy(t => -1f * t.Priority);
        }
    }
}
