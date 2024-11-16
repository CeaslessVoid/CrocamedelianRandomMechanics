using HarmonyLib;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
//using MoreFactionInteraction;
using RimWorld.Planet;

namespace CrocamedelianExaction
{
    public static class CrE_PawnExtort_Test
    {
        public static bool Do()
        {

            IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamed("CrE_PiratePawn_Extort", true);

            var incidentParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.AnyPlayerHomeMap);
            incidentParms.forced = true;
            incidentParms.target = Find.AnyPlayerHomeMap;

            bool result = incidentDef.Worker.TryExecute(incidentParms);


            return true;

        }

    }

}
