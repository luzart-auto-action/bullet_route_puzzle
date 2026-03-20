using BulletRoute.Tile;

namespace BulletRoute.Command
{
    public class RotateTileCommand : ICommand
    {
        private readonly TileBase _tile;
        private readonly int _steps;

        public RotateTileCommand(TileBase tile, int steps = 1)
        {
            _tile = tile;
            _steps = steps;
        }

        public void Execute()
        {
            _tile.Rotate(_steps);
        }

        public void Undo()
        {
            _tile.Rotate(-_steps);
        }
    }
}
