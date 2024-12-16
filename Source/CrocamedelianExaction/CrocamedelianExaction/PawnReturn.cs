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
    //Fires a raid if the pawn doesn't leave (player captures victim)
    public class CrE_PiratePawn_NoPawn : IncidentWorker
    {
        private static Pawn target;
        private static Faction faction;

        public static void Initialize(Pawn pawn, Faction extorter)
        {
            target = pawn;
            faction = extorter;
        }

        public static bool Do()
        {

            target.SetFactionDirect(Faction.OfPlayer);

            IncidentDef CrE_PiratePawn_NoPawn = CrE_DefOf.CrE_PiratePawn_NoPawn;
            TaggedString taggedString = GrammarResolverSimpleStringExtensions.Formatted(
                CrE_PiratePawn_NoPawn.letterLabel,
                new NamedArgument(faction, "FACTION")
            );

            TaggedString taggedString2 = GrammarResolverSimpleStringExtensions.Formatted(CrE_PiratePawn_NoPawn.letterText, NamedArgumentUtility.Named(target, "PAWN")).AdjustedFor(target, "PAWN", true);

            Find.LetterStack.ReceiveLetter(taggedString, taggedString2, CrE_PiratePawn_NoPawn.letterDef, new LookTargets(target), null, null, null, null, 0, true);


            var incidentParms =
                            StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, Find.AnyPlayerHomeMap);
            incidentParms.forced = true;
            incidentParms.faction = faction;
            incidentParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            incidentParms.target = Find.AnyPlayerHomeMap;

            IncidentDefOf.RaidEnemy.Worker.TryExecute(incidentParms);

            return true;
        }
    }

    // Returns the pawn from extortion
    public class CrE_PiratePawn_Return : IncidentWorker
    {
        private static Pawn target;
        private static Faction faction;

        public static void Initialize(Pawn pawn, Faction f)
        {
            target = pawn;
            faction = f;
        }
        public static bool Do()
        {
            faction.kidnapped.KidnappedPawnsListForReading.Remove(target);

            Map randomPlayerHomeMap = Current.Game.RandomPlayerHomeMap;
            IntVec3 intVec;
            if (!CrE_PiratePawn_Return.TryFindEntryCell(randomPlayerHomeMap, out intVec))
            {
                return false;
            }

            target.SetFactionDirect(Faction.OfPlayer);

            float brokenSeverityGain = Rand.Range(0.3f, 0.7f);
            target.needs.mood.thoughts.memories.TryGainMemory(xxx.got_raped);
            target.health.AddHediff(xxx.feelingBroken, null, null, null);
            target.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken).Severity += brokenSeverityGain;

            var thoughtDef = ThoughtDef.Named("PirateForceWork");
            target.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);

            GenSpawn.Spawn(target, intVec, randomPlayerHomeMap, 0);
            IncidentDef CrE_PawnReturn = CrE_DefOf.CrE_PiratePawn_Return;
            TaggedString taggedString = GrammarResolverSimpleStringExtensions.Formatted(CrE_PawnReturn.letterLabel, NamedArgumentUtility.Named(target, "PAWN")).AdjustedFor(target, "PAWN", true);
            TaggedString taggedString2 = GrammarResolverSimpleStringExtensions.Formatted(CrE_PawnReturn.letterText, NamedArgumentUtility.Named(target, "PAWN")).AdjustedFor(target, "PAWN", true);
            Find.LetterStack.ReceiveLetter(taggedString, taggedString2, CrE_PawnReturn.letterDef, new LookTargets(target), null, null, null, null, 0, true);

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
        private static Pawn target;

        public static void Initialize(Pawn pawn)
        {
            target = pawn;
        }
        public static bool Do()
        {

            foreach (Pawn colonist in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
            {
                if (colonist.needs?.mood?.thoughts != null)
                {
                    var thoughtDef = ThoughtDef.Named("PirateNoReturn");
                    colonist.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
                }
            }

            IncidentDef CrE_PawnReturn = CrE_DefOf.CrE_PiratePawn_NoReturn;
            TaggedString taggedString = GrammarResolverSimpleStringExtensions.Formatted(CrE_PawnReturn.letterLabel, NamedArgumentUtility.Named(target, "PAWN")).AdjustedFor(target, "PAWN", true);
            TaggedString taggedString2 = GrammarResolverSimpleStringExtensions.Formatted(CrE_PawnReturn.letterText, NamedArgumentUtility.Named(target, "PAWN")).AdjustedFor(target, "PAWN", true);
            Find.LetterStack.ReceiveLetter(taggedString, taggedString2, CrE_PawnReturn.letterDef, new LookTargets(target), null, null, null, null, 0, true);

            return true;
        }

    }

}
