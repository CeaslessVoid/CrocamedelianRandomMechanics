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
using static CrocamedelianExaction.SitePartWorker_CrEPrisonerRescue;
using RimWorld.Planet;

namespace CrocamedelianExaction
{
    // Players keep an internal colony points system. The move submissive the player is, the more bad quest will happen
    // Also works vice-versa (not yet implemented)

    internal class CrE_GameComponent : GameComponent
    {
        // Load Settings
        public static Settings Settings { get; private set; }
        public CrE_GameComponent(Game game)
        {
            Settings = LoadedModManager.GetMod<CrE_Mod>().GetSettings<Settings>();
        }

        public static void InitOnNewGame()
        {
            CrE_Points = 0;
            CrE_Pawn_Return_Time = -1;
            CrE_Pirate = null;
            HasPawnOut = false;
            CurrentCrEPawn = null;
            CrE_NextPrisonRescueTIme = -1;

            if (CapturedPawnsQueue == null)
                CapturedPawnsQueue = new List<Pawn>();


        }

        public static void InitOnLoad()
        {

            if (Settings.EnabledTattoos == null)
            {
                Settings.EnabledTattoos = new Dictionary<string, bool>();
            }

            foreach (var tattoo in DefDatabase<TattooDef>.AllDefsListForReading)
            {
                if (!Settings.EnabledTattoos.ContainsKey(tattoo.defName))
                {
                    Settings.EnabledTattoos[tattoo.defName] = true;

                    if (tattoo.defName == "NoTattoo_Face" || tattoo.defName == "NoTattoo_Body")
                    {
                        Settings.EnabledTattoos[tattoo.defName] = false;
                    }
                }
            }

            if (CapturedPawnsQueue == null)
                CapturedPawnsQueue = new List<Pawn>();

            CapturedPawnsQueue.Clear();
        }

        public override void GameComponentTick() // Every day
        {
            base.GameComponentTick();

            if (GenTicks.IsTickInterval(60000))
                PerformDailyPawnCheck();

        }

        public static void MakePawnSlave(Pawn pawn)
        {
            var pirateFactions = Find.FactionManager.AllFactionsListForReading
            .Where(faction => faction.def.pawnGroupMakers != null
                           && faction.def.pawnGroupMakers.Any(group => group.options.Any(opt => opt.kind.isFighter))
                           && faction.def.permanentEnemy
                           && !(faction.def == FactionDefOf.Mechanoid)
                           && !(faction.def == FactionDefOf.Insect))
            .ToList();

            if (pirateFactions.Count == 0)
            {
                Log.Warning("No pirate factions available to kidnap this pawn.");
                return;
            }

            Faction pirateFaction = pirateFactions.RandomElement();

            if (CrE_Pirate != null && CurrentCrEPawn == pawn)
            {
                pirateFaction = CrE_Pirate.Faction;
                CrE_Pirate = null;
            }


            pawn.SetFaction(Faction.OfPlayer);

            if (!Find.WorldPawns.Contains(pawn))
            {
                Find.WorldPawns.PassToWorld(pawn);
                if (!Find.WorldPawns.Contains(pawn))
                {
                    Log.Error("WorldPawns discarded kidnapped pawn.");
                    return;
                }
            }

            pirateFaction.kidnapped.KidnappedPawnsListForReading.Add(pawn);

            Find.GameEnder.CheckOrUpdateGameOver();
        }

        public static void DoPirateTakePawn()
        {
            int minDays = Settings.CrE_minDaysBetweenEvents * 60000;
            int maxDays = Settings.CrE_maxDaysBetweenEvents * 60000;

            CrE_Pawn_Return_Time = Find.TickManager.TicksGame + UnityEngine.Random.Range(minDays, maxDays);
        }

        public static void TransferCapturedPawnsToWorldPawns()
        {
            Util.Msg("Moved Kidnapped Pawns");

            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                List<Pawn> kidnappedPawns = faction.kidnapped.KidnappedPawnsListForReading;

                foreach (Pawn pawn in kidnappedPawns.ToList())
                {
                    faction.kidnapped.RemoveKidnappedPawn(pawn);

                    pawn.SetFaction(faction);

                }
            }
        }

