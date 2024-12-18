using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CrocamedelianExaction
{
    // Players keep an internal colony points system. The move submissive the player is, the more bad quest will happen
    // Also works vice-versa (not yet implemented)

    public class Settings : ModSettings
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool> (ref this.CrE_PirateExtort,          "CrE_PirateExtort", this.CrE_PirateExtort, true);
            //Scribe_Values.Look<bool> (ref this.CrE_PirateAffectRelations, "CrE_PirateAffectRelations", this.CrE_PirateAffectRelations, true);
            Scribe_Values.Look<bool> (ref this.CrE_Male,                  "CrE_Male", this.CrE_Male, true);
            Scribe_Values.Look<bool> (ref this.CrE_Female,                "CrE_Female", this.CrE_Female, true);

            Scribe_Values.Look<float>(ref this.CrE_PirateExtort_BaseChance, "CrE_PirateExtort_BaseChance", 1f, true);
            Scribe_Values.Look<float>(ref this.CrE_PirateExtort_PointsMod, "CrE_PirateExtort_PointsMod", 1f, true);

            Scribe_Values.Look<float>(ref this.CrE_ExtortLossChance,      "CrE_ExtortLossChance", 0.3f, true);
            Scribe_Values.Look<float>(ref this.CrE_ExtortPregChance,      "CrE_ExtortPregChance", 0.3f, true);

            Scribe_Values.Look<float>(ref this.CrE_ExtortPawnPrice,       "CrE_ExtortPregChance", 0.3f, true);

            Scribe_Values.Look<int>(ref this.CrE_minDaysBetweenEvents, "CrE_minDaysBetweenEvents", 10, true);
            Scribe_Values.Look<int>(ref this.CrE_maxDaysBetweenEvents, "CrE_maxDaysBetweenEvents", 25, true);
            Scribe_Values.Look<float>(ref this.CrE_pointsMod,             "CrE_pointsMod", 0.2f, true);

            //Scribe_Values.Look<bool> (ref this.CrE_RapeTats,              "CrE_RapeTats", this.CrE_RapeTats, true);
            //Scribe_Values.Look<bool> (ref this.CrE_RapeTatsColonist,      "CrE_RapeTatsColonist", this.CrE_RapeTatsColonist, true);
            //Scribe_Values.Look<bool> (ref this.CrE_RapeTatsOthers,        "CrE_RapeTatsOthers", this.CrE_RapeTatsOthers, true);
            Scribe_Values.Look<bool> (ref this.CrE_OpenTattoos,           "CrE_OpenTattoos", this.CrE_OpenTattoos, true);
            Scribe_Collections.Look  (ref this.EnabledTattoos,            "EnabledTattoos", LookMode.Undefined, LookMode.Undefined);


            //Scribe_Values.Look<bool> (ref this.CrE_Squatters,             "CrE_Squatters", this.CrE_Squatters, false);
            //Scribe_Values.Look<float>(ref this.CrE_SquatterLeaveChance,   "CrE_SquatterLeaveChance", 0.3f, true);

            //Scribe_Values.Look<bool> (ref this.CrE_Respect_Active,        "CrE_Respect_Active", this.CrE_Respect_Active, true);

            Scribe_Values.Look<bool> (ref this.CrE_PrisonerRescue,        "CrE_PrisonerRescue", this.CrE_PrisonerRescue, true);
            //Scribe_Values.Look<int>(ref this.CrE_minDaysBetweenRescue,    "CrE_minDaysBetweenRescue", 20, true);
            //Scribe_Values.Look<int>(ref this.CrE_maxDaysBetweenRescue,    "CrE_maxDaysBetweenRescue", 50, true);

            //Scribe_Values.Look<bool>(ref this.CrE_forceRescue,            "CrE_froceRescue", this.CrE_forceRescue, false);
            //Scribe_Values.Look<int>(ref this.CrE_forceRescueDays,         "CrE_forceRescueDays", 5, true);

        }

        // Allow Pirate Extort Event
        public bool     CrE_PirateExtort =          true;

        public float    CrE_PirateExtort_BaseChance = 1;
        public float    CrE_PirateExtort_PointsMod = 1;

        // Disable for male / female
        public bool     CrE_Male =                  true;
        public bool     CrE_Female =                true;

        // Lose Pawn when extort
        public float    CrE_ExtortLossChance =      0.3f;
        public float    CrE_ExtortPregChance =      0.3f;

        // Money

        public float    CrE_ExtortPawnPrice =       0.5f;

        // Lose Pawn Days
        public float    CrE_pointsMod = 0.2f;
        public int      CrE_minDaysBetweenEvents = 10;
        public int      CrE_maxDaysBetweenEvents = 10;

        // Lose relations when high points
        //public bool     CrE_PirateAffectRelations = true;

        // Rape Tattoos
        //public bool     CrE_RapeTats =              true;
        //public bool     CrE_RapeTatsColonist =      true;
        //public bool     CrE_RapeTatsOthers =        true;
        public bool     CrE_OpenTattoos =           false;

        public Dictionary<string, bool> EnabledTattoos = new Dictionary<string, bool>();

        // Respect (Not Finished)
        //public bool     CrE_Respect_Active =        false;

        // Prisoner Rescue Quest
        public bool     CrE_PrisonerRescue =       true;

        //public int      CrE_minDaysBetweenRescue =  20;
        //public int      CrE_maxDaysBetweenRescue =  50;

        // Force Rescue Quest
        //public bool     CrE_forceRescue =           true;
        //public int      CrE_forceRescueDays =       5;

    }


    internal class CrE_Mod : Mod
    {
        public CrE_Mod(ModContentPack content) : base(content)
        {
            this._settings = GetSettings<Settings>();
        }

        private Settings _settings;

        private Vector2 scrollPosition = Vector2.zero;
        public override void DoSettingsWindowContents(Rect inRect)
        {
            float contentHeight = 500f;

            if (this._settings.CrE_PirateExtort)
                contentHeight += 200f;
            if (this._settings.CrE_OpenTattoos)
                contentHeight += (DefDatabase<TattooDef>.AllDefsListForReading.Count * 30f);
            if (this._settings.CrE_PrisonerRescue)
                contentHeight += 200f;

            //float contentHeight = 300f + (DefDatabase<TattooDef>.AllDefsListForReading.Count * 30f);
            Rect viewRect = new Rect(0, 0, inRect.width - 20f, contentHeight);

            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);

            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(viewRect);


            listing_Standard.ColumnWidth = viewRect.width - 30f;


            listing_Standard.CheckboxLabeled("Allow Pirate Extortion for Pawns", ref this._settings.CrE_PirateExtort);

            if (this._settings.CrE_PirateExtort)
            {
                listing_Standard.Label("Do note that 10 is not 10%, but 10x more possible than event with 1. Chance is also modified by storyteller and population");
                string baseChanceText = this._settings.CrE_PirateExtort_BaseChance.ToString();
                listing_Standard.TextFieldNumericLabeled("Base chance of event", ref this._settings.CrE_PirateExtort_BaseChance, ref baseChanceText, 0.1f, 100f);
                this._settings.CrE_PirateExtort_BaseChance = listing_Standard.Slider(this._settings.CrE_PirateExtort_BaseChance, 0.1f, 100f);
                listing_Standard.Gap(8f);

                listing_Standard.Label("This modifies how effective each 'submissive' point is at increasing chance, higher value = chance scales faster");
                string scaleChanceText = this._settings.CrE_PirateExtort_PointsMod.ToString();
                listing_Standard.TextFieldNumericLabeled("Modifier on scaling chance", ref this._settings.CrE_PirateExtort_PointsMod, ref scaleChanceText, 0, 100);
                this._settings.CrE_PirateExtort_PointsMod = listing_Standard.Slider(this._settings.CrE_PirateExtort_PointsMod, 0f, 100f);
                listing_Standard.Gap(8f);

                listing_Standard.Label("The day kept is ~+ 0.5 because of the check");

                string minDaysText = this._settings.CrE_minDaysBetweenEvents.ToString();
                listing_Standard.TextFieldNumericLabeled("Minimum Pirate Pawn Kept Day", ref this._settings.CrE_minDaysBetweenEvents, ref minDaysText, 0, 300);
                this._settings.CrE_minDaysBetweenEvents = Mathf.RoundToInt(listing_Standard.Slider(this._settings.CrE_minDaysBetweenEvents, 0f, 300f));
                listing_Standard.Gap(8f);

                string maxDaysText = this._settings.CrE_maxDaysBetweenEvents.ToString();
                listing_Standard.TextFieldNumericLabeled("Maximum Pirate Pawn Kept Day", ref this._settings.CrE_maxDaysBetweenEvents, ref maxDaysText, 1, 300);
                this._settings.CrE_maxDaysBetweenEvents = Mathf.RoundToInt(listing_Standard.Slider(this._settings.CrE_maxDaysBetweenEvents, 1f, 300f));
                listing_Standard.Gap(8f);

                listing_Standard.Label("This modifies how effective each 'submissive' point is at increasing duration, higher value = chance scales faster");
                string modDaysText = this._settings.CrE_pointsMod.ToString();
                listing_Standard.TextFieldNumericLabeled("Modifier on scaling duration", ref this._settings.CrE_pointsMod, ref modDaysText, 0f, 10);
                this._settings.CrE_pointsMod = Mathf.RoundToInt(listing_Standard.Slider(this._settings.CrE_pointsMod, 0f, 10f));
                listing_Standard.Gap(8f);

                string CrE_ExtortLossChance = this._settings.CrE_ExtortLossChance.ToString("F2");
                listing_Standard.TextFieldNumericLabeled("Chance Pirate Keeps Pawn (x 100)", ref this._settings.CrE_ExtortLossChance, ref CrE_ExtortLossChance, 0f, 100f);
                this._settings.CrE_ExtortLossChance = listing_Standard.Slider(this._settings.CrE_ExtortLossChance, 0f, 1f);
                listing_Standard.Gap(8f);

                string CrE_ExtortPregChance = this._settings.CrE_ExtortPregChance.ToString("F2");
                listing_Standard.TextFieldNumericLabeled("Chance Pawn Gets Pregnant (x 100)", ref this._settings.CrE_ExtortPregChance, ref CrE_ExtortPregChance, 0f, 100f);
                this._settings.CrE_ExtortPregChance = listing_Standard.Slider(this._settings.CrE_ExtortPregChance, 0f, 1f);
                listing_Standard.Gap(8f);

                string CrE_ExtortPawnPrice = this._settings.CrE_ExtortPawnPrice.ToString("F2");
                listing_Standard.TextFieldNumericLabeled("Price Multiplier Of Pawn (For Bribe)", ref this._settings.CrE_ExtortPawnPrice, ref CrE_ExtortPawnPrice, 0f, 2f);
                this._settings.CrE_ExtortPawnPrice = listing_Standard.Slider(this._settings.CrE_ExtortPawnPrice, 0f, 2f);
                listing_Standard.Gap(8f);

                listing_Standard.CheckboxLabeled("Allow Male", ref this._settings.CrE_Male);
                listing_Standard.Gap(6f);

                listing_Standard.CheckboxLabeled("Allow Female", ref this._settings.CrE_Female);
                listing_Standard.Gap(6f);

                if (this._settings.CrE_minDaysBetweenEvents >= this._settings.CrE_maxDaysBetweenEvents)
                {
                    this._settings.CrE_minDaysBetweenEvents = this._settings.CrE_maxDaysBetweenEvents - 1;
                }

                if (this._settings.CrE_minDaysBetweenEvents < 1)
                {
                    this._settings.CrE_minDaysBetweenEvents = 1;
                }
            }

            //listing_Standard.CheckboxLabeled("Allow Relationship Change", ref this._settings.CrE_PirateAffectRelations);
            //listing_Standard.Gap(6f);

            listing_Standard.Label("---------------------------------------------------------------------------------------------------------------------------------------------------------");

            listing_Standard.CheckboxLabeled("Allow Prisoner Rescue Quest", ref this._settings.CrE_PrisonerRescue);
            listing_Standard.Gap(6f);

            if (this._settings.CrE_PrisonerRescue)
            {

                listing_Standard.Label("Do note that if the kidnapped pawns are used by anything else (e.g. for another event of given to the faction) they might not appear");
                listing_Standard.Label("Making kidnapped pawns exclusive (so they can only be used by this event) had many issues. Sorry");

                //string minDaysRescueText = this._settings.CrE_minDaysBetweenRescue.ToString();
                //listing_Standard.TextFieldNumericLabeled("Minimum Days Between Prisoner Rescue Quest", ref this._settings.CrE_minDaysBetweenRescue, ref minDaysRescueText, 0, 300);
                //this._settings.CrE_minDaysBetweenRescue = Mathf.RoundToInt(listing_Standard.Slider(this._settings.CrE_minDaysBetweenRescue, 0f, 300f));
                //listing_Standard.Gap(8f);

                //string maxDaysRescueText = this._settings.CrE_maxDaysBetweenRescue.ToString();
                //listing_Standard.TextFieldNumericLabeled("Maximum Days Between Prisoner Rescue Quest", ref this._settings.CrE_maxDaysBetweenRescue, ref maxDaysRescueText, 1, 300);
                //this._settings.CrE_maxDaysBetweenRescue = Mathf.RoundToInt(listing_Standard.Slider(this._settings.CrE_maxDaysBetweenRescue, 1f, 300f));
                //listing_Standard.Gap(8f);

                //if (this._settings.CrE_minDaysBetweenRescue >= this._settings.CrE_maxDaysBetweenRescue)
                //{
                //    this._settings.CrE_minDaysBetweenRescue = this._settings.CrE_maxDaysBetweenRescue - 1;
                //}

                //if (this._settings.CrE_PirateExtort)
                //{
                //    listing_Standard.CheckboxLabeled("Force Rescue Quest If Pawn Is Lost (From Pirate Extortion)", ref this._settings.CrE_forceRescue);
                //    listing_Standard.Gap(6f);

                //    if (this._settings.CrE_forceRescue)
                //    {
                //        string forceRescueDays = this._settings.CrE_forceRescueDays.ToString();
                //        listing_Standard.TextFieldNumericLabeled("Force After (x) days", ref this._settings.CrE_forceRescueDays, ref forceRescueDays, 1, 300);
                //        this._settings.CrE_forceRescueDays = Mathf.RoundToInt(listing_Standard.Slider(this._settings.CrE_forceRescueDays, 1f, 300f));
                //        listing_Standard.Gap(8f);
                //    }
                //}

                listing_Standard.CheckboxLabeled("Open Possible Tattoo", ref this._settings.CrE_OpenTattoos);
                listing_Standard.Gap(6f);

                if (this._settings.CrE_OpenTattoos)
                {
                    listing_Standard.Label("Toggle tattoos:");

                    if (this._settings.EnabledTattoos == null)
                    {
                        this._settings.EnabledTattoos = new Dictionary<string, bool>();
                    }

                    foreach (var tattoo in DefDatabase<TattooDef>.AllDefsListForReading)
                    {
                        if (!this._settings.EnabledTattoos.ContainsKey(tattoo.defName))
                        {
                            this._settings.EnabledTattoos[tattoo.defName] = true;

                            if (tattoo.defName == "NoTattoo_Face" || tattoo.defName == "NoTattoo_Body")
                            {
                                this._settings.EnabledTattoos[tattoo.defName] = false;
                            }
                        }

                        bool isEnabled = this._settings.EnabledTattoos[tattoo.defName];
                        listing_Standard.CheckboxLabeled(tattoo.label, ref isEnabled);

                        this._settings.EnabledTattoos[tattoo.defName] = isEnabled;

                    }

                }
            }

            listing_Standard.End();


            base.DoSettingsWindowContents(inRect);
            WriteSettings();
            Widgets.EndScrollView();
        }

        public override string SettingsCategory()
        {
            return "Crocamedelian's Random Mechanics";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            LoadedModManager.GetMod<CrE_Mod>().GetSettings<Settings>().Write();
        }
    }

}
