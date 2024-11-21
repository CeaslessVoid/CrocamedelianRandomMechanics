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
using Verse;
using Verse.Grammar;

namespace CrocamedelianExaction
{
    internal static class Util
    {
        public static void Msg(object o)
        {
            Log.Message("[CrE] " + ((o != null) ? o.ToString() : null));
        }

        public static void Warn(object o)
        {
            Log.Warning("[CrE] " + ((o != null) ? o.ToString() : null));
        }

        public static void Error(object o)
        {
            Log.Error("[CrE] " + ((o != null) ? o.ToString() : null));
        }

        public static void SitePartWorker_Base_Notify_GeneratedByQuestGen(SitePart part, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
        {
            outExtraDescriptionRules.AddRange(GrammarUtility.RulesForDef("", part.def));
            outExtraDescriptionConstants.Add("sitePart", part.def.defName);
        }

        public static void DressPawnIfCold(Pawn pawn, int tile)
        {
            PawnApparelGenerator.GenerateStartingApparelFor(pawn, new PawnGenerationRequest(pawn.kindDef, pawn.Faction, RimWorld.PawnGenerationContext.NonPlayer, tile, false, false, false, false, false, 0f, true, true, true, true, true, false, false, false, true, 0f, 0f, null, 0f, null, null, null, null, new float?(0f), null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, pawn.DevelopmentalStage, null, null, null, false, false, false, -1, 0, false));
        }
    }

}
