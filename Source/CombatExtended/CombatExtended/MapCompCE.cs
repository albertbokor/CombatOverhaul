using System;
using Verse;

namespace CombatExtended
{
    /// <summary>
    /// Store general data related to tools used in CE
    /// </summary>
    public class MapCompCE : MapComponent
    {
        private CellFlooder flooder;
        public CellFlooder Flooder
        {
            get => flooder;
        }

        public MapCompCE(Map map): base(map)
        {
            this.flooder = new CellFlooder(map);
        }
    }
}

