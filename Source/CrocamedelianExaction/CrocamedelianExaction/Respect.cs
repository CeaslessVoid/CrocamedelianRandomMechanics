//using CrocamedelianExaction;
//using HarmonyLib;
//using LudeonTK;
//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//using Verse;

//namespace CrocamedelianExaction
//{
//    public class CrE_Need_Respect : Need
//    {
//        public CrE_Need_Respect(Pawn pawn) : base(pawn)
//        {
//        }

//        private float BaseRespect = 0.5f;

//        public int RespectFixed = 0;

//        public int RespectSpecial = 0;

//        public override void SetInitialLevel()
//        {
//            base.SetInitialLevel();

//            CurLevel = BaseRespect;
//            UpdateLevel();
//        }

//        public override void NeedInterval()
//        {

//        }

//        public override float MaxLevel => 1.0f;

//        public override int GUIChangeArrow => 0;

//        public void SetSpecial()
//        {
//            if (RespectSpecial > 1)
//            {
//                RespectSpecial = (int)(RespectSpecial / 1.1f);
//            }
//            else if (RespectSpecial < -1)
//            {
//                RespectSpecial = (int)(RespectSpecial / 1.1f);
//            }
//            else
//            {
//                RespectSpecial = 0;
//            }
//        }

//        public void UpdateLevel()
//        {
//            if (pawn == null || CrE_DefOf.RespectModifier == null)
//            {
//                Util.Error("[CrE]: Pawn or RespectModifier is null. UpdateLevel failed.");
//                return;
//            }

//            SetSpecial();

//            int RespectModifier = (int)pawn.GetStatValue(CrE_DefOf.RespectModifier);
//            RespectSpecial = Mathf.Clamp(RespectSpecial, -50, 50);

//            int totalRespect = (int)(BaseRespect * 100) + RespectFixed + RespectSpecial + RespectModifier;

//            totalRespect = Mathf.Clamp(totalRespect, 0, 100);

//            CurLevel = totalRespect / 100f;
//        }
//    }

//    //[HarmonyPatch(typeof(Pawn_NeedsTracker), "AddOrRemoveNeedsAsAppropriate")]
//    //public static class AddRespectNeedPatch
//    //{
//    //    public static void Postfix(Pawn_NeedsTracker __instance, Pawn ___pawn)
//    //    {
//    //        bool respectActive = CrE_GameComponent.Settings.CrE_Respect_Active;

//    //        if (___pawn?.RaceProps?.Humanlike != true)
//    //        {
//    //            return;
//    //        }

//    //        Need respectNeed = __instance.AllNeeds.FirstOrDefault(n => n.def.defName == "Respect");

//    //        if (respectActive && respectNeed == null)
//    //        {
//    //            NeedDef respectDef = DefDatabase<NeedDef>.GetNamed("Respect", false);
//    //            if (respectDef == null)
//    //            {
//    //                Log.Error("Respect NeedDef not found!");
//    //                return;
//    //            }

//    //            Need newRespectNeed = new CrE_Need_Respect(___pawn) { def = respectDef };
//    //            __instance.AllNeeds.Add(newRespectNeed);
//    //        }
//    //        else if (!respectActive && respectNeed != null)
//    //        {
//    //            __instance.AllNeeds.Remove(respectNeed);
//    //        }
//    //    }
//    //}

//    //[HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
//    //public static class AddRespectNeedPatch
//    //{
//    //    public static void Postfix(ref bool __result, NeedDef nd)
//    //    {

//    //        if (nd.defName == "Respect")
//    //        {
//    //            __result = CrE_GameComponent.Settings.CrE_Respect_Active;
//    //        }

//    //    }
//    //}

//}
