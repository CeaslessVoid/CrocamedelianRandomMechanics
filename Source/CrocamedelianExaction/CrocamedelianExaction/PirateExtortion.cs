using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static System.Collections.Specialized.BitVector32;
using Verse.Noise;

namespace CrocamedelianExaction
{
    public class IncidentWorker_CrEPiratePawnExtort : IncidentWorker
    {
        private const int TimeoutTicks = GenDate.TicksPerDay / 8;
        private Pawn victim;
        private Faction faction;

        public float chance_modifier = (float)Math.Round(Math.Exp(2 * ((1 / (1 + Mathf.Exp (-0.02f * CrE_GameComponent.CrE_Points) ) ) - 0.5f)) - 1,2);

        //   \left(e^{2\left(\frac{1}{1+e^{-0.02\cdot x}}-0.5\right)}-1\right)

        /// <summary>
        /// StorytellerUtilityPopulation.PopulationIntent is how much story teller wants to get you to 3 pawns. Negative means this event happens less when less pawns.
        /// </summary>

        public override float BaseChanceThisGame => CrE_GameComponent.Settings.CrE_PirateExtort_BaseChance - StorytellerUtilityPopulation.PopulationIntent + (chance_modifier * CrE_GameComponent.Settings.CrE_pointsMod);

        //public override float BaseChanceThisGame => 9999999;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms)
                && CrE_GameComponent.GetRandomPirateFaction(out faction)
                && TryFindVictim(out victim)
                && CrE_GameComponent.Settings.CrE_PirateExtort
                && PawnsFinder.AllMaps_FreeColonists.Count() > 1;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!CrE_GameComponent.GetRandomPirateFaction(out faction) || !TryFindVictim(out victim))
            {
                return false;
            }

            var text = "CrE_PiratePawn_Extort"
                .Translate(victim.LabelShort, faction.Name)
                .CapitalizeFirst();

            var ChoiceLetter_CrEDemandPawn =
                (ChoiceLetter_CrEDemandPawn)LetterMaker.MakeLetter(def.letterLabel, text, def.letterDef);

            ChoiceLetter_CrEDemandPawn.title =
                "CrE_PiratePawn_ExtortLabel".Translate().CapitalizeFirst();

            ChoiceLetter_CrEDemandPawn.radioMode = false;
            ChoiceLetter_CrEDemandPawn.faction = faction;
            ChoiceLetter_CrEDemandPawn.victim = victim;
            ChoiceLetter_CrEDemandPawn.StartTimeout(TimeoutTicks);

