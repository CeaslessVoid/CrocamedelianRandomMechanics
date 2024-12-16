﻿using HarmonyLib;
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

            CrE_NextPrisonRescueTIme = -1;

            if (PirateExtortPawn == null)
                PirateExtortPawn = new List<PirateExtortPawnData>();

            if (FactionRaidCooldowns == null)
                FactionRaidCooldowns = new Dictionary<Faction, int>();
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

            if (PirateExtortPawn == null)
                PirateExtortPawn = new List<PirateExtortPawnData>();

            if (FactionRaidCooldowns == null)
                FactionRaidCooldowns = new Dictionary<Faction, int>();

        }

        public override void GameComponentTick() // Every day
        {
            base.GameComponentTick();

            if (GenTicks.IsTickInterval(60000))
                PerformDailyPawnCheck();

            if (GenTicks.IsTickInterval(3000))
                PerformCheck();

        }

        public static Settlement GetRandomPirateSettlement()
        {
            var pirateSettlements = Find.WorldObjects.Settlements
                .Where(settlement => settlement.Faction != null
                                  && settlement.Faction.def.pawnGroupMakers != null
                                  && settlement.Faction.def.pawnGroupMakers.Any(group => group.options.Any(opt => opt.kind.isFighter))
                                  && settlement.Faction.def.permanentEnemy
                                  && !(settlement.Faction.def == FactionDefOf.Mechanoid)
                                  && !(settlement.Faction.def == FactionDefOf.Insect))
                .ToList();

            return pirateSettlements.RandomElementWithFallback();
        }

        public static void CleanupFactionRaidCooldowns()
        {
            var expiredFactions = FactionRaidCooldowns
                .Where(kvp => Find.TickManager.TicksGame >= kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var faction in expiredFactions)
            {
                FactionRaidCooldowns.Remove(faction);
            }
        }

        public static void MakePawnSlave(Pawn pawn, Faction faction)
        {
            pawn.SetFaction(Faction.OfPlayer);

            faction.kidnapped.KidnappedPawnsListForReading.Add(pawn);

            Find.GameEnder.CheckOrUpdateGameOver();
        }

        

        public static void TransferCapturedPawnsToWorldPawns()
        {
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

        private static void ReturnOrKeepPawn(Pawn pawn, Faction faction)
        {
            //faction.kidnapped.KidnappedPawnsListForReading.Remove(pawn);

            if (Rand.Chance(CrE_GameComponent.Settings.CrE_ExtortLossChance))
            {
                CrE_PiratePawn_NoReturn.Initialize(pawn);
                CrE_PiratePawn_NoPawn.Do();
            }
            else
            {
                CrE_PiratePawn_Return.Initialize(pawn, faction);
                CrE_PiratePawn_Return.Do();
            }
        }
        private void PerformCheck()
        {
            CleanupFactionRaidCooldowns();

            CheckPawnForExtort();
        }

        private void CheckPawnForExtort()
        {
            for (int i = PirateExtortPawn.Count - 1; i >= 0; i--)
            {
                var entry = PirateExtortPawn[i];
                Pawn pawn = entry.Pawn;
                int returnTime = entry.TargetTick;
                int expiryTick = entry.TimeoutTick;
                Faction faction = entry.Faction;

                if (Find.TickManager.TicksGame > returnTime)
                {
                    ReturnOrKeepPawn(pawn, faction);
                    PirateExtortPawn.RemoveAt(i);
                    continue;
                }

                if (expiryTick != 0)
                {
                    if (Find.TickManager.TicksGame > expiryTick && !Find.WorldPawns.Contains(pawn))
                    {
                        CrE_PiratePawn_NoPawn.Initialize(pawn, faction);
                        CrE_PiratePawn_NoPawn.Do();

                        entry.TimeoutTick = 0;
                    }
                    else if (Find.TickManager.TicksGame > expiryTick && Find.WorldPawns.Contains(pawn))
                    {
                        MakePawnSlave(pawn, faction);
                    }

                    entry.TimeoutTick = 0;
                }

            }
        }

        private void PerformDailyPawnCheck()
        {

            //float chance1 = 0.25f + (float)Math.Round(Math.Exp(2 * ((1 / (1 + Mathf.Exp(-0.02f * Mathf.Abs(CrE_GameComponent.CrE_Points)))) - 0.5f)) - 1, 2);

            //if (CrE_Points >= 10 && Find.TickManager.TicksGame >= CrELoseRelationsCooldown && Rand.Chance(Mathf.Clamp(chance1, 0.0f, 1.0f)))
            //{
            //    IncidentParms parms = new IncidentParms
            //    {
            //        target = Find.AnyPlayerHomeMap
            //    };

            //    IncidentDef def = IncidentDef.Named("CrE_FactionRelationsDeterioration");
            //    def.Worker.TryExecute(parms);

            //    Util.Msg(CrELoseRelationsCooldown);
            //}

            //GetRandomPrisoner();

            //if (Settings.CrE_PrisonerRescue && CrE_NextPrisonRescueTIme > 0 && Find.TickManager.TicksGame >= CrE_NextPrisonRescueTIme && CrE_GameComponent.GetRandomPrisoner() != null)
            //{
            //    IncidentCrPrisonerRescue.Do();
            //}

        }

        // Change points
        public static void ChangeCrEPoints(int points)
        {
            CrE_GameComponent.CrE_Points += points; // Just use negative numbers for decrease
        }

        // MapLoader
        public static void EnterMapWithTemporaryEscort(Map targetMap)
        {
            Pawn escortPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            List<Pawn> caravanPawns = new List<Pawn> { escortPawn };

            Util.Msg(caravanPawns);
            Util.Msg(targetMap);
            Util.Msg(Faction.OfPlayer);

            int randomTile = Find.WorldGrid.tiles.FindIndex(tile => !tile.biome.impassable);
            if (randomTile < 0)
            {
                Util.Msg("Error: No valid world tiles found for caravan creation.");
                return;
            }

            Caravan caravan = CaravanMaker.MakeCaravan(caravanPawns, Faction.OfPlayer, randomTile, false);

            if (caravan == null)
            {
                Util.Msg("Error: Failed to create caravan.");
                return;
            }

            Util.Msg("Caravan Formed");

            //SettlementUtility
            CaravanEnterMapUtility.Enter(caravan, targetMap, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, draftColonists: true);

            escortPawn.Destroy(DestroyMode.Vanish);
        }

        public static Pawn GetRandomPrisoner()
        {
            List<Pawn> kidnappedPawns = new List<Pawn>();

            foreach (Faction faction in Find.FactionManager.AllFactions)
            {
                List<Pawn> factionKidnappedPawns = faction.kidnapped.KidnappedPawnsListForReading;

                if (factionKidnappedPawns != null && factionKidnappedPawns.Count > 0)
                {
                    kidnappedPawns.AddRange(factionKidnappedPawns.Where(p => !p.Dead));
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
            return (Settings.CrE_Male || pawn.gender != Gender.Male)
                && (Settings.CrE_Female || pawn.gender != Gender.Female)
                && pawn.ageTracker.AgeBiologicalYears > 19;
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

            Scribe_Values.Look<int>(ref CrE_NextPrisonRescueTIme, "CrE_NextPrisonRescueTIme", -1, true);

            Scribe_Collections.Look(ref PirateExtortPawn, "PirateExtortPawn", LookMode.Deep);

        }

        public static int CrE_Points; // CrE Points

        public static int CrELoseRelationsCooldown; // Cooldown for the lose relationship

        public static int CrE_NextPrisonRescueTIme = -1;

        // Pawn, return time, 1 day over, faction captured
        public static List<PirateExtortPawnData> PirateExtortPawn = new List<PirateExtortPawnData>();

        public static Dictionary<Faction, int> FactionRaidCooldowns = new Dictionary<Faction, int>();
    }


}
