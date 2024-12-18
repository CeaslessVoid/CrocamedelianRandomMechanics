using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using rjw;
using rjw.Modules.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.Noise;
using static System.Net.Mime.MediaTypeNames;


namespace CrocamedelianExaction
{
    [HarmonyPatch(typeof(Site), "ShouldRemoveMapNow")]
    internal class Patch_ShouldRemoveMapNow
    {
        private static void Postfix(ref Site __instance, ref bool alsoRemoveWorldObject, ref bool __result)
        {
            Map map = __instance.Map;
            Pawn prisoner = FindPrisonerInMap(map);

            if (prisoner != null)
            {
                if (AnyBlockingPawnsExceptPrisoner(__instance.Map))
                {
                    __result = false;
                    return;
                }

                Hediff hediff = prisoner.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken);

                if (hediff != null && hediff.Severity >= 1.0f)
                {
                    __result = true;
                }
                else
                {
                    __result = false;
                    alsoRemoveWorldObject = false;
                }
            }

        }

        private static bool AnyBlockingPawnsExceptPrisoner(Map map)
        {
            foreach (Pawn pawn in map.mapPawns.AllPawns)
            {
                if (!pawn.IsPrisoner && IsBlockingPawn(pawn))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBlockingPawn(Pawn pawn)
        {
            if (pawn.DeadOrDowned && !pawn.HasDeathRefusalOrResurrecting)
            {
                return false;
            }

            return (pawn.IsColonist || (pawn.IsColonyMutant && pawn.mutant.Def.canTravelInCaravan));
        }


        private static Pawn FindPrisonerInMap(Map map)
        {
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.IsPrisoner && pawn.Faction == Faction.OfPlayer)
                {
                    return pawn;
                }
            }
            return null;
        }
    }

    public static class MapGen
    {
        public static Map GetOrGenerateMap(int tile, IntVec3 size, WorldObjectDef suggestedMapParentDef)
        {
            Map map = Current.Game.FindMap(tile);
            MapParent mapParent = Find.WorldObjects.MapParentAt(tile);
            if (mapParent == null)
            {
                if (suggestedMapParentDef == null)
                {
                    Log.Error("Tried to get or generate map at " + tile + ", but there isn't any MapParent world object here and map parent def argument is null.");
                    return null;
                }

                mapParent = (MapParent)WorldObjectMaker.MakeWorldObject(suggestedMapParentDef);
                mapParent.Tile = tile;
                Find.WorldObjects.Add(mapParent);
            }

            map = MapGenerator.GenerateMap(size, mapParent, mapParent.MapGeneratorDef, mapParent.ExtraGenStepDefs);

            return map;
        }
    }



    // Taken from RJW. I mostly don't own this code

    // This normal only runs in passive mode. After map being open for some time they will swtich to active mode
    class JobGiver_AIRapePrisoner : ThinkNode_JobGiver
    {
        public static Pawn find_victim(Pawn pawn, Map m)
        {
            float avg_fuckability = 0f;
            var valid_targets = new Dictionary<Pawn, float>();
            Pawn chosentarget = null;

            IEnumerable<Pawn> targets = m.mapPawns.AllPawns.Where(x
                => x != pawn
                && x.Faction != pawn.Faction
                && x.IsHuman()
                && pawn.CanReserveAndReach(x, PathEndMode.Touch, Danger.Some, xxx.max_rapists_per_prisoner, 0)
                && !x.Position.IsForbidden(pawn)
                );

            foreach (Pawn target in targets)
            {
                if (!IsPrisonerOf(target, pawn.Faction))
                    return null;

                chosentarget = target;
            }

            //if (valid_targets.Any())
            //{
            //    avg_fuckability = valid_targets.Average(x => x.Value);

            //    var valid_targetsFiltered = valid_targets.Where(x => x.Value >= avg_fuckability);

            //    if (valid_targetsFiltered.Any())
            //        chosentarget = valid_targetsFiltered.RandomElement().Key;
            //}

            return chosentarget;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (RJWSettings.DebugRape) ModLog.Message(" JobGiver_AIRapePrisoner Crocamedelian's ::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) called ");

            if (pawn.health.hediffSet.HasHediff(HediffDef.Named("CrE_Hediff_RapeEnemyCD"))) return null;

            if (!RJWSettings.rape_enabled) return null;

            if (xxx.is_human(pawn))
            {
                if (pawn.ageTracker.Growth < 1f && !pawn.ageTracker.CurLifeStage.reproductive)
                {
                    return null;
                }
            }

            if (xxx.is_healthy(pawn))
            {
                Pawn prisoner = find_victim(pawn, pawn.Map);

                if (prisoner != null && pawn.CanReserve(prisoner))
                {

                    if (RJWSettings.DebugRape) ModLog.Message(" JobGiver_RandomRape::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) - found victim " + xxx.get_pawnname(prisoner));

                    pawn.health.AddHediff(HediffDef.Named("CrE_Hediff_RapeEnemyCD"), null, null, null);

                    return JobMaker.MakeJob(xxx.RapeRandom, prisoner);
                }
            }

            return null;
        }

        protected static bool IsPrisonerOf(Pawn pawn, Faction faction)
        {
            if (pawn?.guest == null) return false;
            return pawn.guest.IsPrisoner;
        }
    }

    // Others
    public class JobDriver_AdministerDrug : JobDriver
    {
        private const int Duration = 150;
        private Pawn Victim => TargetA.Thing as Pawn;
        private Thing Drug;

        public static bool jobInProgress = false;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Victim == null || Victim.Dead);

            yield return new Toil
            {
                initAction = () =>
                {
                    Drug = ThingMaker.MakeThing(ThingDef.Named("RJW_FertPill"));
                    if (Drug != null)
                    {
                        GenPlace.TryPlaceThing(Drug, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                        job.SetTarget(TargetIndex.B, Drug);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDestroyedOrNull(TargetIndex.B);

            yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: false);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil administerDrug = new Toil
            {
                initAction = () =>
                {
                    
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = Duration
            };
            administerDrug.WithProgressBarToilDelay(TargetIndex.A, false);
            yield return administerDrug;

            Toil endtoil = new Toil
            {
                initAction = () =>
                {
                    if (Victim != null)
                    {
                        Hediff hediff = HediffMaker.MakeHediff(HediffDef.Named("RJW_FertPill"), Victim);
                        Victim.health.AddHediff(hediff);
                    }

                    Hediff hediff2 = HediffMaker.MakeHediff(HediffDef.Named("RJW_FertPill"), pawn);
                    pawn.health.AddHediff(hediff2);

                    if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null)
                    {
                        pawn.carryTracker.CarriedThing.Destroy(DestroyMode.Vanish);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            endtoil.AddFinishAction(() => jobInProgress = false);

            yield return endtoil;

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref Drug, "Drug");
        }
    }

    public class ThinkNode_AdministerDrug : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (JobDriver_AdministerDrug.jobInProgress)
                return null;

            // Fuck it, cooldowns on everything
            if (pawn.health.hediffSet.HasHediff(HediffDef.Named("CrE_Hediff_DrugEnemyCD"))) 
                return null;

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;

            if (!pawn.IsHuman())
                return null;

            if (pawn.CurJobDef == CrE_DefOf.CrE_AdministerDrug)
                return null;

            Pawn victim = FindVictim(pawn.Map);
            if (victim == null)
                return null;
            
            if (!pawn.CanReserve(victim))
                return null;

            if (victim.CurJob?.def == CrE_DefOf.CrE_AdministerDrug)
                return null;

            pawn.health.AddHediff(HediffDef.Named("CrE_Hediff_DrugEnemyCD"), null, null, null);
            Job job = JobMaker.MakeJob(CrE_DefOf.CrE_AdministerDrug, victim);
            job.count = 1;
            return job;
        }

        private Pawn FindVictim(Map map)
        {
            return map.mapPawns.AllPawns.FirstOrDefault(p =>
                p.IsPrisoner &&
                !p.Dead &&
                !p.health.hediffSet.HasHediff(HediffDef.Named("RJW_FertPill")));
        }
    }

    public class JobDriver_FeedPrisoner : JobDriver
    {
        private const int FeedingDuration = 250;
        private Pawn Victim => TargetA.Thing as Pawn;
        private Thing Meal;

        public static bool jobInProgress = false;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Victim == null || Victim.Dead);

            yield return new Toil
            {
                initAction = () =>
                {
                    Meal = ThingMaker.MakeThing(ThingDef.Named("Kibble"));
                    if (Meal != null)
                    {
                        GenPlace.TryPlaceThing(Meal, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                        job.SetTarget(TargetIndex.B, Meal);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
                .FailOnDestroyedOrNull(TargetIndex.B);

            yield return Toils_Haul.StartCarryThing(TargetIndex.B);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil feedPrisoner = new Toil
            {
                initAction = () =>
                {
                    
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = FeedingDuration
            };

            feedPrisoner.WithProgressBarToilDelay(TargetIndex.A, false);

            yield return feedPrisoner;

            Toil endToil = new Toil
            {
                initAction = () =>
                {
                    if (Victim.needs.food != null)
                    {
                        //float nutritionAmount = Meal.GetStatValue(StatDefOf.Nutrition);
                        Victim.needs.food.CurLevel += 100;
                    }

                    // Might as well
                    pawn.needs.food.CurLevel += 100;

                    ThoughtDef ateKibbleThought = ThoughtDef.Named("AteKibble");
                    if (ateKibbleThought != null)
                    {
                        Victim.needs.mood.thoughts.memories.TryGainMemory(ateKibbleThought);
                    }

                    if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null)
                    {
                        pawn.carryTracker.CarriedThing.Destroy(DestroyMode.Vanish);
                    }


                    Hediff existingHediff = Victim.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken);
                    if (existingHediff != null)
                    {
                        float severityIncrease = Rand.Range(0.01f, 0.05f);
                        existingHediff.Severity += severityIncrease;
                    }
                    else
                    {
                        Hediff newHediff = HediffMaker.MakeHediff(xxx.feelingBroken, Victim);
                        newHediff.Severity = Rand.Range(0.01f, 0.05f);
                        Victim.health.AddHediff(newHediff);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            endToil.AddFinishAction(() => jobInProgress = false);

            yield return endToil;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref Meal, "Meal");
        }
    }

    public class ThinkNode_FeedPrisoner : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (JobDriver_FeedPrisoner.jobInProgress)
                return null;

            if (pawn.health.hediffSet.HasHediff(HediffDef.Named("CrE_Hediff_FeedEnemyCD"))) return null;

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;

            if (!pawn.IsHuman())
                return null;

            if (pawn.CurJobDef == CrE_DefOf.CrE_FeedEnemy)
                return null;

            Pawn victim = FindVictim(pawn.Map);
            if (victim == null)
                return null;

            if (victim.CurJob?.def == CrE_DefOf.CrE_FeedEnemy)
                return null;

            pawn.health.AddHediff(HediffDef.Named("CrE_Hediff_FeedEnemyCD"), null, null, null);
            Job job = JobMaker.MakeJob(CrE_DefOf.CrE_FeedEnemy, victim);
            job.count = 1;

            JobDriver_FeedPrisoner.jobInProgress = true;

            return job;
        }

        private Pawn FindVictim(Map map)
        {
            return map.mapPawns.AllPawns.FirstOrDefault(p =>
                p.IsPrisoner &&
                !p.Dead &&
                p.needs.food != null &&
                p.needs.food.CurLevelPercentage < 0.5f);
        }
    }


    public class JobDriver_AdministerYayo : JobDriver
    {
        private const int Duration = 150;
        private Pawn Victim => TargetA.Thing as Pawn;
        private Thing Drug;

        public static bool jobInProgress = false;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Victim == null || Victim.Dead);

            yield return new Toil
            {
                initAction = () =>
                {
                    Drug = ThingMaker.MakeThing(ThingDef.Named("Yayo"));
                    if (Drug != null)
                    {
                        GenPlace.TryPlaceThing(Drug, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                        job.SetTarget(TargetIndex.B, Drug);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDestroyedOrNull(TargetIndex.B);

            yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: false);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil administerDrug = new Toil
            {
                initAction = () =>
                {

                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = Duration
            };
            administerDrug.WithProgressBarToilDelay(TargetIndex.A, false);
            yield return administerDrug;

            Toil endtoil = new Toil
            {
                initAction = () =>
                {
                    if (Victim != null)
                    {

                        float overdoseIncrease = Rand.Range(0.18f, 0.35f);
                        Hediff overdoseHediff = Victim.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("DrugOverdose"));

                        if (overdoseHediff != null)
                        {
                            overdoseHediff.Severity += overdoseIncrease;
                        }
                        else
                        {
                            Hediff newOverdose = HediffMaker.MakeHediff(HediffDef.Named("DrugOverdose"), Victim);
                            newOverdose.Severity = overdoseIncrease;
                            Victim.health.AddHediff(newOverdose);
                        }

                        Hediff tolerance = Victim.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("PsychiteTolerance"));
                        if (tolerance != null)
                        {
                            tolerance.Severity += 0.1f;
                        }
                        else
                        {
                            Hediff newTolerance = HediffMaker.MakeHediff(HediffDef.Named("PsychiteTolerance"), Victim);
                            newTolerance.Severity = 0.1f;
                            Victim.health.AddHediff(newTolerance);
                        }

                        if (Rand.Chance(tolerance?.Severity > 0.5f ? 0.25f : 0.05f))
                        {
                            Hediff addiction = HediffMaker.MakeHediff(HediffDef.Named("PsychiteAddiction"), Victim);
                            Victim.health.AddHediff(addiction);

                            Find.LetterStack.ReceiveLetter(
                                "LetterLabelNewAddiction".Translate(Victim.Named("PAWN")),
                                "LetterNewAddiction".Translate(Victim.LabelNoCountColored, Victim.gender.GetPronoun(), Victim.gender.GetPossessive()),
                                LetterDefOf.NegativeEvent,
                                new LookTargets(Victim)
                            );

                        }

                        Hediff newHigh = HediffMaker.MakeHediff(HediffDef.Named("YayoHigh"), Victim);
                        newHigh.Severity = 0.1f;
                        Victim.health.AddHediff(newHigh);

                        Hediff existingHediff = Victim.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken);
                        if (existingHediff != null)
                        {
                            float severityIncrease = Rand.Range(0.01f, 0.05f);
                            existingHediff.Severity += severityIncrease;
                        }
                        else
                        {
                            Hediff newHediff = HediffMaker.MakeHediff(xxx.feelingBroken, Victim);
                            newHediff.Severity = Rand.Range(0.01f, 0.05f);
                            Victim.health.AddHediff(newHediff);
                        }
                    }

                    if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null)
                    {
                        pawn.carryTracker.CarriedThing.Destroy(DestroyMode.Vanish);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            endtoil.AddFinishAction(() => jobInProgress = false);

            yield return endtoil;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref Drug, "Drug");
        }
    }

    public class ThinkNode_AdministerYayo : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (JobDriver_AdministerYayo.jobInProgress)
                return null;

            // Cooldown check
            if (pawn.health.hediffSet.HasHediff(HediffDef.Named("CrE_Hediff_DrugEnemyCD")))
                return null;

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;

            if (!pawn.IsHuman())
                return null;

            if (pawn.CurJobDef == CrE_DefOf.CrE_AdministerYayo)
                return null;

            Pawn victim = FindVictim(pawn.Map);
            if (victim == null)
                return null;

            if (!pawn.CanReserve(victim))
                return null;

            if (victim.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.DrugOverdose)?.Severity > 0.5f)
                return null;

            pawn.health.AddHediff(HediffDef.Named("CrE_Hediff_DrugEnemyCD"), null, null, null);
            Job job = JobMaker.MakeJob(CrE_DefOf.CrE_AdministerYayo, victim);
            job.count = 1;
            return job;
        }

        private Pawn FindVictim(Map map)
        {
            return map.mapPawns.AllPawns.FirstOrDefault(p =>
                p.IsPrisoner &&
                !p.Dead &&
                (p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.DrugOverdose)?.Severity ?? 0) <= 0.5f);
        }
    }

    public class JobDriver_ApplyTattoo : JobDriver
    {
        private const int Duration = 1200;
        private Pawn Victim => TargetA.Thing as Pawn;

        public static bool jobInProgress = false;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Victim == null || Victim.Dead);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil applyTattooToil = new Toil
            {
                initAction = () =>
                {
                    StripPrisoner(Victim);

                    Victim.jobs.StopAll();
                    Victim.stances.stunner.StunFor(Duration, pawn);
                },
                tickAction = () =>
                {
                    Victim.rotationTracker.Face(pawn.DrawPos);
                    pawn.rotationTracker.Face(Victim.DrawPos);
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = Duration
            };

            applyTattooToil.WithProgressBarToilDelay(TargetIndex.A);
            yield return applyTattooToil;

            yield return new Toil
            {
                initAction = () =>
                {
                    CrE_GameComponent.RapeTattoo(Victim);
                    jobInProgress = false;

                    Hediff existingHediff = Victim.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken);
                    if (existingHediff != null)
                    {
                        float severityIncrease = Rand.Range(0.1f, 0.2f);
                        existingHediff.Severity += severityIncrease;
                    }
                    else
                    {
                        Hediff newHediff = HediffMaker.MakeHediff(xxx.feelingBroken, Victim);
                        newHediff.Severity = Rand.Range(0.1f, 0.2f);
                        Victim.health.AddHediff(newHediff);
                    }

                    Hediff cooldown = HediffMaker.MakeHediff(HediffDef.Named("CrE_Hediff_LastOneISwear"), Victim);
                    Victim.health.AddHediff(cooldown);

                    Find.LetterStack.ReceiveLetter(
                                "LetterLabelCrETattoo".Translate(Victim.Named("PAWN")),
                                "LetterCrETattoo".Translate(Victim.LabelNoCountColored, Victim.gender.GetPronoun(), Victim.gender.GetPossessive(), pawn.Faction.Name, Victim.GetName()),
                                LetterDefOf.NegativeEvent,
                                new LookTargets(Victim)
                            );
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private static void StripPrisoner(Pawn victim)
        {
            while (victim.apparel.WornApparel.Any())
            {
                Apparel apparel = victim.apparel.WornApparel.First();
                victim.apparel.TryDrop(apparel, out _, victim.PositionHeld, false);
            }
        }
    }

    public class ThinkNode_ApplyTattoo : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (JobDriver_ApplyTattoo.jobInProgress)
                return null;

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;

            if (pawn.CurJobDef == CrE_DefOf.CrE_ApplyTattoo)
                return null;

            Pawn victim = FindVictim(pawn.Map);
            if (victim == null)
                return null;

            if (!pawn.CanReserve(victim))
                return null;

            JobDriver_ApplyTattoo.jobInProgress = true;
            Job job = JobMaker.MakeJob(CrE_DefOf.CrE_ApplyTattoo, victim);
            job.count = 1;
            return job;
        }

        private Pawn FindVictim(Map map)
        {
            List<TattooDef> enabledTattoos = DefDatabase<TattooDef>.AllDefsListForReading
                                        .Where(t => CrE_GameComponent.Settings.EnabledTattoos.TryGetValue(t.defName, out bool isEnabled) && isEnabled)
                                        .ToList();

            return map.mapPawns.AllPawns.FirstOrDefault(p =>
                p.IsPrisoner &&
                !p.Dead &&
                p.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken)?.Severity >= 0.5f &&
                !p.health.hediffSet.HasHediff(HediffDef.Named("CrE_Hediff_LastOneISwear"))
            );
        }
    }



}
