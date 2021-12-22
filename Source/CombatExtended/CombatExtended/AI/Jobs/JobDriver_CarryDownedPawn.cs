using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
	public class JobDriver_CarryDownedPawn : JobDriver
	{
        private Pawn targetPawn;
        /// <summary>
        /// The pawn to carry.
        /// </summary>
        public Pawn Takee
        {
            get => targetPawn ?? (targetPawn = TargetThingA as Pawn);
        }
        /// <summary>
        /// The spot to carry the pawn to.
        /// </summary>
        public IntVec3 TargetPosition
        {
            get => TargetB.Cell;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {            
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnAggroMentalState(TargetIndex.A);
            Toil goToTakee = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.B)                    
                    .FailOn(() => !Takee.Downed)
                    .FailOn(() => !pawn.CanReach(Takee, PathEndMode.OnCell, Danger.Deadly))                    
                    .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            Toil startCarryingTakee = Toils_Haul.StartCarryThing(TargetIndex.A);
            Toil goToThing = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);
            yield return Toils_Jump.JumpIf(goToThing, () => pawn.IsCarryingPawn(Takee));
            yield return goToTakee;
            yield return startCarryingTakee;
            yield return goToThing;
            yield return Toils_Haul.PlaceCarriedThingInCellFacing(TargetIndex.B);            
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(targetPawn, job, 1, -1, null, errorOnFailed);
        }
    }
}

