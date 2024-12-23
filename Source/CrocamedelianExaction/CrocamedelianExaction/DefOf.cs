﻿using HarmonyLib;
using LudeonTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using Verse.AI;

namespace CrocamedelianExaction
{
    [DefOf]
    public static class CrE_DefOf
    {

        public static IncidentDef CrE_PiratePawn_Return;
        public static IncidentDef CrE_PiratePawn_NoReturn;

        public static IncidentDef CrE_PiratePawn_NoPawn;

        public static JobDef      CrE_AdministerDrug;
        public static JobDef      CrE_FeedEnemy;
        public static JobDef      CrE_AdministerYayo;
        public static JobDef      CrE_ApplyTattoo;
        //public static JobDef      CrE_ApplyTattooTribute;

        //public static ThingDef    ChastityCage;

        //public static DutyDef     Get_Raped;
        //public static DutyDef     Make_Tribute;

        //[MayRequireRoyalty]
        //public static RitualOutcomeEffectDef TributeCeremony;

        static CrE_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CrE_DefOf));
        }
    }

}
