using RimWorld.QuestGen;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static System.Collections.Specialized.BitVector32;
using System.Data;
using Verse.Grammar;
using System.Collections;
using Verse.AI.Group;
using RimWorld.Planet;
using Verse.AI;
using UnityEngine;
using Verse.Sound;
using rjw;
using System.Net.NetworkInformation;

namespace CrocamedelianExaction
{

    public class QuestNode_Root_CrEPirateTributary : QuestNode
    {
        private bool TryGetTributaryTarget(Slate slate, out Pawn pawn, out Faction bestowingFaction)
        {
            // Checks should of been done beforehand

            slate.TryGet<Faction>("bestowingFaction", out bestowingFaction, false);
            slate.TryGet<Pawn>("titleHolder", out pawn, false);

            return true;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;

            Faction faction;
            Pawn pawn;

            if (!TryGetTributaryTarget(QuestGen.slate, out pawn, out faction))
            {
                Util.Error("Failed to find a valid target for the Pirate Tributary quest.");
                return;
            }

            RoyalTitleDef titleAwardedWhenUpdating = pawn.royalty.GetTitleAwardedWhenUpdating(faction, pawn.royalty.GetFavor(faction)); ; // Title Can be deleted

            // Signas
            string text = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("Bestowing");
            string text2 = QuestGenUtility.QuestTagSignal(text, "CeremonyExpired");
            string inSignal = QuestGenUtility.QuestTagSignal(text, "CeremonyFailed");
            string inSignal2 = QuestGenUtility.QuestTagSignal(text, "CeremonyDone");
            string inSignal3 = QuestGenUtility.QuestTagSignal(text, "BeingAttacked");
            string inSignal4 = QuestGenUtility.QuestTagSignal(text, "Fleeing");
            string inSignal5 = QuestGenUtility.QuestTagSignal(text, "TitleAwardedWhenUpdatingChanged");
            Thing thing = QuestGen_Shuttle.GenerateShuttle(faction, null, null, false, false, false, -1, false, false, false, false, null, null, -1, null, false, true, false, false, false);

            Pawn pawn2 = quest.GetPawn(new QuestGen_Pawns.GetPawnParms
            {
                mustBeOfKind = PawnKindDefOf.Empire_Royal_Bestower,
                mustBeWorldPawn = true,
                ifWorldPawnThenMustBeFree = true,

                canGeneratePawn = true,
                mustBeOfFaction = faction,
                redressPawn = true
            });

            QuestUtility.AddQuestTag(ref thing.questTags, text);
            QuestUtility.AddQuestTag(ref pawn.questTags, text);

            // Skip Psy

            List<Pawn> list = new List<Pawn>();
            list.Add(pawn2);
			slate.Set<List<Pawn>>("shuttleContents", list, false);
			slate.Set<Thing>("shuttle", thing, false);
			slate.Set<Pawn>("target", pawn, false);
			slate.Set<Pawn>("bestower", pawn2, false);
			slate.Set<Faction>("bestowingFaction", faction, false);

            List<Pawn> list2 = new List<Pawn>();
            for (int k = 0; k < 6; k++)
            {
                Pawn item = quest.GeneratePawn(faction.RandomPawnKind(), faction, true, null, 1f, true, null, 0f, 1f, false, false, DevelopmentalStage.Adult, false);
                list.Add(item);
                list2.Add(item);
            }

            quest.EnsureNotDowned(list, null);
            slate.Set<List<Pawn>>("defenders", list2, false);
            thing.TryGetComp<CompShuttle>().requiredPawns = list;

            TransportShip transportShip = quest.GenerateTransportShip(TransportShipDefOf.Ship_Shuttle, list, thing, null).transportShip;
            quest.AddShipJob_Arrive(transportShip, null, pawn, null, ShipJobStartMode.Instant, Faction.OfEmpire, null);
            quest.AddShipJob(transportShip, ShipJobDefOf.Unload, ShipJobStartMode.Queue, null);
            quest.AddShipJob_WaitForever(transportShip, true, false, list.Cast<Thing>().ToList<Thing>(), null).sendAwayIfAnyDespawnedDownedOrDead = new List<Thing>
            {
                pawn2
            };

            QuestUtility.AddQuestTag(ref transportShip.questTags, text);
            quest.FactionGoodwillChange(faction, -5, QuestGenUtility.HardcodedSignalWithQuestID("defenders.Killed"), true, true, true, HistoryEventDefOf.QuestPawnLost, QuestPart.SignalListenMode.OngoingOnly, false);

            QuestPart_MakeTributeCeremony questPart_BestowingCeremony = new QuestPart_MakeTributeCeremony();
            questPart_BestowingCeremony.inSignal = QuestGen.slate.Get<string>("inSignal", null, false);
            questPart_BestowingCeremony.pawns.Add(pawn2);
            questPart_BestowingCeremony.mapOfPawn = pawn;
            questPart_BestowingCeremony.faction = pawn2.Faction;
            questPart_BestowingCeremony.bestower = pawn2;
            questPart_BestowingCeremony.target = pawn;
            questPart_BestowingCeremony.shuttle = thing;
            questPart_BestowingCeremony.questTag = text;
            quest.AddPart(questPart_BestowingCeremony);

            QuestPart_EscortPawn questPart_EscortPawn = new QuestPart_EscortPawn();
            questPart_EscortPawn.inSignal = QuestGen.slate.Get<string>("inSignal", null, false);
            questPart_EscortPawn.escortee = pawn2;
            questPart_EscortPawn.pawns.AddRange(list2);
            questPart_EscortPawn.mapOfPawn = pawn;
            questPart_EscortPawn.faction = pawn2.Faction;
            questPart_EscortPawn.shuttle = thing;
            questPart_EscortPawn.questTag = text;
            questPart_EscortPawn.leavingDangerMessage = "MessageBestowingDanger".Translate();
            quest.AddPart(questPart_EscortPawn);

            string inSignal6 = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.Killed");
            quest.FactionGoodwillChange(faction, 0, inSignal6, true, true, true, HistoryEventDefOf.ShuttleDestroyed, QuestPart.SignalListenMode.OngoingOnly, true);
            quest.End(QuestEndOutcome.Fail, 0, null, inSignal6, QuestPart.SignalListenMode.OngoingOnly, true, false);

            QuestPart_RequirementsToAcceptPawnOnColonyMap questPart_RequirementsToAcceptPawnOnColonyMap = new QuestPart_RequirementsToAcceptPawnOnColonyMap();
            questPart_RequirementsToAcceptPawnOnColonyMap.pawn = pawn;
            quest.AddPart(questPart_RequirementsToAcceptPawnOnColonyMap);

            QuestPart_RequirementsToAcceptNoDanger questPart_RequirementsToAcceptNoDanger = new QuestPart_RequirementsToAcceptNoDanger();
            questPart_RequirementsToAcceptNoDanger.mapPawn = pawn;
            questPart_RequirementsToAcceptNoDanger.dangerTo = faction;
            quest.AddPart(questPart_RequirementsToAcceptNoDanger);

            string inSignal7 = QuestGenUtility.HardcodedSignalWithQuestID("shuttleContents.Recruited");
            string inSignal8 = QuestGenUtility.HardcodedSignalWithQuestID("bestowingFaction.BecameHostileToPlayer");

            quest.Signal(inSignal7, delegate
            {
                quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true, false);
            }, null, QuestPart.SignalListenMode.OngoingOnly);
            quest.Bestowing_TargetChangedTitle(pawn, pawn2, titleAwardedWhenUpdating, inSignal5);

