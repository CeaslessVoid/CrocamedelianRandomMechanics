using HarmonyLib;
using LudeonTK;
using rjw;
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
    [HarmonyPatch(typeof(AfterSexUtility), "UpdateRecordsInitiator")]
    public static class Patch_UpdateRecordsInitiatorRapeTattoo
    {
        public static void Prefix(SexProps props)
        {
            Pawn pawn = props.pawn;
            Pawn partner = props.partner;

            if (!CrE_GameComponent.Settings.CrE_RapeTats)
            {
                return;
            }

            if (props.isRape && xxx.is_human(pawn))
            {
                if ((CrE_GameComponent.Settings.CrE_RapeTatsColonist && pawn.IsColonist) || (CrE_GameComponent.Settings.CrE_RapeTatsOthers && !pawn.IsColonist))
                {
                    CrE_GameComponent.RapeTattoo(partner);
                }
                
            }
        }
    }
}
