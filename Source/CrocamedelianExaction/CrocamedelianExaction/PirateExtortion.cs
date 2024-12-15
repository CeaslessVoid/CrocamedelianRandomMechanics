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
        private const int TimeoutTicks = GenDate.TicksPerDay;
        private Pawn victim;
        private Faction faction;

        public float chance_modifier = (float)Math.Round(Math.Exp(2 * ((1 / (1 + Mathf.Exp(-0.02f * CrE_GameComponent.CrE_Points))) - 0.5f)) - 1,2);
        public override float BaseChanceThisGame => Math.Max(0.01f,
            Mathf.Clamp(base.BaseChanceThisGame - StorytellerUtilityPopulation.PopulationIntent + chance_modifier, 0.0f, 1.0f));

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms)
                && TryFindFaction(out faction)
                && TryFindVictim(out victim)
                && CrE_GameComponent.Settings.CrE_PirateExtort
                && PawnsFinder.AllMaps_FreeColonists.Count() > 1;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TryFindFaction(out faction) || !TryFindVictim(out victim))
            {
                return false;
            }

            var text = "CrE_PiratePawn_Extort"
                .Translate(faction.Name, victim.LabelShort)
                .CapitalizeFirst();

            var ChoiceLetter_CrE_Demand_Pawn =
                (ChoiceLetter_CrE_Demand_Pawn)LetterMaker.MakeLetter(def.letterLabel, text, def.letterDef);

            ChoiceLetter_CrE_Demand_Pawn.title =
                "CrE_PiratePawn_ExtortLabel".Translate(victim.LabelShort).CapitalizeFirst();

            ChoiceLetter_CrE_Demand_Pawn.radioMode = false;
            ChoiceLetter_CrE_Demand_Pawn.faction = faction;
            ChoiceLetter_CrE_Demand_Pawn.victim = victim;
            ChoiceLetter_CrE_Demand_Pawn.StartTimeout(TimeoutTicks);

            Find.LetterStack.ReceiveLetter(ChoiceLetter_CrE_Demand_Pawn);
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


        private static bool TryFindFaction(out Faction faction)
        {
            return (from f in Find.FactionManager.AllFactions
                    where f.HostileTo(Faction.OfPlayer)
                          && !f.def.hidden
                          && !f.defeated
                          && f.def.humanlikeFaction 
                    select f).TryRandomElement(out faction);
        }
    }


    public class ChoiceLetter_CrE_Demand_Pawn : ChoiceLetter
    {
        public Pawn victim;
        public Faction faction;

        public override bool CanShowInLetterStack => base.CanShowInLetterStack
                                                    && PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists.Contains(victim);

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
                            CrE_GameComponent.CurrentCrEPawn = victim;

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

                            CrE_GameComponent.DoPirateTakePawn();
                            DetermineAndDoOutcome(faction, victim);
                            CrE_GameComponent.CrE_PirateFaction = faction;

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
            victim.SetFaction(faction);
            Faction.OfPlayer.ideos?.RecalculateIdeosBasedOnPlayerPawns();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref victim, "victim");
            Scribe_References.Look(ref faction, "faction");
        }
    }

}
