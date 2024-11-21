using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;
using UnityEngine;
using rjw;
using HarmonyMod;
using CrocamedelianExaction;
using RimWorld.Planet;

namespace crocamedelianexaction
{
    public class CrE_ChoiceLetter_GameEnded : ChoiceLetter
    {
        public override bool CanDismissWithRightClick
        {
            get
            {
                return false;
            }
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (base.ArchivedOnly)
                {
                    yield return base.Option_Close;
                }
                else
                {
                    if (!Find.GameEnder.CanSpawnNewWanderers())
                    {
                        yield return new DiaOption("GameOverKeepWatching".Translate())
                        {
                            resolveTree = true
                        };
                    }
                    else
                    {
                        yield return new DiaOption("GameOverKeepWatchingForNow".Translate())
                        {
                            resolveTree = true
                        };


                        //DiaOption diaOption3 = new DiaOption("CrENewColonyPrisoner".Translate());
                        //diaOption3.action = delegate ()
                        //{
                        //    CrE_GameComponent gameComponent = Current.Game.GetComponent<CrE_GameComponent>();
                        //    gameComponent.ContinueAsCapturedPawn = true;

                        //    QuestScriptDef named = DefDatabase<QuestScriptDef>.GetNamed("CrE_PrisonerRescue", true);
                        //    float num2 = StorytellerUtility.DefaultThreatPointsNow(Current.Game.AnyPlayerHomeMap);

                        //    QuestUtility.GenerateQuestAndMakeAvailable(named, num2);

                        //};
                        //diaOption3.resolveTree = true;
                        //diaOption3.disabled = (CrE_GameComponent.GetRandomPrisoner() == null);
                        //diaOption3.disabledReason = ("CrENoPrisoners".Translate());
                        //yield return diaOption3;



                        float num = (float)(20000 - (GenTicks.TicksGame - this.arrivalTick)) / 2500f;
                        DiaOption diaOption = new DiaOption((num > 0f) ? "GameOverCreateNewWanderersWait".Translate(Math.Ceiling((double)num)) : "GameOverCreateNewWanderers".Translate());
                        diaOption.action = delegate ()
                        {
                            Find.WindowStack.Add(new Dialog_ChooseNewWanderers());
                        };
                        diaOption.resolveTree = true;
                        diaOption.disabled = (num > 0f || Find.AnyPlayerHomeMap == null);
                        diaOption.disabledReason = ((Find.AnyPlayerHomeMap == null) ? "NoColony".Translate() : null);
                        yield return diaOption;
                    }
                    DiaOption diaOption2 = new DiaOption("GameOverMainMenu".Translate());
                    diaOption2.action = delegate ()
                    {
                        GenScene.GoToMainMenu();
                    };
                    diaOption2.resolveTree = true;
                    yield return diaOption2;
                }
                yield break;
            }
        }
    }
}
