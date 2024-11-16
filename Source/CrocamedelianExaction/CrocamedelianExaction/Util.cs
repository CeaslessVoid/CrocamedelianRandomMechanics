using HarmonyLib;
using LudeonTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CrocamedelianExaction
{
    internal static class Util
    {
        public static void Msg(object o)
        {
            Log.Message("[CrE] " + ((o != null) ? o.ToString() : null));
        }

        public static void Warn(object o)
        {
            Log.Warning("[CrE] " + ((o != null) ? o.ToString() : null));
        }

        public static void Error(object o)
        {
            Log.Error("[CrE] " + ((o != null) ? o.ToString() : null));
        }

    }

}
