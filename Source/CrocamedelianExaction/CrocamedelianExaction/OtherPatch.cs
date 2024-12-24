using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CrocamedelianExaction
{
    [HarmonyPatch(typeof(IncidentWorker_Raid), "TryExecuteWorker")]
    public static class Patch_IncidentWorker_Raid_TryExecuteWorker
    {
        static bool Prefix(ref bool __result, IncidentParms parms)
        {
            if (parms.faction != null && IsFactionOnCooldown(parms.faction))
            {
                __result = false;
                return false;
            }

            return true;
        }

        private static bool IsFactionOnCooldown(Faction faction)
        {
            if (CrE_GameComponent.FactionRaidCooldowns.TryGetValue(faction, out int nextRaidTick))
            {
                return Find.TickManager.TicksGame < nextRaidTick;
            }
            return false;
        }
    }

    [HarmonyPatch]
    public static class Patch_Settlement_Visitable
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Settlement), nameof(Settlement.Visitable));
        }

        static bool Prefix(ref bool __result, Settlement __instance)
        {
            __result = __instance.Faction != Faction.OfPlayer;
            return false;
        }
    }

    [HarmonyPatch(typeof(Quest), "End")]
    public static class Patch_Quest_End
    {
        static void Postfix(Quest __instance, QuestEndOutcome outcome)
        {
            if (outcome != QuestEndOutcome.Fail)
            {
                return;
            }

            var involvedFactions = __instance.InvolvedFactions;

            if (involvedFactions.Count() != 1)
            {
                return;
            }

            Faction faction = involvedFactions.First();

            if (faction != null && faction.def.permanentEnemy)
            {
                var incidentParms =
                            StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, Find.AnyPlayerHomeMap);
                incidentParms.forced = true;
                incidentParms.faction = faction;
                incidentParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                incidentParms.target = Find.AnyPlayerHomeMap;

                IncidentDefOf.RaidEnemy.Worker.TryExecute(incidentParms);
            }
        }
    }

}
