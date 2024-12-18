using HarmonyLib;
using LudeonTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static CrocamedelianExaction.SitePartWorker_CrEPrisonerRescue;

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

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Print Captured Pawns Available", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void PrintPossiblePrisoners() // Prints current CrE points
        {
            for (int i = 0; i < CrE_GameComponent.CrECapturePawns.Count; i++)
            {
                Log.Message(CrE_GameComponent.CrECapturePawns[i]);
            }
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Print Extorted Pawn List", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void PrintCurrentVictim() // Prints current CrE vicitim pawn
        {
            for (int i = 0; i < CrE_GameComponent.PirateExtortPawn.Count; i++)
            {
                Log.Message(CrE_GameComponent.PirateExtortPawn[i]);
            }
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Do Prisoner Rescue Event", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void DoPrisonerRescueEvent() // Prints current CrE points
        {

            if (CrE_GameComponent.GetRandomPrisoner() != null)
            {
                IncidentCrPrisonerRescue.Do();
            }
            else
            {
                Util.Warn("No avaiable prisoner");
            }
            
        }

        private const string CATEGORY = "Crocamedelian Random Mechanics";
    }
}
