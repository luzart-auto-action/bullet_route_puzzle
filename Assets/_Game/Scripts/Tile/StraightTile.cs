using System.Collections.Generic;
using BulletRoute.Core;

namespace BulletRoute.Tile
{
    public class StraightTile : TileBase
    {
        // Straight tile: passes bullet through in a line
        // Default (rotation 0): Up <-> Down
        public override List<Direction> GetExitDirections(Direction entryDirection)
        {
            Direction localEntry = ReverseRotation(entryDirection);

            var exits = new List<Direction>();

            if (localEntry == Direction.Up)
                exits.Add(ApplyRotation(Direction.Down));
            else if (localEntry == Direction.Down)
                exits.Add(ApplyRotation(Direction.Up));

            return exits;
        }
    }
}
