using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CrocamedelianExaction
{
    public class PirateExtortPawnData : IExposable, ILoadReferenceable
    {
        public Pawn Pawn;
        public int TargetTick;
        public int TimeoutTick;
        public Faction Faction;

        private string uniqueID;

        public PirateExtortPawnData()
        {
        }

        public PirateExtortPawnData(Pawn pawn, int targetTick, int timeoutTick, Faction faction)
        {
            Pawn = pawn;
            TargetTick = targetTick;
            TimeoutTick = timeoutTick;
            Faction = faction;
            uniqueID = pawn.ThingID;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref Pawn, "Pawn");
            Scribe_Values.Look(ref TargetTick, "TargetTick");
            Scribe_Values.Look(ref TimeoutTick, "TimeoutTick");
            Scribe_References.Look(ref Faction, "Faction");
            Scribe_Values.Look(ref uniqueID, "UniqueID");
        }

        public string GetUniqueLoadID()
        {
            return $"PirateExtortPawn_{uniqueID}";
        }
    }

}
