using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using rjw;
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

        public static void HealPawn(Pawn pawn)
        {
            IEnumerable<Hediff> enumerable = from hd in pawn.health.hediffSet.hediffs
                                             where !hd.IsTended() && hd.TendableNow()
                                             select hd;

            if (enumerable != null)
            {
                foreach (Hediff item in enumerable)
                {
                    HediffWithComps val = item as HediffWithComps;
                    if (val != null)
                        if (val.Bleeding)
                        {
                            val.Heal(1.0f);
                        }
                        else if ((!val.def.chronic && val.def.lethalSeverity > 0f) || (val.CurStage?.lifeThreatening ?? false))
                        {
                            HediffComp_TendDuration val2 = HediffUtility.TryGetComp<HediffComp_TendDuration>(val);
                            val2.tendQuality = 1f;
                            val2.tendTicksLeft = 10000;
                            pawn.health.Notify_HediffChanged(item);
                        }
                }
            }

            List<Hediff> hediffsToRemove = pawn.health.hediffSet.hediffs
                .Where(hd =>
                    pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) == 10 ||
                    pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) <= 10 ||
                    pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 10)
                .ToList();

            foreach (Hediff hediff in hediffsToRemove)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        public static void GiveBadTraits(Pawn pawn)
        {
            if (pawn == null) return;

            if (pawn.story.traits.HasTrait(xxx.rapist))
            {
                pawn.story.traits.RemoveTrait(pawn.story.traits.GetTrait(xxx.rapist));
            }

            if (Rand.Chance(0.5f) && !(pawn.story.traits.HasTrait(xxx.masochist)))
            {
                pawn.story.traits.GainTrait(pawn.story.traits.GetTrait(xxx.masochist));
            }
        }

        public static void DressPawnIfCold(Pawn pawn, int tile)
        {
            PawnApparelGenerator.GenerateStartingApparelFor(pawn, new PawnGenerationRequest(pawn.kindDef, pawn.Faction, RimWorld.PawnGenerationContext.NonPlayer, tile, false, false, false, false, false, 0f, true, true, true, true, true, false, false, false, true, 0f, 0f, null, 0f, null, null, null, null, new float?(0f), null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, pawn.DevelopmentalStage, null, null, null, false, false, false, -1, 0, false));
        }
    }

}
