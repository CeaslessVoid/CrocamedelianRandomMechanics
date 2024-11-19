using CrocamedelianExaction;
using HarmonyLib;
using LudeonTK;
using RimWorld;
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
    public class CrE_Need_Respect : Need
    {
        public CrE_Need_Respect(Pawn pawn) : base(pawn)
        {
        }

        private float BaseRespect = 0.5f;

        public int RespectFixed = 0;

        public int RespectSpecial = 0;

        public override void SetInitialLevel()
        {
            base.SetInitialLevel();

            CurLevel = BaseRespect;
            UpdateLevel();
        }

        public override void NeedInterval()
        {

        }

        public override float MaxLevel => 1.0f;

        public override int GUIChangeArrow => 0;

        public void SetSpecial()
        {
            if (RespectSpecial > 1)
            {
                RespectSpecial = (int)(RespectSpecial / 1.1f);
            }
            else if (RespectSpecial < -1)
            {
                RespectSpecial = (int)(RespectSpecial / 1.1f);
            }
            else
            {
                RespectSpecial = 0;
            }
        }

        public void UpdateLevel()
        {
            SetSpecial();

            int RespectModifier = (int)pawn.GetStatValue(CrE_DefOf.RespectModifier);

            int totalRespect = (int)(BaseRespect * 100) + RespectFixed + RespectSpecial + RespectModifier;


            totalRespect = Mathf.Clamp(totalRespect, 0, 100);

            CurLevel = totalRespect / 100f;
        }
    }

    [HarmonyPatch(typeof(Pawn_NeedsTracker), "AddOrRemoveNeedsAsAppropriate")]
    public static class AddRespectNeedPatch
    {
        public static void Postfix(Pawn_NeedsTracker __instance, Pawn ___pawn)
        {
            bool respectActive = CrE_GameComponent.Settings.CrE_Respect_Active;

            if (___pawn.RaceProps.Humanlike)
            {
                Need respectNeed = __instance.AllNeeds.FirstOrDefault(n => n.def.defName == "Respect");

                if (respectActive && respectNeed == null)
                {
                    Need newRespectNeed = new CrE_Need_Respect(___pawn)
                    {
                        def = DefDatabase<NeedDef>.GetNamed("Respect")
                    };

                    __instance.AllNeeds.Add(newRespectNeed);
                }
                else if (!respectActive && respectNeed != null)
                {
                    // Remove Respect need if inactive
                    __instance.AllNeeds.Remove(respectNeed);
                }
            }

        }
    }

}
