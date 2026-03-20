using System.Collections.Generic;
using BulletRoute.Core;

namespace BulletRoute.Tile
{
    public class CrossTile : TileBase
    {
        // Cross tile: passes bullet straight through in any direction
        public override List<Direction> GetExitDirections(Direction entryDirection)
        {
            var exits = new List<Direction>();
            exits.Add(DirectionHelper.Opposite(entryDirection));
            return exits;
        }
    }
}
