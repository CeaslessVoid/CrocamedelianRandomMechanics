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
using UnityEngine;
using Verse;
using static System.Collections.Specialized.BitVector32;
using Verse.Noise;


//Impossible

namespace CrocamedelianExaction
{
    public class IncidentWorker_CrEPirateGuestHost : IncidentWorker
    {
        private const int TimeoutTicks = GenDate.TicksPerDay / 8;

        private Faction faction;

        public float chance_modifier = (float)Math.Round(
            Math.Exp(2 * ((1 / (1 + Mathf.Exp(-0.02f * CrE_GameComponent.CrE_Points))) - 0.5f)) - 1, 2);

        public override float BaseChanceThisGame =>
            CrE_GameComponent.Settings.CrE_PirateExtort_BaseChance
            - StorytellerUtilityPopulation.PopulationIntent
            + (chance_modifier * CrE_GameComponent.Settings.CrE_pointsMod);

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && CrE_GameComponent.GetRandomPirateFaction(out faction);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!CrE_GameComponent.GetRandomPirateFaction(out faction))
            {
                return false;
            }

            string text = "CrE_PirateGuestHost".Translate(faction.Name).CapitalizeFirst();

            var choiceLetter = (ChoiceLetter_CrGuestHost)LetterMaker.MakeLetter(def.letterLabel, text, def.letterDef);

            if (string.IsNullOrEmpty(def.letterLabel) || def.letterDef == null)
            {
                Log.Error("Missing letter label or letter definition.");
                return false;
            }

            choiceLetter.title = "CrE_PirateGuestHostLabel".Translate().CapitalizeFirst();
            choiceLetter.radioMode = false;
            choiceLetter.faction = faction;
            choiceLetter.StartTimeout(TimeoutTicks);

            Find.LetterStack.ReceiveLetter(choiceLetter);
            return true;
        }

    }

    public class ChoiceLetter_CrGuestHost : ChoiceLetter
    {
        public Faction faction;
        public override bool CanShowInLetterStack => base.CanShowInLetterStack;
        public override bool CanDismissWithRightClick => false;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (ArchivedOnly)
                {
                    yield return Option_Close;
                }
                else
                {
                    var accept = new DiaOption("CrE_RansomDemand_Accept".Translate())
                    {
                        action = () =>
                        {
                            CrE_GameComponent.ChangeCrEPoints(Rand.Range(3, 5));
                            GenerateGuests(faction);
                            Find.LetterStack.RemoveLetter(this);
                        }
                    };
                    var acceptNode = new DiaNode("CrE_AcceptedPirateGuestHost".Translate().CapitalizeFirst());
                    acceptNode.options.Add(Option_Close);
                    accept.link = acceptNode;

                    var reject = new DiaOption("CrE_RansomDemand_Reject".Translate())
                    {
                        action = () =>
                        {
                            CrE_GameComponent.ChangeCrEPoints(Rand.Range(-2, -3));
                            TriggerRaid(faction);
                            Find.LetterStack.RemoveLetter(this);
                        }
                    };
                    var rejectNode = new DiaNode("CrE_RejectedPirateGuestHost".Translate().CapitalizeFirst());
                    rejectNode.options.Add(Option_Close);
                    reject.link = rejectNode;

                    yield return accept;
                    yield return reject;
                    yield return Option_Postpone;
                }
            }
        }

        private void GenerateGuests(Faction faction)
        {

        }

        private void TriggerRaid(Faction faction)
        {
            Map map = Find.AnyPlayerHomeMap;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            parms.faction = faction;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            parms.forced = true;

            IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref faction, "faction");
        }
    }

}

//Impossible