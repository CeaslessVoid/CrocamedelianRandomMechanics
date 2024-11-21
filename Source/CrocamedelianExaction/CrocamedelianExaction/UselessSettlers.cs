//using HarmonyLib;
//using LudeonTK;
//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//using Verse;
//using Verse.AI;

//namespace CrocamedelianExaction
//{
//    public class IncidentWorker_CrESquatterArrival : IncidentWorker
//    {
//        protected override bool CanFireNowSub(IncidentParms parms)
//        {
//            return base.CanFireNowSub(parms) && !this.IsScenarioBlocked()
//                                             && CrE_GameComponent.Settings.CrE_Squatters;
//        }
//        protected override bool TryExecuteWorker(IncidentParms parms)
//        {
//            Map map = (Map)parms.target;
//            Pawn guest = GenerateGuestPawn();

//            IntVec3 edgeCell = CellFinder.RandomEdgeCell(map);
//            IntVec3 destinationCell = DropCellFinder.TradeDropSpot(map);
//            GenSpawn.Spawn(guest, edgeCell, map, WipeMode.Vanish);
//            Job walkInJob = JobMaker.MakeJob(JobDefOf.Goto, destinationCell);
//            guest.jobs.StartJob(walkInJob, JobCondition.InterruptForced);

//            Find.LetterStack.ReceiveLetter(
//                "Squatter Arrival",
//                $"{guest.Name} has arrived at your colony. They won’t work but will enjoy your amenities and may leave after some time.",
//                LetterDefOf.PositiveEvent,
//                new LookTargets(guest)
//            );

//            Log.Message($"{guest.Name} has arrived as a temporary settler.");

//            return true;
//        }

//        private Pawn GenerateGuestPawn()
//        {
//            PawnGenerationRequest request = new PawnGenerationRequest(
//                PawnKindDefOf.Colonist,
//                Faction.OfPlayer,
//                PawnGenerationContext.NonPlayer,
//                -1,
//                forceGenerateNewPawn: true,
//                inhabitant: false,
//                relationWithExtraPawnChanceFactor: 0,
//                fixedGender: Gender.Male,
//                forbidAnyTitle: true
//            );

//            Pawn guest = PawnGenerator.GeneratePawn(request);

//            HediffDef leaveHediffDef = DefDatabase<HediffDef>.GetNamed("CrE_Leave");
//            guest.health.AddHediff(leaveHediffDef);

//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Construction);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Hauling);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.PlantCutting);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Doctor);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Childcare);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Cleaning);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Research);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Mining);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Childcare);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Handling);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Hunting);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Crafting);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.DarkStudy);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Growing);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Smithing);
//            guest.GetDisabledWorkTypes().Add(WorkTypeDefOf.Warden);

//            return guest;
//        }
//    }

//    public class HediffComp_LeaveCountdown : HediffComp
//    {
//        public HediffCompPropreties_LeaveCountdown Props
//        {
//            get
//            {
//                return (HediffCompPropreties_LeaveCountdown)this.props;
//            }
//        }


//        public override void CompPostTick(ref float severityAdjustment)
//        {
//            Pawn pawn = base.Pawn;


//            if (pawn.IsHashIntervalTick(60000))
//            {
//                if (Rand.Value < CrE_GameComponent.Settings.CrE_SquatterLeaveChance)
//                {
//                    Messages.Message($"{pawn.Name} has decided to leave your colony.", pawn, MessageTypeDefOf.NeutralEvent);


//                    pawn.SetFaction(null);
//                }
//            }

//            base.CompPostTick(ref severityAdjustment);
//        }
//    }

//    public class HediffCompPropreties_LeaveCountdown : HediffCompProperties
//    {
//        public HediffCompPropreties_LeaveCountdown()
//        {
//            this.compClass = typeof(HediffComp_LeaveCountdown);
//        }
//    }

//}
