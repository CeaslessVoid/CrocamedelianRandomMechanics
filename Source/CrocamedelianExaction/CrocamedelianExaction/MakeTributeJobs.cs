//using RimWorld;
//using rjw;
//using rjw.Modules.Shared.Extensions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Verse;
//using Verse.AI;
//using Verse.AI.Group;

//namespace CrocamedelianExaction
//{
//    class JobGiver_AIMakeTribute : ThinkNode_JobGiver
//    {
//        public static Pawn victim;
//        protected override Job TryGiveJob(Pawn pawn)
//        {
//            Job job = JobMaker.MakeJob(CrE_DefOf.CrE_ApplyTattooTribute, victim);
//            job.targetA = victim;
//            return job;

//        }
//    }

//    public class JobDriver_ApplyTattooTribute : JobDriver
//    {
//        private const int Duration = 1200;
//        private Pawn Victim => TargetA.Thing as Pawn;

//        public static bool jobInProgress = false;

//        public override bool TryMakePreToilReservations(bool errorOnFailed)
//        {
//            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
//        }

//        protected override IEnumerable<Toil> MakeNewToils()
//        {
//            this.FailOn(() => Victim == null || Victim.Dead);

//            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

//            Toil applyTattooToil = new Toil
//            {
//                initAction = () =>
//                {
//                    JobDriver_ApplyTattoo.StripPrisoner(Victim);

//                    Victim.jobs.StopAll();
//                    Victim.stances.stunner.StunFor(Duration, pawn);
//                },
//                tickAction = () =>
//                {
//                    Victim.rotationTracker.Face(pawn.DrawPos);
//                    pawn.rotationTracker.Face(Victim.DrawPos);
//                },
//                defaultCompleteMode = ToilCompleteMode.Delay,
//                defaultDuration = Duration
//            };

//            applyTattooToil.WithProgressBarToilDelay(TargetIndex.A);
//            yield return applyTattooToil;

//            yield return new Toil
//            {
//                initAction = () =>
//                {
//                    CrE_GameComponent.RapeTattoo(Victim);
//                    jobInProgress = false;

//                    Hediff existingHediff = Victim.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken);
//                    if (existingHediff != null)
//                    {
//                        float severityIncrease = Rand.Range(0.1f, 0.2f);
//                        existingHediff.Severity += severityIncrease;
//                    }
//                    else
//                    {
//                        Hediff newHediff = HediffMaker.MakeHediff(xxx.feelingBroken, Victim);
//                        newHediff.Severity = Rand.Range(0.1f, 0.2f);
//                        Victim.health.AddHediff(newHediff);
//                    }

//                    Victim.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("BodyTattooByCapture"));

//                    Hediff cooldown = HediffMaker.MakeHediff(HediffDef.Named("CrE_Hediff_LastOneISwear"), Victim);
//                    Victim.health.AddHediff(cooldown);

//                    Find.LetterStack.ReceiveLetter(
//                                "LetterLabelCrETattoo".Translate(Victim.Named("PAWN")),
//                                "LetterCrETattoo".Translate(Victim.LabelNoCountColored, Victim.gender.GetPronoun(), Victim.gender.GetPossessive(), pawn.Faction.Name, Victim.GetName()),
//                                LetterDefOf.NegativeEvent,
//                                new LookTargets(Victim)
//                    );
//                },
//                defaultCompleteMode = ToilCompleteMode.Instant
//            };
//        }
//    }
//}