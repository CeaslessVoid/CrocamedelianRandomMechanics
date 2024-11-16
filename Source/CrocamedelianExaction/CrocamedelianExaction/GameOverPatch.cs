//using HarmonyLib;
//using LudeonTK;
//using RimWorld.Planet;
//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//using Verse;

//namespace CrocamedelianExaction
//{
//    [HarmonyPatch(typeof(GameEnder), "CheckGameOver")]
//    public static class Patch_GameEnder_CheckGameOver
//    {
//        static void Postfix(GameEnder __instance)
//        {
//            if (ShouldTriggerGameOver(__instance))
//            {
//                TransferCapturedPawnsToWorldPawns();
//            }
//        }

//        private static bool ShouldTriggerGameOver(GameEnder instance)
//        {
//            return instance.gameEnding;
//        }

//        private static void TransferCapturedPawnsToWorldPawns()
//        {
//            var capturedPawnsQueue = CrE_GameComponent.CapturedPawnsQue;

//            if (capturedPawnsQueue != null)
//            {
//                foreach (Pawn pawn in capturedPawnsQueue)
//                {
//                    if (!pawn.Dead && pawn.Spawned)
//                    {
//                        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
//                    }
//                }

//                capturedPawnsQueue.Clear();
//            }
//        }
//    }
//}