        private void PerformDailyPawnCheck()
        {

            // Remove the pawn from worldpawns so they wont be used
            if (CrE_GameComponent.CurrentCrEPawn != null && Find.WorldPawns.Contains(CrE_GameComponent.CurrentCrEPawn))
            {
                Find.WorldPawns.RemovePawn(CurrentCrEPawn);

                if (CurrentCrEPawn.gender == Gender.Female && Rand.Chance(Settings.CrE_ExtortPregChance))
                {
                    Util.Msg("Tried To Give Preg");
                    PregnancyHelper.AddPregnancyHediff(CurrentCrEPawn, CrE_Pirate);
                }

            }

            if (Find.TickManager.TicksGame >= CrE_GameComponent.CrE_Pawn_Return_Time && CrE_GameComponent.CrE_Pawn_Return_Time != -1 && CrE_GameComponent.CurrentCrEPawn != null)
            {
                // All these actions will set the timer back down
                CrE_GameComponent.CrE_Pawn_Return_Time = -1;
                HasPawnOut = false;

                if (Rand.Chance(Settings.CrE_ExtortLossChance))
                {
                    CrE_PiratePawn_NoReturn.Do();
                }
                else
                {
                    CrE_PiratePawn_Return.Do();
                }

                return;
            }

            // Forces events to happen -----------------------------------------------------------------------------------------------
            float chance = 0.05f + (float)Math.Round(Math.Exp(2 * ((1 / (1 + Mathf.Exp(-0.02f * CrE_GameComponent.CrE_Points))) - 0.5f)) - 1, 2);

            if (Settings.CrE_PirateExtort && !HasPawnOut && CrE_Pawn_Return_Time == -1 && Rand.Chance(Mathf.Clamp(chance, 0.0f, 1.0f)) && Find.TickManager.TicksGame >= 60000 * 30)
            {
                IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamed("CrE_PiratePawn_Extort", true);

                var incidentParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.AnyPlayerHomeMap);
                incidentParms.forced = true;
                incidentParms.target = Find.AnyPlayerHomeMap;

                bool result = incidentDef.Worker.TryExecute(incidentParms);

                return;
            }

            float chance1 = 0.25f + (float)Math.Round(Math.Exp(2 * ((1 / (1 + Mathf.Exp(-0.02f * Mathf.Abs(CrE_GameComponent.CrE_Points)))) - 0.5f)) - 1, 2);

            if (CrE_Points >= 10 && Find.TickManager.TicksGame >= CrELoseRelationsCooldown && Rand.Chance(Mathf.Clamp(chance1, 0.0f, 1.0f)))
            {
                IncidentParms parms = new IncidentParms
                {
                    target = Find.AnyPlayerHomeMap
                };

                IncidentDef def = IncidentDef.Named("CrE_FactionRelationsDeterioration");
                def.Worker.TryExecute(parms);

                Util.Msg(CrELoseRelationsCooldown);
            }

            GetRandomPrisoner();

