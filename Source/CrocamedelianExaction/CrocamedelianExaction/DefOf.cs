using HarmonyLib;
using LudeonTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace CrocamedelianExaction
{
    [DefOf]
    public static class CrE_DefOf
    {

        public static IncidentDef CrE_PiratePawn_Return;
        public static IncidentDef CrE_PiratePawn_NoReturn;

        public static IncidentDef CrE_PiratePawn_NoPawn;

        static CrE_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CrE_DefOf));
        }
    }

}
