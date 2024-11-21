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
        private Pawn pirateLeader;
        // Make sure not all your colinists are taken
        public float chance_modifier = (float)Math.Round(Math.Exp(2 * ((1 / (1 + Mathf.Exp(-0.02f * CrE_GameComponent.CrE_Points))) - 0.5f)) - 1,2);
        public override float BaseChanceThisGame => Math.Max(0.01f,
            Mathf.Clamp(base.BaseChanceThisGame - StorytellerUtilityPopulation.PopulationIntent + chance_modifier, 0.0f, 1.0f));

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && TryFindPirateLeader(out pirateLeader)
                                             && TryFindVictim(out victim)
                                             && !CrE_GameComponent.HasPawnOut
                                             && CrE_GameComponent.Settings.CrE_PirateExtort;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TryFindPirateLeader(out pirateLeader) || !TryFindVictim(out victim))
            {
                return false;
            }

            var text = "CrE_PiratePawn_Extort"
                .Translate(pirateLeader.LabelShort, victim.LabelShort, pirateLeader.Faction.Name)
                .AdjustedFor(pirateLeader);

            var ChoiceLetter_CrE_Demand_Pawn =
                (ChoiceLetter_CrE_Demand_Pawn)LetterMaker.MakeLetter(def.letterLabel, text, def.letterDef);

            ChoiceLetter_CrE_Demand_Pawn.title =
                "CrE_PiratePawn_ExtortLabel".Translate(victim.LabelShort).CapitalizeFirst();

            ChoiceLetter_CrE_Demand_Pawn.radioMode = false;
            ChoiceLetter_CrE_Demand_Pawn.pirateLeader = pirateLeader;
            ChoiceLetter_CrE_Demand_Pawn.victim = victim;
            ChoiceLetter_CrE_Demand_Pawn.StartTimeout(TimeoutTicks);

            Find.LetterStack.ReceiveLetter(ChoiceLetter_CrE_Demand_Pawn);
            return true;
        }

        private bool TryFindVictim(out Pawn victim)
        {
            return (from potentialPartners in PawnsFinder
                    .AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners_NoCryptosleep
                    where CrE_GameComponent.isValidPawn(potentialPartners)
                    select potentialPartners).TryRandomElement(out victim);
        }

        private static bool TryFindPirateLeader(out Pawn pirateLeader)
        {
            return (from x in Find.WorldPawns.AllPawnsAlive
                    where x.Faction != null && !x.Faction.def.hidden && x.Faction.def.permanentEnemy && !x.Faction.IsPlayer
                          && !x.Faction.defeated
                          && !x.Spawned && x.RaceProps.Humanlike
                          && !SettlementUtility.IsPlayerAttackingAnySettlementOf(x.Faction)
                           select x)
                          .TryRandomElement(out pirateLeader);
        }
    }


    public class ChoiceLetter_CrE_Demand_Pawn : ChoiceLetter
    {
        public Pawn victim;
        public Pawn pirateLeader;

        public override bool CanShowInLetterStack => base.CanShowInLetterStack 
                                                    && PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists.Contains(value: victim);

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

                            CrE_GameComponent.ChangeCrEPoints(Rand.Range(3,5));
                            CrE_GameComponent.HasPawnOut = true;
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

                            DetermineAndDoOutcome(pirateLeader, victim);
                            CrE_GameComponent.CrE_Pirate = pirateLeader;


                            Find.LetterStack.RemoveLetter(this);
                        }
                    };
                    var dialogueNodeAccept = new DiaNode("CrE_AcceptedPiratePawn_Extort"
                        .Translate(victim, pirateLeader.Faction).CapitalizeFirst().AdjustedFor(pirateLeader));
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
                    var dialogueNodeMoney = new DiaNode("CrE_MoneyPiratePawn_Extort"
                        .Translate(bribeAmount, pirateLeader.Faction).CapitalizeFirst().AdjustedFor(pirateLeader));
                    dialogueNodeMoney.options.Add(Option_Close);
                    money.link = dialogueNodeMoney;


                    var reject = new DiaOption("CrE_RansomDemand_Reject".Translate())
                    {
                        action = () =>
                        {
                            CrE_GameComponent.ChangeCrEPoints(Rand.Range(-2, -3));
                            Find.LetterStack.RemoveLetter(this);

                            var incidentParms =
                            StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, Find.AnyPlayerHomeMap);
                            incidentParms.forced = true;
                            incidentParms.faction = pirateLeader.Faction;
                            incidentParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                            incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                            incidentParms.target = Find.AnyPlayerHomeMap;

                            IncidentDefOf.RaidEnemy.Worker.TryExecute(incidentParms);

                        }
                    };
                    var dialogueNodeReject = new DiaNode("CrE_RejectedPiratePawn_Extort"
                        .Translate(pirateLeader.LabelCap, pirateLeader.Faction).CapitalizeFirst()
                        .AdjustedFor(pirateLeader));
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
            float factor = 0.75f;
            return Mathf.CeilToInt(pawnWealth * factor);
        }
        private static void DetermineAndDoOutcome(Pawn pirate, Pawn victim)
        {

            victim.SetFaction(!pirate.HostileTo(Faction.OfPlayer)
                ? pirate.Faction
                : null);

            //victim.SetFaction(pirate.Faction);

            Faction.OfPlayer.ideos?.RecalculateIdeosBasedOnPlayerPawns();

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref victim, "victim");
            Scribe_References.Look(ref pirateLeader, "pirateLeader");
        }
    }
}