            Find.LetterStack.ReceiveLetter(ChoiceLetter_CrEDemandPawn);
            return true;
        }

        private bool TryFindVictim(out Pawn victim)
        {
            var potentialVictims = PawnsFinder
                .AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep
                .Where(CrE_GameComponent.isValidPawn);

            victim = potentialVictims
                .OrderByDescending(p => p.GetStatValue(StatDefOf.Beauty, true))
                .FirstOrDefault();

            return victim != null;
        }

    }


    public class ChoiceLetter_CrEDemandPawn : ChoiceLetter
    {
        public Pawn victim;
        public Faction faction;

        public override bool CanShowInLetterStack => base.CanShowInLetterStack
                                                    && PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists.Contains(victim);
        public override bool CanDismissWithRightClick
        {
            get
            {
                return false;
            }
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (ArchivedOnly)
                {
                    yield return Option_Close;
                }
                else
                {
                    var accept = new DiaOption("CrE_RansomDemand_Accept".Translate())
                    {
                        action = () =>
                        {
                            CrE_GameComponent.ChangeCrEPoints(Rand.Range(3, 5));

                            var caravan = victim.GetCaravan();
                            if (caravan != null)
                            {
                                CaravanInventoryUtility.MoveAllInventoryToSomeoneElse(victim, caravan.PawnsListForReading);
                                caravan.RemovePawn(victim);
                            }

                            foreach (Pawn colonist in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
                            {
                                if (colonist == victim)
                                {
                                    continue;
                                }

                                if (colonist.needs?.mood?.thoughts != null)
                                {
                                    var thoughtDef = ThoughtDef.Named("PirateForceWorkOther");
                                    colonist.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
                                }
                            }

                            DetermineAndDoOutcome(faction, victim);
                            Find.LetterStack.RemoveLetter(this);
                        }
                    };
                    var dialogueNodeAccept = new DiaNode("CrE_AcceptedPiratePawn_Extort"
                    .Translate(victim, faction).CapitalizeFirst().AdjustedFor(victim));
                    dialogueNodeAccept.options.Add(Option_Close);
                    accept.link = dialogueNodeAccept;

                    int bribeAmount = CalculateBribeAmount(victim);
                    var money = new DiaOption("CrE_RansomDemand_Money".Translate(bribeAmount))
                    {
                        action = () =>
                        {
                            CrE_GameComponent.ChangeCrEPoints(Rand.Range(1, 2));
                            TradeUtility.LaunchSilver(Find.CurrentMap, bribeAmount);
                            Find.LetterStack.RemoveLetter(this);
                        }
                    };
                    if (!TradeUtility.ColonyHasEnoughSilver(Find.CurrentMap, bribeAmount))
                    {
                        money.Disable("CrE_NotEnoughSilver".Translate(bribeAmount));
                    }

                    var reject = new DiaOption("CrE_RansomDemand_Reject".Translate())
                    {
                        action = () =>
                        {
                            CrE_GameComponent.ChangeCrEPoints(Rand.Range(-2, -3));
                            Find.LetterStack.RemoveLetter(this);

                            var incidentParms =
                            StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, Find.AnyPlayerHomeMap);
                            incidentParms.forced = true;
                            incidentParms.faction = faction;
                            incidentParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                            incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                            incidentParms.target = Find.AnyPlayerHomeMap;

                            IncidentDefOf.RaidEnemy.Worker.TryExecute(incidentParms);
                        }
                    };
                    var dialogueNodeReject = new DiaNode("CrE_RejectedPiratePawn_Extort"
                    .Translate(faction).CapitalizeFirst()
                    .AdjustedFor(victim));
                    dialogueNodeReject.options.Add(Option_Close);
                    reject.link = dialogueNodeReject;

                    yield return accept;
                    yield return money;
                    yield return reject;
                    yield return Option_Postpone;
                }
            }
        }

        private int CalculateBribeAmount(Pawn pawn)
        {
            float pawnWealth = pawn.MarketValue;
            float factor = CrE_GameComponent.Settings.CrE_ExtortPawnPrice;
            return Mathf.CeilToInt(pawnWealth * factor);
        }

        private static void DetermineAndDoOutcome(Faction faction, Pawn victim)
        {
            victim.SetFaction(null);
            int lostTime = PawnLostTime();

            CrE_GameComponent.PirateExtortPawn.Add(new PirateExtortPawnData(victim, lostTime, NextDate(), faction));

            int cooldownTicks = lostTime * 2;
            if (CrE_GameComponent.FactionRaidCooldowns.ContainsKey(faction))
            {
                CrE_GameComponent.FactionRaidCooldowns[faction] = cooldownTicks;
            }
            else
            {
                CrE_GameComponent.FactionRaidCooldowns.Add(faction, cooldownTicks);
            }

            Faction.OfPlayer.ideos?.RecalculateIdeosBasedOnPlayerPawns();
        }

        private static int PawnLostTime()
        {
            int minDays = CrE_GameComponent.Settings.CrE_minDaysBetweenEvents * 60000;
            int maxDays = CrE_GameComponent.Settings.CrE_maxDaysBetweenEvents * 60000;

            float pointsMod = Math.Max(1f, CrE_GameComponent.CrE_Points * CrE_GameComponent.Settings.CrE_pointsMod);

            return Find.TickManager.TicksGame + (int)(UnityEngine.Random.Range(minDays, maxDays) * pointsMod) + (GenDate.TicksPerDay / 2); //Add an extra 1/2 day for check
        }

        private static int NextDate()
        {
            return Find.TickManager.TicksGame + (GenDate.TicksPerDay / 2);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref victim, "victim");
            Scribe_References.Look(ref faction, "faction");
        }
    }

}
