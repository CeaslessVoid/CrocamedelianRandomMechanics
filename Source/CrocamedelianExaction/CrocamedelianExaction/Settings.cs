using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            Scribe_Values.Look<bool>(ref this.CrE_PirateExtort, "CrE_PirateExtort", this.CrE_PirateExtort, true);
            Scribe_Values.Look<bool>(ref this.CrE_PirateAffectRelations, "CrE_PirateAffectRelations", this.CrE_PirateAffectRelations, true);
            Scribe_Values.Look<bool>(ref this.CrE_Male, "CrE_Male", this.CrE_Male, true);
            Scribe_Values.Look<bool>(ref this.CrE_Female, "CrE_Female", this.CrE_Female, true);

            Scribe_Values.Look<float>(ref this.CrE_ExtortLossChance, "CrE_ExtortLossChance", 0.3f, true);
            Scribe_Values.Look<float>(ref this.CrE_ExtortPregChance, "CrE_ExtortPregChance", 0.3f, true);

            Scribe_Values.Look<int>(ref this.CrE_minDaysBetweenEvents, "CrE_minDaysBetweenEvents", 10, true);
            Scribe_Values.Look<int>(ref this.CrE_maxDaysBetweenEvents, "CrE_maxDaysBetweenEvents", 25, true);

            Scribe_Values.Look<bool>(ref this.CrE_RapeTats, "CrE_RapeTats", this.CrE_RapeTats, true);
            Scribe_Values.Look<bool>(ref this.CrE_RapeTatsColonist, "CrE_RapeTatsColonist", this.CrE_RapeTatsColonist, true);
            Scribe_Values.Look<bool>(ref this.CrE_RapeTatsOthers, "CrE_RapeTatsOthers", this.CrE_RapeTatsOthers, true);

            Scribe_Values.Look<bool>(ref this.CrE_Squatters, "CrE_Squatters", this.CrE_Squatters, true);
            Scribe_Values.Look<float>(ref this.CrE_SquatterLeaveChance, "CrE_SquatterLeaveChance", 0.3f, true);

        }

        public bool CrE_PirateExtort = true;

        // Disable for male / female
        public bool CrE_Male = true;
        public bool CrE_Female = true;

        // Lose Pawn when extort
        public float CrE_ExtortLossChance = 0.3f;
        public float CrE_ExtortPregChance = 0.3f;

        // Lose Pawn Days
        public int CrE_minDaysBetweenEvents = 10; // 10
        public int CrE_maxDaysBetweenEvents = 25; // 25

        // Lose relations when high points
        public bool CrE_PirateAffectRelations = true;

        // Rape Tattoos
        public bool CrE_RapeTats = true;
        public bool CrE_RapeTatsColonist = true;
        public bool CrE_RapeTatsOthers = true;

        // Squatter
        public bool CrE_Squatters = true;
        public float CrE_SquatterLeaveChance = 0.01f;

    }


    internal class CrEMod : Mod
    {
        public CrEMod(ModContentPack content) : base(content)
        {
            this._settings = GetSettings<Settings>();
        }

        private Settings _settings;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect);

            listing_Standard.CheckboxLabeled("Allow Pirate Extortion for Pawns (Needs restart)", ref this._settings.CrE_PirateExtort);

            if (this._settings.CrE_PirateExtort)
            {


                string minDaysText = this._settings.CrE_minDaysBetweenEvents.ToString();
                listing_Standard.TextFieldNumericLabeled("Minimum Pirate Pawn Kept Day", ref this._settings.CrE_minDaysBetweenEvents, ref minDaysText, 0, 300);
                this._settings.CrE_minDaysBetweenEvents = Mathf.RoundToInt(listing_Standard.Slider(this._settings.CrE_minDaysBetweenEvents, 0f, 300f));
                listing_Standard.Gap(12f);

                string maxDaysText = this._settings.CrE_maxDaysBetweenEvents.ToString();
                listing_Standard.TextFieldNumericLabeled("Maximum Pirate Pawn Kept Day", ref this._settings.CrE_maxDaysBetweenEvents, ref maxDaysText, 1, 300);
                this._settings.CrE_maxDaysBetweenEvents = Mathf.RoundToInt(listing_Standard.Slider(this._settings.CrE_maxDaysBetweenEvents, 1f, 300f));

                if (this._settings.CrE_minDaysBetweenEvents >= this._settings.CrE_maxDaysBetweenEvents)
                {
                    this._settings.CrE_minDaysBetweenEvents = this._settings.CrE_maxDaysBetweenEvents - 1;
                }

                listing_Standard.Gap(12f);

                string CrE_ExtortLossChance = this._settings.CrE_ExtortLossChance.ToString("F2");
                listing_Standard.TextFieldNumericLabeled("Chance Pirate Keeps Pawn (x 100)", ref this._settings.CrE_ExtortLossChance, ref CrE_ExtortLossChance, 0f, 100f);
                this._settings.CrE_ExtortLossChance = listing_Standard.Slider(this._settings.CrE_ExtortLossChance, 0f, 1f);
                listing_Standard.Gap(12f);

                string CrE_ExtortPregChance = this._settings.CrE_ExtortPregChance.ToString("F2");
                listing_Standard.TextFieldNumericLabeled("Chance Pawn Gets Pregnant (x 100)", ref this._settings.CrE_ExtortPregChance, ref CrE_ExtortPregChance, 0f, 100f);
                this._settings.CrE_ExtortPregChance = listing_Standard.Slider(this._settings.CrE_ExtortPregChance, 0f, 1f);
                listing_Standard.Gap(12f);

                listing_Standard.CheckboxLabeled("Allow Male", ref this._settings.CrE_Male);
                listing_Standard.Gap(6f);

                listing_Standard.CheckboxLabeled("Allow Female", ref this._settings.CrE_Female);
                listing_Standard.Gap(6f);
            }

            listing_Standard.CheckboxLabeled("Allow Relationship Change", ref this._settings.CrE_PirateAffectRelations);
            listing_Standard.Gap(6f);

            listing_Standard.CheckboxLabeled("Apply Random Tattos On Rape", ref this._settings.CrE_RapeTats);
            listing_Standard.Gap(6f);

            if (this._settings.CrE_RapeTats)
            {
                listing_Standard.CheckboxLabeled("Own Colonist Apply Tattoos", ref this._settings.CrE_RapeTatsColonist);
                listing_Standard.Gap(6f);

                listing_Standard.CheckboxLabeled("Not Own Colonist Apply Tattos", ref this._settings.CrE_RapeTatsOthers);
                listing_Standard.Gap(6f);
            }

            listing_Standard.CheckboxLabeled("Allow Usless Colonist Event", ref this._settings.CrE_Squatters);
            listing_Standard.Gap(6f);

            if (this._settings.CrE_Squatters)
            {
                string CrE_SquatterLeaveChance = this._settings.CrE_SquatterLeaveChance.ToString("F2");
                listing_Standard.TextFieldNumericLabeled("Chance Squatters Leave Per Day (x 100)", ref this._settings.CrE_SquatterLeaveChance, ref CrE_SquatterLeaveChance, 0f, 100f);
                this._settings.CrE_SquatterLeaveChance = listing_Standard.Slider(this._settings.CrE_SquatterLeaveChance, 0f, 1f);
                listing_Standard.Gap(12f);
            }

            listing_Standard.End();

            base.DoSettingsWindowContents(inRect);
            WriteSettings();
        }

        public override string SettingsCategory()
        {
            return "Crocamedelian's Random Mechanics";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            LoadedModManager.GetMod<CrEMod>().GetSettings<Settings>().Write();
        }
    }

}
