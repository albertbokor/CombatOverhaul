using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CombatExtended.AI
{
    public class CompGasMask : ICompTactics
    {
        private const string GASMASK_TAG = "GasMask";

        private const int SMOKE_TICKS_OFFSET = 800;

        private int lastSmokeTick = -1;

        private bool maskEquiped = false;

        public override int Priority => 50;

        private int ticks = 0;

        public override void TickRarer()
        {
            base.TickRarer();
            if (ticks % 2 == 0)
                UpdateGasMask();
            ticks++;
        }

        public void UpdateGasMask()
        {
            if (selPawn.Faction.IsPlayerSafe())
                return;
            if (!selPawn.Spawned)
                return;
            if (selPawn.Downed || selPawn.apparel?.wornApparel == null)
                return;
            if (ticks % 4 == 0)
                CheckForMask();
            if (lastSmokeTick < GenTicks.TicksGame && maskEquiped)
                RemoveMask();
        }

        public void Notify_ShouldEquipGasMask()
        {
            if (lastSmokeTick < GenTicks.TicksGame
                && !selPawn.Faction.IsPlayerSafe()
                && !selPawn.Downed
                && selPawn.apparel?.wornApparel != null)
            {
                WearMask();
                lastSmokeTick = GenTicks.TicksGame + SMOKE_TICKS_OFFSET;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastSmokeTick, "lastSmokeTick", -1);
            Scribe_Values.Look(ref maskEquiped, "maskEquiped", false);
        }

        private void WearMask()
        {

            Apparel mask = null;
            foreach (Apparel apparel in CompInventory.container.Where(t => t is Apparel).Cast<Apparel>())
            {
                if (apparel.def.apparel?.tags?.Contains(GASMASK_TAG) ?? false)
                {
                    mask = apparel;
                    break;
                }
            }
            if (mask != null)
            {
                selPawn.inventory.innerContainer.Remove(mask);
                selPawn.apparel.Wear(mask);
            }
        }

        private void RemoveMask()
        {
            Apparel mask = null;
            foreach (Apparel apparel in selPawn.apparel.wornApparel)
            {
                if (apparel.def.apparel?.tags?.Contains(GASMASK_TAG) ?? false)
                {
                    mask = apparel;
                    break;
                }
            }
            if (mask != null)
            {
                selPawn.apparel.Remove(mask);
                selPawn.inventory.innerContainer.TryAddOrTransfer(mask);
            }
            maskEquiped = false;
        }

        private void CheckForMask()
        {
            maskEquiped = false;
            foreach (Apparel apparel in selPawn.apparel.wornApparel)
            {
                if (apparel.def.apparel?.tags?.Contains(GASMASK_TAG) ?? false)
                {
                    maskEquiped = true;
                    return;
                }
            }
        }
    }
}
