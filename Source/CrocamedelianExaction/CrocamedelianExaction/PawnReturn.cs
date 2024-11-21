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
using Unity.Jobs.LowLevel.Unsafe;
using rjw;

namespace CrocamedelianExaction
{
    public class CrE_PiratePawn_Return : IncidentWorker
    {
        public static bool Do()
        {
            Map randomPlayerHomeMap = Current.Game.RandomPlayerHomeMap;
            IntVec3 intVec;
            if (!CrE_PiratePawn_Return.TryFindEntryCell(randomPlayerHomeMap, out intVec))
            {
                return false;
            }
            Pawn pawn = CrE_GameComponent.CurrentCrEPawn;
            if (pawn == null)
            {
                return false;
            }
            pawn.SetFactionDirect(Faction.OfPlayer);

            float brokenSeverityGain = Rand.Range(0.3f, 0.7f);
            pawn.needs.mood.thoughts.memories.TryGainMemory(xxx.got_raped);
            pawn.health.AddHediff(xxx.feelingBroken, null, null, null);
            pawn.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken).Severity += brokenSeverityGain;
            pawn.needs.mood.thoughts.memories.TryGainMemory(CrE_DefOf.FeelingBroken);

            var thoughtDef = ThoughtDef.Named("PirateForceWork");
            pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);

            GenSpawn.Spawn(pawn, intVec, randomPlayerHomeMap, 0);
            IncidentDef CrE_PawnReturn = CrE_DefOf.CrE_PiratePawn_Return;
            TaggedString taggedString = GrammarResolverSimpleStringExtensions.Formatted(CrE_PawnReturn.letterLabel, NamedArgumentUtility.Named(pawn, "PAWN")).AdjustedFor(pawn, "PAWN", true);
            TaggedString taggedString2 = GrammarResolverSimpleStringExtensions.Formatted(CrE_PawnReturn.letterText, NamedArgumentUtility.Named(pawn, "PAWN")).AdjustedFor(pawn, "PAWN", true);
            Find.LetterStack.ReceiveLetter(taggedString, taggedString2, CrE_PawnReturn.letterDef, new LookTargets(pawn), null, null, null, null, 0, true);

            CrE_GameComponent.CurrentCrEPawn = null;
            return true;
        }

        public static bool TryFindEntryCell(Map map, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => map.reachability.CanReachColony(c) && !GridsUtility.Fogged(c, map), map, CellFinder.EdgeRoadChance_Neutral, out cell);
        }
    }

    // Not returned
    public class CrE_PiratePawn_NoReturn : IncidentWorker
    {
        public static bool Do()
        {
            Pawn pawn = CrE_GameComponent.CurrentCrEPawn;
            if (pawn == null)
            {
                return false;
            }
            //CrE_GameComponent.CapturedPawnsQue.Add(pawn);
            CrE_GameComponent.MakePawnSlave(pawn);

            if (CrE_GameComponent.Settings.CrE_forceRescue)
            {
                CrE_GameComponent.CapturedPawnsQueue.Add(pawn);
                CrE_GameComponent.GetNextPrisonerTime(true);
            }

            foreach (Pawn colonist in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
            {

                if (colonist.needs?.mood?.thoughts != null)
                {
                    var thoughtDef = ThoughtDef.Named("PirateNoReturn");
                    colonist.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
                }
            }

            IncidentDef CrE_PawnReturn = CrE_DefOf.CrE_PiratePawn_NoReturn;
            TaggedString taggedString = GrammarResolverSimpleStringExtensions.Formatted(CrE_PawnReturn.letterLabel, NamedArgumentUtility.Named(pawn, "PAWN")).AdjustedFor(pawn, "PAWN", true);
            TaggedString taggedString2 = GrammarResolverSimpleStringExtensions.Formatted(CrE_PawnReturn.letterText, NamedArgumentUtility.Named(pawn, "PAWN")).AdjustedFor(pawn, "PAWN", true);
            Find.LetterStack.ReceiveLetter(taggedString, taggedString2, CrE_PawnReturn.letterDef, new LookTargets(pawn), null, null, null, null, 0, true);

            CrE_GameComponent.CurrentCrEPawn = null;
            return true;
        }

    }

}
