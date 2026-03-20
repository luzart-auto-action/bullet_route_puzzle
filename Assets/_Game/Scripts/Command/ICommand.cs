namespace BulletRoute.Command
{
    public interface ICommand
    {
        void Execute();
        void Undo();
    }
}
