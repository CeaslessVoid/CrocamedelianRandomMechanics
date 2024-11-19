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

namespace CrocamedelianExaction
{
    public static class Debug
    {
        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Print Current Points", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void PrintCrEPoints() // Prints current CrE points
        {
            Util.Msg("Points " + CrE_GameComponent.CrE_Points);
            Util.Msg(CrE_GameComponent.has_pawn_out);
            Util.Msg("Modifier " +(float)Math.Round(Math.Exp(2 * ((1 / (1 + Mathf.Exp(-0.02f * CrE_GameComponent.CrE_Points))) - 0.5f)) - 1, 2));
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Add 10 points", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void Add10Points() // Prints current CrE points
        {
            CrE_GameComponent.ChangeCrEPoints(10);
            PrintCrEPoints();
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Apply Random Tatto To Random Pawn", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void RandomPawnTattoo() // Apply Random Tattoo to Random Pawn
        {
            CrE_GameComponent.RapeTattoo(CrE_GameComponent.GetRandomPawnForEvent());
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Print Current Pawn", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void PrintCurrentVictim() // Prints current CrE vicitim pawn
        {
            Util.Msg(CrE_GameComponent.CurrentCrEPawn);
            Util.Msg(CrE_GameComponent.CrE_Pawn_Return_Time);
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Print Settings", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void PrintRandomValidPawn() // Prints current CrE points
        {
            Util.Msg(CrE_GameComponent.Settings.CrE_Male);
            Util.Msg(CrE_GameComponent.Settings.CrE_Female);
            Util.Msg(CrE_GameComponent.Settings.CrE_ExtortLossChance);
            Util.Msg(CrE_GameComponent.Settings.CrE_maxDaysBetweenEvents);
            Util.Msg(CrE_GameComponent.Settings.CrE_minDaysBetweenEvents);
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Do Pirate Exortion", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void TestDoInci() // Prints current CrE points
        {
            CrE_PawnExtort_Test.Do();
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Return the pawn", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void TestDoInciReturn() // Prints current CrE points
        {
            CrE_PiratePawn_Return.Do();
        }

        [DebugAction(null, null, false, false, false, false, 0, false, category = "Crocamedelian Random Mechanics", name = "Send all kidnapped to kidnapper faction", requiresRoyalty = false, requiresIdeology = false, requiresBiotech = false, actionType = 0, allowedGameStates = LudeonTK.AllowedGameStates.Playing)]
        private static void ToFactionKidnapped() // Prints current CrE points
        {
            CrE_GameComponent.transfercapturedpawnstoworldpawns();
        }

        private const string CATEGORY = "Crocamedelian Random Mechanics";
    }
}
