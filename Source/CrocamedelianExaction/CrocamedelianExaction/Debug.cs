using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static CrocamedelianExaction.SitePartWorker_CrEPrisonerRescue;
using static System.Collections.Specialized.BitVector32;

namespace CrocamedelianExaction
{
    public static class Debug
    {
        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Print Current Points", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void PrintCrEPoints() // Prints current CrE points
        {
            Util.Msg("Points " + CrE_GameComponent.CrE_Points);
            Util.Msg("Modifier " +(float)Math.Round(Math.Exp(2 * ((1 / (1 + Mathf.Exp(-0.02f * CrE_GameComponent.CrE_Points))) - 0.5f)) - 1, 2));
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Add 10 points", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void Add10Points() // Prints current CrE points
        {
            CrE_GameComponent.ChangeCrEPoints(10);
            PrintCrEPoints();
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Force Recalculate Captured Pawns", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void CalcCapturedPawns()
        {
            CrE_GameComponent.AddAllCapturedPawns();
        }


        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Print Captured Pawns Available", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void PrintPossiblePrisoners()
        {
            for (int i = 0; i < CrE_GameComponent.CrECapturePawns.Count; i++)
            {
                Log.Message(CrE_GameComponent.CrECapturePawns[i]);
            }
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Print Extorted Pawn List", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void PrintCurrentVictim()
        {
            for (int i = 0; i < CrE_GameComponent.PirateExtortPawn.Count; i++)
            {
                Log.Message(CrE_GameComponent.PirateExtortPawn[i]);
            }
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Do Prisoner Rescue Event", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void DoPrisonerRescueEvent() // Prints current CrE points
        {

            if (CrE_GameComponent.CrECapturePawns.Count != 0)
            {
                Util.Msg("Quest Started");
                IncidentCrPrisonerRescue.Do();
            }
            else
            {
                Util.Warn("No avaiable prisoner");
            }
            
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Test", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void Test()
        {
            CrE_GameComponent.GetRandomPirateFaction(out CrE_GameComponent.CrETributeFaction);
            Pawn pawn;

            if (!CrE_GameComponent.TryGetLeaderForTribute(out pawn) || pawn == null)
            {
                Util.Warn("No Faction Leader Found");
                return;
            }

            Slate slate = new Slate();
            slate.Set<Faction>("domFaction", CrE_GameComponent.CrETributeFaction, false);
            slate.Set<Pawn>("randomPawn", pawn, false);

            QuestScriptDef questDef = DefDatabase<QuestScriptDef>.GetNamed("CrEPirateTributeQuest");
            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);

            //quest.Accept(null);
        }

        private const string CATEGORY = "Crocamedelian Random Mechanics";

    }
}
