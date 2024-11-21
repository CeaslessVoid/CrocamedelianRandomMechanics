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

namespace CrocamedelianExaction
{
    public class SitePartWorker_CrEPrisonerRescue : SitePartWorker_PrisonerWillingToJoin
    {
        public static bool CrE_IsGameEnd = false;
        public static Map CrE_TempMap = null;

        public static class IncidentCrPrisonerRescue
        {
            public static bool Do(bool GameEnd = false)
            {
                if (!CrE_GameComponent.Settings.CrE_PrisonerRescue || CrE_GameComponent.GetRandomPrisoner() == null)
                    return false;

                QuestScriptDef named = DefDatabase<QuestScriptDef>.GetNamed("CrE_PrisonerRescue", true);
                float num = StorytellerUtility.DefaultThreatPointsNow(Current.Game.AnyPlayerHomeMap);

                if (!GameEnd)
                    QuestUtility.SendLetterQuestAvailable(QuestUtility.GenerateQuestAndMakeAvailable(named, num));
                else
                {
                    CrE_IsGameEnd = true;
                    QuestUtility.GenerateQuestAndMakeAvailable(named, num);
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
            if (randomPawnForSpawning == null)
                return;

            //CrE_GameComponent.RemovePawnWorld(randomPawnForSpawning);

            randomPawnForSpawning.health.AddHediff(xxx.feelingBroken, null, null, null);
            float brokenSeverityGain = Rand.Range(0.5f, 0.8f);
            randomPawnForSpawning.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken).Severity += brokenSeverityGain;

            //Util.GiveBadTraits(randomPawnForSpawning);
            randomPawnForSpawning.needs.mood.thoughts.memories.TryGainMemory(xxx.got_raped);

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

            //CrE_TempMap = part.site.Map;

            //if (CrE_IsGameEnd)
            //{
            //    Util.Msg("Game Ender Activated");
            //    CrE_IsGameEnd = false;

            //    CrE_GameComponent.EnterMapWithTemporaryEscort(CrE_TempMap);
            //}
        }

        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            CrE_GameComponent.CrE_NextPrisonRescueTIme = -2;

            Util.Msg("Prison Rescue Map Generated");

        }

        public override void PostDestroy(SitePart sitePart)
        {
            base.PostDestroy(sitePart);
            CrE_GameComponent.CrE_NextPrisonRescueTIme = -1;
            //Util.OnPostDestroyReschedule(sitePart);
        }
    }
}
