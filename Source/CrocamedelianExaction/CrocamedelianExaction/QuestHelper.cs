using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using rjw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.Noise;


namespace CrocamedelianExaction
{
    [HarmonyPatch(typeof(Site), "ShouldRemoveMapNow")]
    internal class Patch_ShouldRemoveMapNow
    {
        private static void Postfix(ref Site __instance, ref bool alsoRemoveWorldObject, ref bool __result)
        {
            if (GenCollection.Any<Pawn>(__instance.Map.mapPawns.AllHumanlikeSpawned, (Pawn x) => x.IsColonist))
            {
                Map map = __instance.Map;

                Pawn prisoner = FindPrisonerInMap(map);

                if (prisoner != null)
                {
                    //if (prisoner.health.hediffSet.HasHediff(xxx.feelingBroken))
                    //{
                    //    __result = true;
                    //}
                    //else
                    //{
                    //    __result = false;
                    //    alsoRemoveWorldObject = false;
                    //}

                    __result = false;
                    alsoRemoveWorldObject = false;
                }
            }

        }

        private static Pawn FindPrisonerInMap(Map map)
        {
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.IsPrisoner)
                {
                    return pawn;
                }
            }
            return null;
        }
    }





    // Taken from RJW. I mostly don't own this code

    // This normal only runs in passive mode. After map being open for some time they will swtich to active mode
    class JobGiver_AIRapePrisoner : ThinkNode_JobGiver
    {
        public static Pawn find_victim(Pawn pawn, Map m)
        {
            float avg_fuckability = 0f;
            var valid_targets = new Dictionary<Pawn, float>();
            Pawn chosentarget = null;

            IEnumerable<Pawn> targets = m.mapPawns.AllPawns.Where(x
                => x != pawn
                && x.Faction != pawn.Faction
                && x.IsHuman()
                && pawn.CanReserveAndReach(x, PathEndMode.Touch, Danger.Some, xxx.max_rapists_per_prisoner, 0)
                && !x.Position.IsForbidden(pawn)
                );

            foreach (Pawn target in targets)
            {
                if (!IsPrisonerOf(target, pawn.Faction))
                    return null;

                if (!Pather_Utility.cells_to_target_rape(pawn, target.Position))
                    continue;

                if (Pather_Utility.can_path_to_target(pawn, target.Position))
                    valid_targets.Add(target, 10f);
            }

            if (valid_targets.Any())
            {
                avg_fuckability = valid_targets.Average(x => x.Value);

                var valid_targetsFiltered = valid_targets.Where(x => x.Value >= avg_fuckability);

                if (valid_targetsFiltered.Any())
                    chosentarget = valid_targetsFiltered.RandomElement().Key;
            }

            return chosentarget;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (RJWSettings.DebugRape) ModLog.Message(" JobGiver_AIRapePrisoner Crocamedelian's ::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) called ");

            if (!xxx.can_rape(pawn, true)) return null;

            if (xxx.is_healthy(pawn))
            {
                Pawn prisoner = find_victim(pawn, pawn.Map);

                if (prisoner != null)
                {
                    if (RJWSettings.DebugRape) ModLog.Message(" JobGiver_RandomRape::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) - found victim " + xxx.get_pawnname(prisoner));

                    return JobMaker.MakeJob(xxx.RapeRandom, prisoner);
                }
            }

            return null;
        }

        protected static bool IsPrisonerOf(Pawn pawn, Faction faction)
        {
            if (pawn?.guest == null) return false;
            return pawn.guest.IsPrisoner;
        }
    }

}
