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
using UnityEngine;

//namespace CrocamedelianExaction
//{
//    public class CrE_ChoiceLetter_ExtortionDemand : ChoiceLetter_ExtortionDemand
//    {
//        public override IEnumerable<DiaOption> Choices
//        {
//            get
//            {
//                if (ArchivedOnly)
//                {
//                    yield return Option_Close;
//                }
//                else
//                {
//                    var accept = new DiaOption("RansomDemand_Accept".Translate())
//                    {
//                        action = () =>
//                        {
//                            completed = true;
//                            TradeUtility.LaunchSilver(map, fee);
//                            Find.LetterStack.RemoveLetter(this);
//                            CrE_GameComponent.ChangeCrEPoints(Rand.Range(2, 3));
//                        },
//                        resolveTree = true
//                    };
//                    if (!TradeUtility.ColonyHasEnoughSilver(map, fee))
//                    {
//                        accept.Disable("NeedSilverLaunchable".Translate(fee.ToString()));
//                    }

//                    yield return accept;

//                    var reject = new DiaOption("RansomDemand_Reject".Translate())
//                    {
//                        action = () =>
//                        {
//                            completed = true;
//                            var incidentParms =
//                                StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
//                            incidentParms.forced = true;
//                            incidentParms.faction = faction;
//                            incidentParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
//                            incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
//                            incidentParms.target = map;
//                            CrE_GameComponent.ChangeCrEPoints(Rand.Range(-1, -2));
//                            if (outpost)
//                            {
//                                incidentParms.points *= 0.7f;
//                            }

//                            IncidentDefOf.RaidEnemy.Worker.TryExecute(incidentParms);
//                            Find.LetterStack.RemoveLetter(this);
//                        },
//                        resolveTree = true
//                    };
//                    yield return reject;
//                    yield return Option_Postpone;
//                }
//            }
//        }

//        public override bool CanShowInLetterStack => Find.Maps.Contains(map);

//        public override void ExposeData()
//        {
//            base.ExposeData();
//            Scribe_References.Look(ref map, "map");
//            Scribe_References.Look(ref faction, "faction");
//            Scribe_Values.Look(ref fee, "fee");
//            Scribe_Values.Look(ref completed, "completed");
//        }

//    }

//    [HarmonyPatch(typeof(IncidentWorker_Extortion), "BaseChanceThisGame", MethodType.Getter)]
//    public static class Patch_IncidentWorker_Extortion_BaseChanceThisGame
//    {
//        public static void Postfix(ref float __result)
//        {
//            float chance_modifier = (float)Math.Round(Math.Exp(2 * ((1 / (1 + Mathf.Exp(-0.02f * CrE_GameComponent.CrE_Points))) - 0.5f)) - 1, 2);

//            __result += chance_modifier;
//        }
//    }

//}