            Quest quest2 = quest;
            LetterDef negativeEvent = LetterDefOf.NegativeEvent;

            string inSignal9 = text2;
            string chosenPawnSignal = null;
            Faction relatedFaction = null;
            MapParent useColonistsOnMap = null;
            bool useColonistsFromCaravanArg = false;
            QuestPart.SignalListenMode signalListenMode = QuestPart.SignalListenMode.OngoingOnly;

            IEnumerable<object> lookTargets = null;
            bool filterDeadPawnsFromLookTargets = false;
            string label = "LetterLabelBestowingCeremonyExpired".Translate();

            quest2.Letter(negativeEvent, inSignal9, chosenPawnSignal, relatedFaction, useColonistsOnMap, useColonistsFromCaravanArg, signalListenMode, lookTargets, filterDeadPawnsFromLookTargets, "LetterTextBestowingCeremonyExpired".Translate(pawn.Named("TARGET")), null, label, null, null);

            quest.End(QuestEndOutcome.Fail, 0, null, QuestGenUtility.HardcodedSignalWithQuestID("target.Killed"), QuestPart.SignalListenMode.OngoingOrNotYetAccepted, true, false);
            quest.End(QuestEndOutcome.Fail, 0, null, QuestGenUtility.HardcodedSignalWithQuestID("bestower.Killed"), QuestPart.SignalListenMode.OngoingOrNotYetAccepted, true, false);
            quest.End(QuestEndOutcome.Fail, 0, null, text2, QuestPart.SignalListenMode.OngoingOnly, false, false);
            quest.End(QuestEndOutcome.Fail, 0, null, inSignal8, QuestPart.SignalListenMode.OngoingOrNotYetAccepted, true, false);
            quest.End(QuestEndOutcome.Fail, 0, null, inSignal, QuestPart.SignalListenMode.OngoingOnly, true, false);
            quest.End(QuestEndOutcome.Fail, 0, null, inSignal3, QuestPart.SignalListenMode.OngoingOnly, true, false);
            quest.End(QuestEndOutcome.Fail, 0, null, inSignal4, QuestPart.SignalListenMode.OngoingOnly, true, false);
            quest.End(QuestEndOutcome.Success, 0, null, inSignal2, QuestPart.SignalListenMode.OngoingOnly, false, false);

            QuestPart_Choice questPart_Choice = quest.RewardChoice(null, null);
            QuestPart_Choice.Choice choice = new QuestPart_Choice.Choice();
            choice.rewards.Add(new Reward_BestowingCeremony
            {
                targetPawnName = pawn.NameShortColored.Resolve(),
                titleName = titleAwardedWhenUpdating.GetLabelCapFor(pawn),
                awardingFaction = faction,
                givePsylink = (titleAwardedWhenUpdating.maxPsylinkLevel > pawn.GetPsylinkLevel()),
                royalTitle = titleAwardedWhenUpdating
            });
            questPart_Choice.choices.Add(choice);

            List<Verse.Grammar.Rule> list1 = new List<Verse.Grammar.Rule>();
            list1.Add(new Rule_String("faction_name", faction.Name));
            QuestGen.AddQuestDescriptionRules(list1);

        }

        protected override bool TestRunInt(Slate slate)
        {
            Faction pirateFaction;
            Pawn pawn;

            return this.TryGetTributaryTarget(slate, out pawn, out pirateFaction);
        }

