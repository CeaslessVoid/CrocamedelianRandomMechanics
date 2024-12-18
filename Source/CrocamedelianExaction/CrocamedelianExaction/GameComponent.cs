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

            if (PirateExtortPawn == null)
                PirateExtortPawn = new List<PirateExtortPawnData>();

            if (FactionRaidCooldowns == null)
                FactionRaidCooldowns = new Dictionary<Faction, int>();

            if (CrECapturePawns == null)
                CrECapturePawns = new List<Pawn>();
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

            if (CrECapturePawns != null)
                CrECapturePawns = new List<Pawn>();
        }

        public override void GameComponentTick() // Every day
        {
            base.GameComponentTick();

            if (GenTicks.IsTickInterval(30000))
                PerformDailyPawnCheck(); // Lies

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

        public static void AddCapturedPawn(Pawn pawn)
        {
            if (pawn != null && !CrECapturePawns.Contains(pawn) && pawn.ageTracker.AgeBiologicalYears >= 18)
            {
                CrECapturePawns.Add(pawn);
            }
        }

         public static void AddAllCapturedPawns()
         {
            CrECapturePawns.RemoveAll(pawn => pawn == null || pawn.Dead || pawn.Destroyed);

            List<Pawn> kidnappedPawns = new List<Pawn>();

            foreach (Faction faction in Find.FactionManager.AllFactions)
            {
                List<Pawn> factionKidnappedPawns = faction.kidnapped?.KidnappedPawnsListForReading;

                if (factionKidnappedPawns != null && factionKidnappedPawns.Count > 0)
                {
                    kidnappedPawns.AddRange(factionKidnappedPawns.Where(pawn => !pawn.Dead));
                }
            }

            foreach (Pawn pawn in kidnappedPawns)
            {
                if (!CrECapturePawns.Contains(pawn))
                {
                    CrECapturePawns.Add(pawn);
                }
            }

        }

        //public static void TransferCapturedPawnsToWorldPawns()
        //{
        //    foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
        //    {
        //        List<Pawn> kidnappedPawns = faction.kidnapped.KidnappedPawnsListForReading;

        //        foreach (Pawn pawn in kidnappedPawns.ToList())
        //        {
        //            faction.kidnapped.RemoveKidnappedPawn(pawn);

        //            pawn.SetFaction(faction);

        //        }
        //    }
        //}

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

        private void PerformDailyPawnCheck() // Half daily now
        {

            AddAllCapturedPawns();

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

        public static void OpenQuestMap(Site site)
        {
            if (site == null)
            {
                Log.Error("Site is null. Cannot open map.");
                return;
            }

            Map map = site.Map;

            if (map == null)
            {
                int tile = site.Tile;
                map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, Find.World.info.initialMapSize, WorldObjectDefOf.Settlement);

                if (map == null)
                {
                    Log.Error("Failed to generate map for site.");
                    return;
                }
            }

            Current.Game.CurrentMap = map;

            IntVec3 center = map.Center;
            CameraJumper.TryJump(center, map);

            Messages.Message("Map opened for the quest site.", MessageTypeDefOf.PositiveEvent, false);
        }


        public static Pawn GetRandomPrisoner() // Kept so that nothing breaks without change much code
        {
            if (CrECapturePawns.Count == 0)
                return null;

            return CrECapturePawns.RandomElement();
        }

        public static void RemovePawnWorld(Pawn pawn)
        {
            Find.WorldPawns.RemovePawn(pawn);
        }

        public static bool isValidPawn(Pawn pawn)
        {
            return (Settings.CrE_Male || pawn.gender != Gender.Male)
                && (Settings.CrE_Female || pawn.gender != Gender.Female)
                && pawn.ageTracker.AgeBiologicalYears >= 18;
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

            Scribe_Collections.Look(ref PirateExtortPawn, "PirateExtortPawn", LookMode.Deep);

            Scribe_Collections.Look(ref CrECapturePawns, "CrECapturePawns", LookMode.Reference);

        }

        public static int CrE_Points; // CrE Points

        public static int CrELoseRelationsCooldown; // Cooldown for the lose relationship

        // Pawn, return time, 1 day over, faction captured
        public static List<PirateExtortPawnData> PirateExtortPawn = new List<PirateExtortPawnData>();

        public static Dictionary<Faction, int> FactionRaidCooldowns = new Dictionary<Faction, int>();

        public static List<Pawn> CrECapturePawns = new List<Pawn>();


    }


}
