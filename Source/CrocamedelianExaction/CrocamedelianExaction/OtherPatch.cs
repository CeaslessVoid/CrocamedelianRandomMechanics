using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
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

}