        public const string QuestTag = "Bestowing";
    }

    public class QuestPart_MakeTributeCeremony : QuestPart_MakeLord
    {
        public override bool QuestPartReserves(Pawn p)
        {
            return p == this.bestower || p == this.target;
        }


        public static bool TryGetCeremonySpot(Pawn pawn, Faction leaderFaction, out LocalTargetInfo spot, out IntVec3 absoluteSpot)
        {
            IntVec3 intVec;
            if (pawn.Map != null && pawn.Map.IsPlayerHome && (RCellFinder.TryFindGatheringSpot(pawn, GatheringDefOf.Party, true, out intVec) || RCellFinder.TryFindRandomSpotJustOutsideColony(pawn.Position, pawn.Map, out intVec)))
            {
                spot = (absoluteSpot = intVec);
                return true;
            }

            spot = LocalTargetInfo.Invalid;
            absoluteSpot = IntVec3.Invalid;
            return false;
        }

        protected override Lord MakeLord()
        {
            LocalTargetInfo targetSpot;
            IntVec3 spotCell;
            if (!QuestPart_BestowingCeremony.TryGetCeremonySpot(this.target, this.bestower.Faction, out targetSpot, out spotCell))
            {
                Log.Error("Cannot find ceremony spot for bestowing ceremony!");
                return null;
            }
            Lord lord = LordMaker.MakeNewLord(this.faction, new LordJob_MakeTributeCeremony(this.bestower, this.target, targetSpot, spotCell, this.shuttle, this.questTag + ".QuestEnded"), base.Map, null);
            QuestUtility.AddQuestTag(ref lord.questTags, this.questTag);
            return lord;
        }

        public override void Cleanup()
        {
            Find.SignalManager.SendSignal(new Signal(this.questTag + ".QuestEnded", this.quest.Named("SUBJECT")));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Pawn>(ref this.bestower, "bestower", false);
            Scribe_References.Look<Pawn>(ref this.target, "target", false);
            Scribe_References.Look<Thing>(ref this.shuttle, "shuttle", false);
            Scribe_Values.Look<string>(ref this.questTag, "questTag", null, false);
        }

        public Pawn bestower;

        public Pawn target;

        public Thing shuttle;

        public string questTag;
    }

    public class LordJob_MakeTributeCeremony : LordJob_Ritual
    {
        public const int ExpirationTicks = 30000;

        public static readonly string MemoCeremonyStarted = "CeremonyStarted";

        private const string MemoCeremonyFinished = "CeremonyFinished";

        public const int WaitTimeTicks = 600;

        public Pawn bestower;

        public Pawn target;

        public LocalTargetInfo targetSpot;

        public IntVec3 spotCell;

        public Thing shuttle;

        public string questEndedSignal;

        public List<Pawn> colonistParticipants = new List<Pawn>();

        public bool ceremonyStarted;

        private LordToil_TributeCeremony_Perform ceremonyToil;

        private LordToil exitToil;

        private RitualOutcomeEffectWorker_MakeTribute outcome;

        private const float HeatstrokeHypothermiaMinSeverityForLeaving = 0.35f;

        private const int HeatstrokeHypothermiaGoodwillOffset = -50;

        private const float GasExposireSeverityForLeaving = 0.9f;

        private Texture2D icon;

        private Dictionary<Pawn, int> totalPresenceTmp = new Dictionary<Pawn, int>();

        public override bool AlwaysShowWeapon => true;

        public override IntVec3 Spot => targetSpot.Cell;

        public override string RitualLabel => "BestowingCeremonyLabel".Translate().CapitalizeFirst();

        public override bool AllowStartNewGatherings => lord.CurLordToil != ceremonyToil;

        public Texture2D Icon
        {
            get
            {
                if (icon == null)
                {
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Rituals/BestowCeremony");
                }

                return icon;
            }
        }

        public override IEnumerable<Pawn> PawnsToCountTowardsPresence => lord.ownedPawns.Where((Pawn p) => p != bestower && p != target && p.IsColonist);

        public LordJob_MakeTributeCeremony()
        {
        }

        public LordJob_MakeTributeCeremony(Pawn bestower, Pawn target, LocalTargetInfo targetSpot, IntVec3 spotCell, Thing shuttle = null, string questEndedSignal = null)
        {
            this.bestower = bestower;
            this.target = target;
            this.targetSpot = targetSpot;
            this.spotCell = spotCell;
            this.shuttle = shuttle;
            this.questEndedSignal = questEndedSignal;
        }

        public override AcceptanceReport AllowsDrafting(Pawn pawn)
        {
            if (lord.CurLordToil == ceremonyToil)
            {
                return new AcceptanceReport("ParticipatingInRitual".Translate(pawn, RitualLabel));
            }

            return true;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            CompShuttle compShuttle = shuttle.TryGetComp<CompShuttle>();

            outcome = (RitualOutcomeEffectWorker_MakeTribute)CrE_DefOf.TributeCeremony.GetInstance();
            LordToil_Wait lordToil_Wait = new LordToil_Wait();
            stateGraph.AddToil(lordToil_Wait);
            LordToil_Wait lordToil_Wait2 = new LordToil_Wait();
            stateGraph.AddToil(lordToil_Wait2);
            LordToil_Wait lordToil_Wait3 = new LordToil_Wait();
            stateGraph.AddToil(lordToil_Wait3);
            LordToil_Wait lordToil_Wait4 = new LordToil_Wait();
            stateGraph.AddToil(lordToil_Wait4);

            LordToil_BestowingCeremony_MoveInPlace lordToil_BestowingCeremony_MoveInPlace = new LordToil_BestowingCeremony_MoveInPlace(spotCell, target);
            stateGraph.AddToil(lordToil_BestowingCeremony_MoveInPlace);
            LordToil_TributeCeremony_Wait lordToil_BestowingCeremony_Wait = new LordToil_TributeCeremony_Wait(target, bestower);
            stateGraph.AddToil(lordToil_BestowingCeremony_Wait);

            ceremonyToil = new LordToil_TributeCeremony_Perform(target, bestower);
            stateGraph.AddToil(ceremonyToil);

            exitToil = ((shuttle == null) ? ((LordToil)new LordToil_ExitMap(LocomotionUrgency.Jog)) : ((LordToil)new LordToil_EnterShuttleOrLeave(shuttle, LocomotionUrgency.Jog, canDig: true, interruptCurrentJob: true)));
            stateGraph.AddToil(exitToil);
            TransitionAction_Custom action = new TransitionAction_Custom((Action)delegate
            {
                lord.RemovePawns(colonistParticipants);
            });

            Transition transition = new Transition(lordToil_Wait, lordToil_Wait2);
            transition.AddTrigger(new Trigger_Custom((TriggerSignal signal) => signal.type == TriggerSignalType.Tick && bestower.Spawned));
            stateGraph.AddTransition(transition);
            Transition transition2 = new Transition(lordToil_Wait2, lordToil_BestowingCeremony_MoveInPlace);
            transition2.AddTrigger(new Trigger_TicksPassed(600));
            stateGraph.AddTransition(transition2);
            Transition transition3 = new Transition(lordToil_BestowingCeremony_MoveInPlace, lordToil_BestowingCeremony_Wait);
            transition3.AddTrigger(new Trigger_Custom((TriggerSignal signal) => signal.type == TriggerSignalType.Tick && bestower.Position == spotCell));
            stateGraph.AddTransition(transition3);
            Transition transition4 = new Transition(lordToil_BestowingCeremony_Wait, ceremonyToil);
            transition4.AddTrigger(new Trigger_Memo(MemoCeremonyStarted));
            transition4.postActions.Add(new TransitionAction_Custom((Action)delegate
            {
                ceremonyStarted = true;
            }));
            stateGraph.AddTransition(transition4);
            Transition transition5 = new Transition(ceremonyToil, lordToil_Wait4);
            transition5.AddTrigger(new Trigger_Custom((TriggerSignal s) => s.type == TriggerSignalType.Tick && bestower.InMentalState));
            transition5.AddPreAction(action);
            transition5.postActions.Add(new TransitionAction_Custom((Action)delegate
            {
                ceremonyStarted = false;
                lord.RemovePawn(target);
            }));
            transition5.AddPreAction(new TransitionAction_Custom((Action)delegate
            {
                Messages.Message("MessageBestowingInterrupted".Translate(), bestower, MessageTypeDefOf.NegativeEvent);
            }));
            stateGraph.AddTransition(transition5);
            Transition transition6 = new Transition(lordToil_Wait4, lordToil_BestowingCeremony_Wait);
            transition6.AddTrigger(new Trigger_Custom((TriggerSignal s) => s.type == TriggerSignalType.Tick && !bestower.InMentalState));
            stateGraph.AddTransition(transition6);
            Transition transition7 = new Transition(lordToil_BestowingCeremony_Wait, exitToil);
            transition7.AddTrigger(new Trigger_TicksPassed(30000));
            transition7.AddPreAction(action);
            transition7.AddPostAction(new TransitionAction_Custom((Action)delegate
            {
                QuestUtility.SendQuestTargetSignals(lord.questTags, "CeremonyExpired", lord.Named("SUBJECT"));
            }));
            stateGraph.AddTransition(transition7);
            Transition transition8 = new Transition(ceremonyToil, exitToil);
            transition8.AddTrigger(new Trigger_Signal(questEndedSignal));
            transition8.AddPreAction(action);
            transition8.AddPreAction(new TransitionAction_Custom((Action)delegate
            {
                Messages.Message("MessageBestowingInterrupted".Translate(), bestower, MessageTypeDefOf.NegativeEvent);
            }));
            stateGraph.AddTransition(transition8);
            Transition transition9 = new Transition(ceremonyToil, lordToil_Wait3);
            transition9.AddTrigger(new Trigger_Memo("CeremonyFinished"));
            transition9.AddPostAction(new TransitionAction_Custom((Action)delegate
            {
                QuestUtility.SendQuestTargetSignals(lord.questTags, "CeremonyDone", lord.Named("SUBJECT"));
            }));
            stateGraph.AddTransition(transition9);
            Transition transition10 = new Transition(lordToil_Wait3, exitToil);
            transition10.AddPreAction(action);
            transition10.AddTrigger(new Trigger_TicksPassed(600));
            stateGraph.AddTransition(transition10);
            Transition transition11 = new Transition(lordToil_BestowingCeremony_MoveInPlace, exitToil);
            transition11.AddSource(lordToil_BestowingCeremony_Wait);
            transition11.AddTrigger(new Trigger_BecamePlayerEnemy());
            transition11.AddPreAction(action);
            stateGraph.AddTransition(transition11);
            Transition transition12 = new Transition(lordToil_BestowingCeremony_MoveInPlace, exitToil);
            transition12.AddSource(lordToil_BestowingCeremony_Wait);
            transition12.AddTrigger(new Trigger_Custom((TriggerSignal signal) => signal.type == TriggerSignalType.Tick && bestower.Spawned && !bestower.CanReach(spotCell, PathEndMode.OnCell, Danger.Deadly)));
            transition12.AddPreAction(action);
            transition12.AddPostAction(new TransitionAction_Custom((Action)delegate
            {
                Messages.Message("MessageBestowingSpotUnreachable".Translate(), bestower, MessageTypeDefOf.NegativeEvent);
                QuestUtility.SendQuestTargetSignals(lord.questTags, "CeremonyFailed", lord.Named("SUBJECT"));
            }));
            stateGraph.AddTransition(transition12);
            if (!questEndedSignal.NullOrEmpty())
            {
                Transition transition13 = new Transition(lordToil_BestowingCeremony_MoveInPlace, exitToil);
                transition13.AddSource(lordToil_BestowingCeremony_Wait);
                transition13.AddSource(lordToil_Wait);
                transition13.AddSource(lordToil_Wait2);
                transition13.AddTrigger(new Trigger_Signal(questEndedSignal));
                transition13.AddPreAction(action);
                stateGraph.AddTransition(transition13);
            }

            Transition transition14 = new Transition(lordToil_BestowingCeremony_MoveInPlace, exitToil);
            transition14.AddSource(lordToil_BestowingCeremony_Wait);
            transition14.AddTrigger(new Trigger_Custom(delegate (TriggerSignal signal)
            {
                if (signal.type == TriggerSignalType.Tick && !bestower.Dead)
                {
                    Hediff firstHediffOfDef3 = bestower.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia);
                    if (firstHediffOfDef3 != null && firstHediffOfDef3.Severity >= 0.35f)
                    {
                        return true;
                    }

                    Hediff firstHediffOfDef4 = bestower.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
                    if (firstHediffOfDef4 != null && firstHediffOfDef4.Severity >= 0.35f)
                    {
                        return true;
                    }
                }

                return false;
            }));
            transition14.AddPreAction(action);
            transition14.AddPostAction(new TransitionAction_Custom((Action)delegate
            {
                compShuttle?.SetPawnToLeaveBehind((Pawn p) => p != bestower);
                bestower.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -50);
                Messages.Message("MessageBestowingDangerTemperature".Translate(), bestower, MessageTypeDefOf.NegativeEvent);
                QuestUtility.SendQuestTargetSignals(lord.questTags, "BeingAttacked", lord.Named("SUBJECT"));
            }));
            stateGraph.AddTransition(transition14);
            Transition transition15 = new Transition(lordToil_BestowingCeremony_MoveInPlace, exitToil);
            transition15.AddSource(lordToil_BestowingCeremony_Wait);
            transition15.AddTrigger(new Trigger_Custom(delegate (TriggerSignal signal)
            {
                if (signal.type == TriggerSignalType.Tick && !bestower.Dead)
                {
                    if (ModsConfig.BiotechActive)
                    {
                        Hediff firstHediffOfDef = bestower.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxGasExposure);
                        if (firstHediffOfDef != null && firstHediffOfDef.Severity >= 0.9f)
                        {
                            return true;
                        }
                    }

                    Hediff firstHediffOfDef2 = bestower.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.LungRotExposure);
                    if (firstHediffOfDef2 != null && firstHediffOfDef2.Severity >= 0.9f)
                    {
                        return true;
                    }
                }

                return false;
            }));
            transition15.AddPreAction(action);
            transition15.AddPostAction(new TransitionAction_Custom((Action)delegate
            {
                compShuttle?.SetPawnToLeaveBehind((Pawn p) => p != bestower);
                bestower.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -50);
                Messages.Message("MessageBestowingDangerGas".Translate(), bestower, MessageTypeDefOf.NegativeEvent);
                QuestUtility.SendQuestTargetSignals(lord.questTags, "BeingAttacked", lord.Named("SUBJECT"));
            }));
            stateGraph.AddTransition(transition15);
            Transition transition16 = new Transition(lordToil_BestowingCeremony_MoveInPlace, exitToil);
            transition16.AddSource(lordToil_BestowingCeremony_Wait);
            transition16.AddTrigger(new Trigger_PawnHarmed(1f, requireInstigatorWithFaction: true, base.Map.ParentFaction));
            transition16.AddPreAction(action);
            transition16.AddPostAction(new TransitionAction_Custom((Action)delegate
            {
                compShuttle?.SetPawnToLeaveBehind((Pawn p) => p != bestower);
                Messages.Message("MessageBestowingDanger".Translate(), bestower, MessageTypeDefOf.NegativeEvent);
                QuestUtility.SendQuestTargetSignals(lord.questTags, "BeingAttacked", lord.Named("SUBJECT"));
            }));
            stateGraph.AddTransition(transition16);
            return stateGraph;
        }

        public override void Notify_InMentalState(Pawn pawn, MentalStateDef stateDef)
        {
            if (stateDef != MentalStateDefOf.SocialFighting)
            {
                lord.Notify_PawnLost(pawn, PawnLostCondition.InMentalState);
            }
        }

        public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
        {
            if (p == bestower || p == target)
            {
                MakeCeremonyFail();
            }
        }

        public void MakeCeremonyFail()
        {
            QuestUtility.SendQuestTargetSignals(lord.questTags, "CeremonyFailed", lord.Named("SUBJECT"));
            outcome.ResetCompDatas();
        }

        public override void LordJobTick()
        {
            if (ritual != null && ritual.behavior != null)
            {
                ritual.behavior.Tick(this);
            }

            if (ceremonyStarted)
            {
                outcome?.Tick(this);
            }

            if (lord.ownedPawns.Count == 0)
            {
                base.Map.lordManager.RemoveLord(lord);
            }
        }

        private bool CanUseSpot(IntVec3 spot)
        {
            if (!spot.InBounds(bestower.Map))
            {
                return false;
            }

            if (!spot.Standable(bestower.Map))
            {
                return false;
            }

            if (!GenSight.LineOfSight(spot, bestower.Position, bestower.Map))
            {
                return false;
            }

            if (!bestower.CanReach(targetSpot, PathEndMode.OnCell, Danger.Deadly))
            {
                return false;
            }

            return true;
        }

        private IntVec3 TryGetUsableSpotAdjacentToBestower()
        {
            foreach (int item in Enumerable.Range(1, 4).InRandomOrder())
            {
                IntVec3 result = bestower.Position + GenRadial.ManualRadialPattern[item];
                if (CanUseSpot(result))
                {
                    return result;
                }
            }

            return IntVec3.Invalid;
        }

        public IntVec3 GetSpot()
        {
            IntVec3 result = IntVec3.Invalid;
            if (targetSpot.Thing != null)
            {
                IntVec3 interactionCell = targetSpot.Thing.InteractionCell;
                IntVec3 intVec = spotCell;
                foreach (IntVec3 item in GenSight.PointsOnLineOfSight(interactionCell, intVec))
                {
                    if (!(item == interactionCell) && !(item == intVec) && CanUseSpot(item))
                    {
                        result = item;
                        break;
                    }
                }
            }

            if (result.IsValid)
            {
                return result;
            }

            return TryGetUsableSpotAdjacentToBestower();
        }

        public override string GetReport(Pawn pawn)
        {
            return "LordReportAttending".Translate("BestowingCeremonyLabel".Translate());
        }

        public void FinishCeremony(Pawn pawn)
        {
            lord.ReceiveMemo("CeremonyFinished");
            totalPresenceTmp.Clear();
            foreach (KeyValuePair<Pawn, int> presentForTick in ceremonyToil.Data.presentForTicks)
            {
                if (presentForTick.Key != null && !presentForTick.Key.Dead)
                {
                    if (!totalPresenceTmp.ContainsKey(presentForTick.Key))
                    {
                        totalPresenceTmp.Add(presentForTick.Key, presentForTick.Value);
                    }
                    else
                    {
                        totalPresenceTmp[presentForTick.Key] += presentForTick.Value;
                    }
                }
            }

            totalPresenceTmp.RemoveAll((KeyValuePair<Pawn, int> tp) => tp.Value < 2500);
            outcome.Apply(1f, totalPresenceTmp, this);
            outcome.ResetCompDatas();
        }

        public override IEnumerable<Gizmo> GetPawnGizmos(Pawn p)
        {
            if (p != bestower && p != target)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "CommandLeaveBestowingCeremony".Translate();
                command_Action.defaultDesc = "CommandLeaveBestowingCeremonyDesc".Translate();
                command_Action.icon = Icon;
                command_Action.action = delegate
                {
                    lord.Notify_PawnLost(p, PawnLostCondition.ForcedByPlayerAction);
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                };
                command_Action.hotKey = KeyBindingDefOf.Misc5;
                yield return command_Action;
            }
            else
            {
                if (!ceremonyStarted)
                {
                    yield break;
                }

                Command_Action command_Action2 = new Command_Action();
                command_Action2.defaultLabel = "CommandCancelBestowingCeremony".Translate();
                command_Action2.defaultDesc = "CommandCancelBestowingCeremonyDesc".Translate();
                command_Action2.icon = Icon;
                command_Action2.action = delegate
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CommandCancelBestowingCeremonyConfirm".Translate(), delegate
                    {
                        QuestUtility.SendQuestTargetSignals(lord.questTags, "CeremonyFailed", lord.Named("SUBJECT"));
                    }));
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                };
                command_Action2.hotKey = KeyBindingDefOf.Misc6;
                yield return command_Action2;
            }
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref bestower, "bestower");
            Scribe_References.Look(ref target, "target");
            Scribe_References.Look(ref shuttle, "shuttle");
            Scribe_TargetInfo.Look(ref targetSpot, "targetSpot");
            Scribe_Values.Look(ref questEndedSignal, "questEndedSignal");
            Scribe_Values.Look(ref spotCell, "spotCell");
            Scribe_Values.Look(ref ceremonyStarted, "ceremonyStarted", defaultValue: false);
            Scribe_Collections.Look(ref colonistParticipants, "colonistParticipants", LookMode.Reference);
        }
    }

    public class LordToil_TributeCeremony_Wait : LordToil_Wait
    {
        public Pawn target;

        public Pawn bestower;
        public LordToil_TributeCeremony_Wait(Pawn target, Pawn bestower)
        {
            this.target = target;
            this.bestower = bestower;
        }

        public override void Init()
        {
            Messages.Message("MessageBestowerWaiting".Translate(this.target.Named("TARGET"), this.lord.ownedPawns[0].Named("BESTOWER")), new LookTargets(new Pawn[]
            {
                this.target,
                this.lord.ownedPawns[0]
            }), MessageTypeDefOf.NeutralEvent, true);
        }

        protected override void DecoratePawnDuty(PawnDuty duty)
        {
            duty.focus = this.target;
        }

        public override void DrawPawnGUIOverlay(Pawn pawn)
        {
            pawn.Map.overlayDrawer.DrawOverlay(pawn, OverlayTypes.QuestionMark);
        }

        public override IEnumerable<Gizmo> GetPawnGizmos(Pawn p)
        {
            if (p == this.bestower)
            {
                LordJob_MakeTributeCeremony job = (LordJob_MakeTributeCeremony)this.lord.LordJob;
                yield return new Command_TributeCeremony(job, this.bestower, this.target, new Action<List<Pawn>>(this.StartRitual));
            }
            yield break;
        }

        public override IEnumerable<FloatMenuOption> ExtraFloatMenuOptions(Pawn target, Pawn forPawn)
        {
            if (target == this.bestower)
            {
                yield return new FloatMenuOption("BeginRitual".Translate("RitualBestowingCeremony".Translate()), delegate ()
                {
                    LordJob_MakeTributeCeremony lordJob_MakeTributeCeremony = (LordJob_MakeTributeCeremony)this.lord.LordJob;
                    Find.WindowStack.Add(new Dialog_BeginRitual("RitualBestowingCeremony".Translate(), null, lordJob_MakeTributeCeremony.targetSpot.ToTargetInfo(this.bestower.Map), this.bestower.Map, delegate (RitualRoleAssignments assignments)
                    {
                        this.StartRitual((from p in assignments.Participants
                                          where p != this.bestower
                                          select p).ToList<Pawn>());
                        return true;
                    }, this.bestower, null, delegate (Pawn pawn, bool voluntary, bool allowOtherIdeos)
                    {
                        Lord lord = pawn.GetLord();
                        return !(((lord != null) ? lord.LordJob : null) is LordJob_Ritual) && !pawn.IsMutant && !pawn.IsPrisonerOfColony && !pawn.RaceProps.Animal;
                    }, "Begin".Translate(), new List<Pawn>
                    {
                        this.bestower,
                        this.target
                    }, null, RitualOutcomeEffectDefOf.BestowingCeremony, null, null));
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
            }
            yield break;
        }

        private void StartRitual(List<Pawn> pawns)
        {
            foreach (Pawn pawn in pawns)
            {
                Lord lord = pawn.GetLord();
                if (((lord != null) ? lord.LordJob : null) is LordJob_VoluntarilyJoinable)
                {
                    pawn.GetLord().Notify_PawnLost(pawn, PawnLostCondition.LeftVoluntarily, null);
                }
            }
            this.lord.AddPawns(pawns, true);
            ((LordJob_MakeTributeCeremony)this.lord.LordJob).colonistParticipants.AddRange(pawns);
            this.lord.ReceiveMemo(LordJob_MakeTributeCeremony.MemoCeremonyStarted);
            foreach (Pawn pawn2 in pawns)
            {
                if (pawn2.drafter != null)
                {
                    pawn2.drafter.Drafted = false;
                }
                if (!pawn2.Awake())
                {
                    RestUtility.WakeUp(pawn2, true);
                }
            }
        }
    }

    public class Command_TributeCeremony : Command
    {
        private Pawn bestower;

        private Pawn forPawn;

        private Action<List<Pawn>> action;

        private LordJob_MakeTributeCeremony job;

        public Command_TributeCeremony(LordJob_MakeTributeCeremony job, Pawn bestower, Pawn forPawn, Action<List<Pawn>> action)
        {
            this.bestower = bestower;
            this.forPawn = forPawn;
            this.action = action;
            this.job = job;
            this.defaultLabel = "BeginCeremony".Translate(this.forPawn);
            this.icon = ContentFinder<Texture2D>.Get("UI/Icons/Rituals/BestowCeremony", true);
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            if (!JobDriver_BestowingCeremony.AnalyzeThroneRoom(this.bestower, this.forPawn))
            {
                this.disabledReason = "BestowingCeremonyThroneroomRequirementsNotSatisfiedShort".Translate(this.forPawn.Named("PAWN"), this.forPawn.royalty.GetTitleAwardedWhenUpdating(this.bestower.Faction, this.forPawn.royalty.GetFavor(this.bestower.Faction)).label.Named("TITLE"));
                this.disabled = true;
            }
            else if (!this.job.GetSpot().IsValid)
            {
                this.disabledReason = "MessageBestowerUnreachable".Translate();
                this.disabled = true;
            }
            else
            {
                Lord lord = this.forPawn.GetLord();
                if (lord != null)
                {
                    if (lord.LordJob is LordJob_Ritual)
                    {
                        this.disabledReason = "CantStartRitualTargetIsAlreadyInRitual".Translate(this.forPawn.LabelShort);
                        this.disabled = true;
                    }
                    else
                    {
                        this.disabledReason = "MessageBestowingTargetIsBusy".Translate(this.forPawn.LabelShort);
                        this.disabled = true;
                    }
                }
            }
            return base.GizmoOnGUI(topLeft, maxWidth, parms);
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            Find.WindowStack.Add(new Dialog_BeginRitual("RitualBestowingCeremony".Translate(), null, this.job.targetSpot.ToTargetInfo(this.bestower.Map), this.bestower.Map, delegate (RitualRoleAssignments assignments)
            {
                this.action((from p in assignments.Participants
                             where p != this.bestower
                             select p).ToList<Pawn>());
                return true;
            }, this.bestower, null, delegate (Pawn pawn, bool voluntary, bool allowOtherIdeos)
            {
                Lord lord = pawn.GetLord();
                return !(((lord != null) ? lord.LordJob : null) is LordJob_Ritual) && !pawn.IsMutant && !pawn.IsPrisonerOfColony && !pawn.RaceProps.Animal;
            }, "Begin".Translate(), new List<Pawn>
            {
                this.bestower,
                this.forPawn
            }, null, CrE_DefOf.TributeCeremony, null, null));
        }

    }

    public class LordToil_TributeCeremony_Perform : LordToil_Wait
    {

        public Pawn target;

        public Pawn bestower;

        public LordToilData_Gathering Data
        {
            get
            {
                return (LordToilData_Gathering)this.data;
            }
        }

        public LordToil_TributeCeremony_Perform(Pawn target, Pawn bestower) : base(true)
        {
            this.target = target;
            this.bestower = bestower;
            this.data = new LordToilData_Gathering();
        }

        public override void Init()
        {
            base.Init();
            if (!this.target.Awake())
            {
                RestUtility.WakeUp(this.target, true);
            }
        }
        public override void LordToilTick()
        {
            List<Pawn> ownedPawns = this.lord.ownedPawns;
            for (int i = 0; i < ownedPawns.Count; i++)
            {
                if (ownedPawns[i] == target && ownedPawns[i].health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken) != null)
                {
                    Util.Msg("HI");

                    LordJob_MakeTributeCeremony lordJob_MakeTributeCeremony = (LordJob_MakeTributeCeremony)this.lord.LordJob;
                    lordJob_MakeTributeCeremony.FinishCeremony(ownedPawns[i]);
                }

                if (GatheringsUtility.InGatheringArea(ownedPawns[i].Position, this.target.Position, base.Map))
                {
                    if (!this.Data.presentForTicks.ContainsKey(ownedPawns[i]))
                    {
                        this.Data.presentForTicks.Add(ownedPawns[i], 0);
                    }
                    Dictionary<Pawn, int> presentForTicks = this.Data.presentForTicks;
                    Pawn key = ownedPawns[i];
                    int num = presentForTicks[key];
                    presentForTicks[key] = num + 1;
                }
            }
        }

        public override void UpdateAllDuties()
        {
            IntVec3 spot = ((LordJob_MakeTributeCeremony)this.lord.LordJob).GetSpot();
            for (int i = 0; i < this.lord.ownedPawns.Count; i++)
            {
                Pawn pawn = this.lord.ownedPawns[i];

                pawn.jobs?.CheckForJobOverride();

                if (!pawn.Awake())
                {
                    RestUtility.WakeUp(pawn, true);
                }
                if (pawn == this.bestower)
                {
                    PawnDuty pawnDuty = new PawnDuty(DutyDefOf.Idle); // Here is where we get the think nodes
                    pawnDuty.focus = spot;
                    pawn.mindState.duty = pawnDuty;
                }
                else if (pawn == target)
                {
                    // This only runs once, why tf?

                    if (pawn.jobs == null)
                    {
                        PawnDuty duty = new PawnDuty(DutyDefOf.Bestow, bestower, spot);

                        pawn.mindState.duty = duty;
                    }

                    //PawnDuty duty = new PawnDuty(DutyDefOf.Spectate, spot);
                    //duty.spectateRect = CellRect.CenteredOn(spot, 0);
                    //duty.spectateRectAllowedSides = SpectateRectSide.All;
                    //duty.spectateDistance = new IntRange(2, 2);
                    //pawn.mindState.duty = duty;
                }
                else
                {
                    PawnDuty pawnDuty2 = new PawnDuty(DutyDefOf.Spectate, spot);
                    pawnDuty2.spectateRect = CellRect.CenteredOn(spot, 0);
                    pawnDuty2.spectateRectAllowedSides = SpectateRectSide.All;
                    pawnDuty2.spectateDistance = new IntRange(2, 2);
                    pawn.mindState.duty = pawnDuty2;
                }
            }
        }
    }

    public class RitualOutcomeEffectWorker_MakeTribute : RitualOutcomeEffectWorker_FromQuality
    {
        public RitualOutcomeEffectWorker_MakeTribute()
        {
        }

        public RitualOutcomeEffectWorker_MakeTribute(RitualOutcomeEffectDef def) : base(def)
        {
        }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            LordJob_MakeTributeCeremony lordJob_BestowingCeremony = (LordJob_MakeTributeCeremony)jobRitual;

            Pawn target = lordJob_BestowingCeremony.target;
            Pawn bestower = lordJob_BestowingCeremony.bestower;
            Hediff_Psylink mainPsylinkSource = target.GetMainPsylinkSource();

            float quality = GetQuality(jobRitual, progress);
            RitualOutcomePossibility outcome = GetOutcome(quality, jobRitual);
            LookTargets letterLookTargets = target;
            string extraLetterText = null;

            if (jobRitual.Ritual != null)
            {
                ApplyAttachableOutcome(totalPresence, jobRitual, outcome, out extraLetterText, ref letterLookTargets);
            }

            RoyalTitleDef currentTitle = target.royalty.GetCurrentTitle(bestower.Faction);
            RoyalTitleDef titleAwardedWhenUpdating = target.royalty.GetTitleAwardedWhenUpdating(bestower.Faction, 10);
            Pawn_RoyaltyTracker.MakeLetterTextForTitleChange(target, bestower.Faction, currentTitle, titleAwardedWhenUpdating, out var headline, out var body);
            if (target.royalty != null)
            {
                target.royalty.TryUpdateTitle(bestower.Faction, sendLetter: false, titleAwardedWhenUpdating);
            }

            List<AbilityDef> abilitiesPreUpdate = ((mainPsylinkSource == null) ? new List<AbilityDef>() : target.abilities.abilities.Select((Ability a) => a.def).ToList());
            //ThingOwner<Thing> innerContainer = bestower.inventory.innerContainer;
            //Thing thing = innerContainer.First((Thing t) => t.def == ThingDefOf.PsychicAmplifier);
            //innerContainer.Remove(thing);
            //thing.Destroy();

            //for (int i = target.GetPsylinkLevel(); i < target.GetMaxPsylinkLevelByTitle(); i++)
            //{
            //    target.ChangePsylinkLevel(1, sendLetter: false);
            //    Find.History.Notify_PsylinkAvailable();
            //}

            foreach (KeyValuePair<Pawn, int> item in totalPresence)
            {
                Pawn key = item.Key;
                if (key != target)
                {
                    key.needs.mood.thoughts.memories.TryGainMemory(outcome.memory);
                }
            }

            //int num = 0;
            //for (int num2 = def.honorFromQuality.PointsCount - 1; num2 >= 0; num2--)
            //{
            //    if (quality >= def.honorFromQuality[num2].x)
            //    {
            //        num = (int)def.honorFromQuality[num2].y;
            //        break;
            //    }
            //}

            //if (num > 0)
            //{
            //    target.royalty.GainFavor(bestower.Faction, num);
            //}

            List<AbilityDef> newAbilities = ((mainPsylinkSource == null) ? new List<AbilityDef>() : (from a in target.abilities.abilities
                                                                                                     select a.def into def
                                                                                                     where !abilitiesPreUpdate.Contains(def)
                                                                                                     select def).ToList());
            string text = headline;
            text = text + "\n\n" + Hediff_Psylink.MakeLetterTextNewPsylinkLevel(lordJob_BestowingCeremony.target, target.GetPsylinkLevel(), newAbilities);
            text = text + "\n\n" + body;
            if (extraLetterText != null)
            {
                text = text + "\n\n" + extraLetterText;
            }

            Find.LetterStack.ReceiveLetter("LetterLabelGainedRoyalTitle".Translate(titleAwardedWhenUpdating.GetLabelCapFor(target).Named("TITLE"), target.Named("PAWN")), text, LetterDefOf.RitualOutcomePositive, letterLookTargets, lordJob_BestowingCeremony.bestower.Faction);
            string text2 = OutcomeDesc(outcome, quality, progress, lordJob_BestowingCeremony, 0, totalPresence.Count);
            Find.LetterStack.ReceiveLetter("OutcomeLetterLabel".Translate(outcome.label.Named("OUTCOMELABEL"), "RitualBestowingCeremony".Translate().Named("RITUALLABEL")), text2, outcome.Positive ? LetterDefOf.RitualOutcomePositive : LetterDefOf.RitualOutcomeNegative, target);

        }

        private string OutcomeDesc(RitualOutcomePossibility outcome, float quality, float progress, LordJob_MakeTributeCeremony jobRitual, int honor, int totalPresence)
        {
            TaggedString taggedString = "BestowingOutcomeQualitySpecific".Translate(quality.ToStringPercent()) + ":\n";
            Pawn target = jobRitual.target;
            Pawn bestower = jobRitual.bestower;
            if (def.startingQuality > 0f)
            {
                taggedString += "\n  - " + "StartingRitualQuality".Translate(def.startingQuality.ToStringPercent()) + ".";
            }

            foreach (RitualOutcomeComp comp in def.comps)
            {
                if (comp is RitualOutcomeComp_Quality && comp.Applies(jobRitual) && Mathf.Abs(comp.QualityOffset(jobRitual, DataForComp(comp))) >= float.Epsilon)
                {
                    taggedString += "\n  - " + comp.GetDesc(jobRitual, DataForComp(comp)).CapitalizeFirst();
                }
            }

            if (progress < 1f)
            {
                taggedString += "\n  - " + "RitualOutcomeProgress".Translate("RitualBestowingCeremony".Translate()) + ": x" + Mathf.Lerp(RitualOutcomeEffectWorker_FromQuality.ProgressToQualityMapping.min, RitualOutcomeEffectWorker_FromQuality.ProgressToQualityMapping.max, progress).ToStringPercent();
            }

            taggedString += "\n\n";
            if (honor > 0)
            {
                taggedString += "LetterPartBestowingExtraHonor".Translate(target.Named("PAWN"), honor, bestower.Faction.Named("FACTION"), totalPresence);
            }
            else
            {
                taggedString += "LetterPartNoExtraHonor".Translate(target.Named("PAWN"));
            }

            taggedString += "\n\n" + "LordJobOutcomeChances".Translate(quality.ToStringPercent()) + ":\n";
            float num = 0f;
            foreach (RitualOutcomePossibility outcomeChance in def.outcomeChances)
            {
                num += (outcomeChance.Positive ? (outcomeChance.chance * quality) : outcomeChance.chance);
            }

            foreach (RitualOutcomePossibility outcomeChance2 in def.outcomeChances)
            {
                taggedString += "\n  - ";
                if (outcomeChance2.Positive)
                {
                    taggedString += outcomeChance2.memory.stages[0].LabelCap + ": " + (outcomeChance2.chance * quality / num).ToStringPercent();
                }
                else
                {
                    taggedString += outcomeChance2.memory.stages[0].LabelCap + ": " + (outcomeChance2.chance / num).ToStringPercent();
                }
            }

            return taggedString;

        }
    }
}
