using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static System.Collections.Specialized.BitVector32;
using Verse.Noise;

namespace CrocamedelianExaction
{
    public class IncidentWorker_FactionRelationsDeterioration : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return Find.FactionManager.AllFactions
                .Where(f => !f.def.permanentEnemy && !f.IsPlayer && f.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Ally)
                .Any();
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Faction targetFaction = Find.FactionManager.AllFactions
                .Where(f => !f.def.permanentEnemy && !f.IsPlayer && f.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Ally)
                .OrderByDescending(f => f.PlayerGoodwill)
                .FirstOrDefault();

            if (targetFaction == null)
            {
                return false;
            }

            CrE_GameComponent.ResetCrELoseRelationsCooldown();

            Util.Msg(targetFaction);

            int relationLoss = Mathf.RoundToInt(CrE_GameComponent.CrE_Points / 10);
            relationLoss = Mathf.Clamp(relationLoss, 1, 20);

            targetFaction.TryAffectGoodwillWith(Faction.OfPlayer, -relationLoss, canSendMessage: true);

            // Notify the player
            string messageText = $"Relations with {targetFaction.Name} have deteriorated due to your questionable activities. Their goodwill has decreased by {relationLoss}.";
            Messages.Message(messageText, MessageTypeDefOf.NegativeEvent);

            return true;
        }
    }


    // REMOVED
    public class IncidentWorker_FactionRelationsImprovement : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return Find.FactionManager.AllFactions
                .Where(f => !f.def.permanentEnemy && f.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Hostile)
                .Any();
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            // Get all valid non-pirate factions with non-hostile relations
            var validFactions = Find.FactionManager.AllFactions
                .Where(f => !f.def.permanentEnemy && f.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Hostile)
                .ToList();

            if (!validFactions.Any())
            {
                return false;
            }

            Faction targetFaction = validFactions.RandomElement();

            int relationGain = Mathf.RoundToInt(Mathf.Abs(CrE_GameComponent.CrE_Points) / 10);
            relationGain = Mathf.Clamp(relationGain, 1, 20); // Minimum of 1, maximum of 20

            targetFaction.TryAffectGoodwillWith(Faction.OfPlayer, relationGain, canSendMessage: true);

            string messageText = $"Your recent assertiveness and bold actions have earned you respect. Relations with {targetFaction.Name} have improved, increasing their goodwill by {relationGain}.";
            Messages.Message(messageText, MessageTypeDefOf.PositiveEvent);

            return true;
        }
    }
}
