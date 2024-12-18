using HarmonyLib;
using LudeonTK;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld;
using rjw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Grammar;
using UnityEngine.Diagnostics;
using System.Collections;
using Verse.Noise;

namespace CrocamedelianExaction
{
    public class SitePartWorker_CrEPrisonerRescue : SitePartWorker_PrisonerWillingToJoin
    {
        public static Site site = null;

        public static class IncidentCrPrisonerRescue
        {
            public static bool Do(bool GameEnd = false)
            {
                if (!CrE_GameComponent.Settings.CrE_PrisonerRescue || CrE_GameComponent.GetRandomPrisoner() == null)
                    return false;

                QuestScriptDef named = DefDatabase<QuestScriptDef>.GetNamed("CrE_PrisonerRescue", true);
                float num = StorytellerUtility.DefaultThreatPointsNow(Current.Game.AnyPlayerHomeMap);


                Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(named, num);

                QuestUtility.SendLetterQuestAvailable(quest);

                if (site != null)
                {
                    Map map = MapGen.GetOrGenerateMap(
                        site.Tile,
                        Find.World.info.initialMapSize,
                        WorldObjectDefOf.Settlement
                    );

                    if (map != null)
                    {
                        Current.Game.CurrentMap = map;

                        CameraJumper.TryJump(map.Center, map);

                    }
                    else
                    {
                        Util.Error("Failed to generate or load the map for the quest site.");
                    }
                }

                return true;
            }

        }

        public SitePartWorker_CrEPrisonerRescue()
        {
            SitePartWorker_CrEPrisonerRescue.Settings = LoadedModManager.GetMod<Mod>().GetSettings<Settings>();
        }
        public static Settings Settings { get; private set; }

        public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
        {

            Util.SitePartWorker_Base_Notify_GeneratedByQuestGen(part, outExtraDescriptionRules, outExtraDescriptionConstants);
            Pawn randomPawnForSpawning = CrE_GameComponent.GetRandomPrisoner();

            //CrE_GameComponent.RemovePawnWorld(randomPawnForSpawning);

            //randomPawnForSpawning.health.AddHediff(xxx.feelingBroken, null, null, null);
            //float brokenSeverityGain = Rand.Range(0.5f, 0.8f);
            //randomPawnForSpawning.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken).Severity += brokenSeverityGain;
            //randomPawnForSpawning.needs.mood.thoughts.memories.TryGainMemory(xxx.got_raped);
            randomPawnForSpawning.guest.SetGuestStatus(part.site.Faction, RimWorld.GuestStatus.Prisoner);

            Util.DressPawnIfCold(randomPawnForSpawning, part.site.Tile);
            Util.HealPawn(randomPawnForSpawning);

            part.things = new ThingOwner<Pawn>(part, true, Verse.LookMode.Deep);
            part.things.TryAdd(randomPawnForSpawning, true);

            string text = "";

            PawnRelationUtility.Notify_PawnsSeenByPlayer(Gen.YieldSingle<Pawn>(randomPawnForSpawning), out text, true, false);

            outExtraDescriptionRules.AddRange(GrammarUtility.RulesForPawn("prisoner", randomPawnForSpawning, outExtraDescriptionConstants, true, true));
            string text2;
            if (!GenText.NullOrEmpty(text))
            {
                text2 = "\n\n" + TranslatorFormattedStringExtensions.Translate("PawnHasTheseRelationshipsWithColonists", randomPawnForSpawning.LabelShort, randomPawnForSpawning) + "\n\n" + text;
            }
            else
            {
                text2 = "";
            }
            outExtraDescriptionRules.Add(new Rule_String("prisonerFullRelationInfo", text2));

            site = part.site;
        }

        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
        }

        public override void PostDestroy(SitePart sitePart)
        {
            base.PostDestroy(sitePart);
        }
    }
}
