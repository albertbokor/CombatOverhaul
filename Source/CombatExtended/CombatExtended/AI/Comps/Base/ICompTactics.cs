using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public abstract class ICompTactics : IExposable
    {
        public Pawn selPawn;
        public CompTacticalManager manager;
        
        public abstract int Priority { get; }
        
        public virtual ThingWithComps CurrentWeapon => manager.CurrentWeapon;
        public virtual CompEquippable CurrentWeaponEq => manager.CurrentWeaponEq;
        public virtual CompAmmoUser CurrentWeaponCompAmmo => manager.CurrentWeaponCompAmmo;
        public CompSuppressable CompSuppressable => manager.CompSuppressable;
        public CompInventory CompInventory => manager.CompInventory;
        public SightTracker.SightReader MapSightReader => manager.MapSightReader;
        public TurretTracker MapTurretTracker => manager.MapTurretTracker;

        public Map Map => selPawn.Map;

        public ICompTactics()
        {
        }

        public virtual void Initialize(CompTacticalManager manager)
        {
            this.manager = manager;
            selPawn = manager.SelPawn;           
        }

        public virtual Job TryGiveTacticalJob()
        {
            return null;
        }

        public virtual void TickRarer()
        {
        }

        public virtual void TickShort()
        {
        }

        public virtual bool StartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            return true;
        }

        public virtual void OnStartCastFailed()
        {
        }

        public virtual void OnStartCastSuccess(Verb verb)
        {
        }

        public virtual void PostExposeData()
        {
        }

        public virtual void Notify_BulletImpactNearBy()
        {
        }        

        public void ExposeData()
        {
            Scribe_References.Look(ref selPawn, "pawnInt");
            this.PostExposeData();
        }

        public void Notify_StartCastChecksFailed(ICompTactics failedComp)
        {
            if (failedComp != this)
                OnStartCastFailed();
        }

        public void Notify_StartCastChecksSuccess(Verb verb)
        {
            OnStartCastSuccess(verb);
        }
    }
}