            if (Settings.CrE_PrisonerRescue && CrE_NextPrisonRescueTIme > 0 && Find.TickManager.TicksGame >= CrE_NextPrisonRescueTIme && CrE_GameComponent.GetRandomPrisoner() != null)
            {
                IncidentCrPrisonerRescue.Do();
            }

        }

        // Change points
        public static void ChangeCrEPoints(int points)
        {
            CrE_GameComponent.CrE_Points += points; // Just use negative numbers for decrease
        }

        //public static List<Pawn> CapturedPawnsQue = new List<Pawn>();
        public static int CrE_Pawn_Return_Time = -1; // Time to return
        public static Pawn CurrentCrEPawn = null;
        public static Pawn CrE_Pirate = null;

        public static int CrE_Points; // CrE Points
        public static bool HasPawnOut; // If a pawn has already been taken

        public static int CrELoseRelationsCooldown; // Cooldown for the lose relationship

        public static int CrE_NextPrisonRescueTIme = -1;

        public static List<Pawn> CapturedPawnsQueue = new List<Pawn>();

        public bool ContinueAsCapturedPawn = false; //Bugfest

        public static Pawn GetRandomPawnForEvent()
        {
            List<Pawn> allPawns = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists.ToList();

            // Filter based on settings
            IEnumerable<Pawn> validPawns = allPawns.Where(pawn =>
                (Settings.CrE_Male || pawn.gender != Gender.Male)
             && (Settings.CrE_Female || pawn.gender != Gender.Female));

            if (!validPawns.Any())
            {
                return null;
            }

            return validPawns.RandomElement();
        }


        public static void GetNextPrisonerTime(bool forced = false)
        {
            if (GetRandomPrisoner() != null)
            {
                if (CrE_NextPrisonRescueTIme == -1 || (Find.TickManager.TicksGame >= CrE_NextPrisonRescueTIme && CrE_NextPrisonRescueTIme >= 0))
                {
                    int minDays = Settings.CrE_minDaysBetweenRescue * 60000;
                    int maxDays = Settings.CrE_maxDaysBetweenRescue * 60000;

                    CrE_NextPrisonRescueTIme = Find.TickManager.TicksGame + UnityEngine.Random.Range(minDays, maxDays);
                }

                if (forced)
                {
                    CrE_NextPrisonRescueTIme = Find.TickManager.TicksGame + (Settings.CrE_forceRescueDays * 60000);
                }

                return;
            }

            CrE_NextPrisonRescueTIme = -1;
        }

        public static Pawn GetRandomPrisoner()
        {
            if (CapturedPawnsQueue != null && CapturedPawnsQueue.Count > 0)
            {
                Pawn pawnFromQueue = CapturedPawnsQueue.RandomElement();

                if (pawnFromQueue != null)
                {
                    CapturedPawnsQueue.Remove(pawnFromQueue);
                    return pawnFromQueue;
                }
            }

            List<Pawn> kidnappedPawns = new List<Pawn>();

            foreach (Faction faction in Find.FactionManager.AllFactions)
            {
                List<Pawn> factionKidnappedPawns = faction.kidnapped.KidnappedPawnsListForReading;

                if (factionKidnappedPawns != null && factionKidnappedPawns.Count > 0)
                {
                    kidnappedPawns.AddRange(factionKidnappedPawns);
                }
            }

            if (kidnappedPawns.Count == 0)
            {
                Util.Msg("No Kidnapped Pawns");
                return null;
            }

            return kidnappedPawns.RandomElement();
        }

        public static void RemovePawnWorld(Pawn pawn)
        {
            Find.WorldPawns.RemovePawn(pawn);
        }

        public static bool isValidPawn(Pawn pawn)
        {
            return (Settings.CrE_Male   || pawn.gender != Gender.Male) 
                && (Settings.CrE_Female || pawn.gender != Gender.Female);
        }

        public static void ResetCrELoseRelationsCooldown()
        {
            int minCooldown = 9;
            int maxCooldown = 35;

            int scaledCooldown = (int)(maxCooldown - ((Mathf.Abs(CrE_GameComponent.CrE_Points) / 100f) * (maxCooldown - minCooldown)));
            CrELoseRelationsCooldown = Find.TickManager.TicksGame + (Mathf.Clamp(scaledCooldown, minCooldown, maxCooldown) * 60000);
        }

        public static void RapeTattoo(Pawn pawn)
        {
            if (pawn == null || DefDatabase<TattooDef>.AllDefsListForReading.Count == 0)
            {
                Log.Warning("Either no tattoos are defined, or the pawn is null.");
                return;
            }

            List<TattooDef> enabledTattoos = DefDatabase<TattooDef>.AllDefsListForReading
                                            .Where(t => CrE_GameComponent.Settings.EnabledTattoos.TryGetValue(t.defName, out bool isEnabled) && isEnabled)
                                            .ToList();

            if (enabledTattoos.Count == 0)
            {
                Log.Warning("No enabled tattoos");
                return;
            }

            TattooDef selectedTattoo = enabledTattoos.OrderBy(t => Rand.Value).FirstOrDefault();

            if (selectedTattoo != null)
            {
                if (selectedTattoo.tattooType == TattooType.Body)
                {
                    pawn.style.BodyTattoo = selectedTattoo;
                }
                else
                {
                    pawn.style.FaceTattoo = selectedTattoo;

                }

                pawn.Drawer.renderer.SetAllGraphicsDirty();
                PortraitsCache.SetDirty(pawn);
                GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);

                Log.Message($"{pawn.Name} has been assigned the {selectedTattoo.label} tattoo.");
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref CrE_Points, "CrE_Points", 0, true);
            Scribe_Values.Look<int>(ref CrELoseRelationsCooldown, "CrELoseRelationsCooldown", 0, true);
            Scribe_Values.Look<bool>(ref HasPawnOut, "has_pawn_out", false, true);

            //Scribe_Collections.Look<Pawn>(ref CrE_GameComponent.CapturedPawnsQue, "CapturedPawnsQue", LookMode.Deep, Array.Empty<object>());
            Scribe_References.Look(ref CurrentCrEPawn, "CurrentCrEPawn");
            Scribe_Values.Look<int>(ref CrE_Pawn_Return_Time, "CrE_Pawn_Return_Time", -1, true);
            Scribe_References.Look(ref CrE_Pirate, "CrE_Pirate");

            Scribe_Values.Look<int>(ref CrE_NextPrisonRescueTIme, "CrE_NextPrisonRescueTIme", -1, true);

            Scribe_Collections.Look(ref CapturedPawnsQueue, "CapturedPawnsQueue", LookMode.Reference);

        }

    }


}
