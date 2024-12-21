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

namespace CrocamedelianExaction
{
    public class QuestNode_Root_CrEPirateTributary : QuestNode
    {
        private bool TryGetTributaryTarget(Slate slate, out Faction faction, out Pawn pawn)
        {
            faction = null;
            pawn = null;

            if (CrE_GameComponent.CrETributeFaction == null || CrE_GameComponent.CrETributeFaction.IsPlayer)
            {
                return false;
            }

            // Find a player home map
            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome)
                {
                    slate.TryGet<Faction>("domFaction", out faction, false);
                    slate.TryGet<Pawn>("randomPawn", out pawn, false);
                    //playerMap = map;
                    //pirateFaction = CrE_GameComponent.CrETributeFaction;
                    return true;
                }
            }

            return false;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;

            Faction faction;
            Pawn pawn;

            if (!TryGetTributaryTarget(QuestGen.slate, out faction, out pawn))
            {
                Util.Error("Failed to find a valid target for the Pirate Tributary quest.");
                return;
            }

            string text = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("Submitting");

            //string text2 = QuestGenUtility.QuestTagSignal(text, "CrETributeExpired");
            //string inSignal = QuestGenUtility.QuestTagSignal(text, "CrETributeFailed");
            //string inSignal2 = QuestGenUtility.QuestTagSignal(text, "CrETributeDone");
            //string inSignal3 = QuestGenUtility.QuestTagSignal(text, "BeingAttacked");
            //string inSignal4 = QuestGenUtility.QuestTagSignal(text, "Fleeing");
            //string inSignal5 = QuestGenUtility.QuestTagSignal(text, "TitleAwardedWhenUpdatingChanged");

            Thing thing = QuestGen_Shuttle.GenerateShuttle(faction, null, null, false, false, false, -1, false, false, false, false, null, null, -1, null, false, true, false, false, false);
            
            Pawn pawn2 = quest.GetPawn(new QuestGen_Pawns.GetPawnParms
            {
                mustBeOfKind = faction.RandomPawnKind(),
                canGeneratePawn = true,
                mustBeOfFaction = faction,
                mustBeWorldPawn = true,
                ifWorldPawnThenMustBeFree = true,
                redressPawn = true
            });

            QuestUtility.AddQuestTag(ref thing.questTags, text);

            List<Pawn> list = new List<Pawn>();
            slate.Set<List<Pawn>>("shuttleContents", list, false);
            slate.Set<Thing>("shuttle", thing, false);
            slate.Set<Pawn>("leader", pawn2, false);
            slate.Set<Faction>("domFaction", faction, false);
            List<Pawn> list2 = new List<Pawn>();
            for (int k = 0; k < 6; k++)
            {
                Pawn item = quest.GeneratePawn(faction.RandomPawnKind(), faction, true, null, 0f, true, null, 0f, 0f, false, false, DevelopmentalStage.Adult, false);
                list.Add(item);
                list2.Add(item);
            }
            quest.EnsureNotDowned(list, null);
            slate.Set<List<Pawn>>("defenders", list2, false);
            thing.TryGetComp<CompShuttle>().requiredPawns = list;
            TransportShip transportShip = quest.GenerateTransportShip(TransportShipDefOf.Ship_Shuttle, list, thing, null).transportShip;
            quest.AddShipJob_Arrive(transportShip, null, pawn, null, ShipJobStartMode.Instant, faction, null);
            quest.AddShipJob(transportShip, ShipJobDefOf.Unload, ShipJobStartMode.Queue, null);
            quest.AddShipJob_WaitForever(transportShip, true, false, list.Cast<Thing>().ToList<Thing>(), null).sendAwayIfAnyDespawnedDownedOrDead = new List<Thing>
            {
                pawn2
            };
            QuestUtility.AddQuestTag(ref transportShip.questTags, text);
            quest.FactionGoodwillChange(faction, -100, QuestGenUtility.HardcodedSignalWithQuestID("defenders.Killed"), true, true, true, HistoryEventDefOf.QuestPawnLost, QuestPart.SignalListenMode.OngoingOnly, false);






            List<Verse.Grammar.Rule> list1 = new List<Verse.Grammar.Rule>();
            list1.Add(new Rule_String("faction_name", faction.Name));
            QuestGen.AddQuestDescriptionRules(list1);

        }

        protected override bool TestRunInt(Slate slate)
        {
            Faction pirateFaction;
            Pawn pawn;

            return this.TryGetTributaryTarget(slate, out pirateFaction, out pawn);
        }

        public const string QuestTag = "Submitting";
    }

    public class QuestPart_MakeTributeCeremony : QuestPart_MakeLord
    {
        public override bool QuestPartReserves(Pawn p)
        {
            return p == this.leader || p == this.target;
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

            if (!QuestPart_MakeTributeCeremony.TryGetCeremonySpot(this.target, this.leader.Faction, out targetSpot, out spotCell))
            {
                Util.Error("Cannot find ceremony spot for bestowing ceremony!");
                return null;
            }

            Lord lord = LordMaker.MakeNewLord(this.faction, new LordJob_BestowingCeremony(this.leader, this.target, targetSpot, spotCell, this.shuttle, this.questTag + ".QuestEnded"), base.Map, null);
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
            Scribe_References.Look<Pawn>(ref this.leader, "leader", false);
            Scribe_References.Look<Pawn>(ref this.target, "target", false);
            Scribe_References.Look<Thing>(ref this.shuttle, "shuttle", false);
            Scribe_Values.Look<string>(ref this.questTag, "questTag", null, false);
        }

        public Pawn leader;

        public Pawn target;

        public Thing shuttle;

        public string questTag;
    }

}
