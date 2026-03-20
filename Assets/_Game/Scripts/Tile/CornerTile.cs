using System.Collections.Generic;
using BulletRoute.Core;

namespace BulletRoute.Tile
{
    public class CornerTile : TileBase
    {
        // Corner tile: changes direction 90 degrees
        // Default (rotation 0): Up -> Right, Right -> Up
        public override List<Direction> GetExitDirections(Direction entryDirection)
        {
            Direction localEntry = ReverseRotation(entryDirection);

            var exits = new List<Direction>();

            if (localEntry == Direction.Up)
                exits.Add(ApplyRotation(Direction.Right));
            else if (localEntry == Direction.Right)
                exits.Add(ApplyRotation(Direction.Up));

            return exits;
        }
    }
}
