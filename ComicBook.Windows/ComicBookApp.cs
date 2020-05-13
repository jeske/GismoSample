using Stride.DebugDrawer;
using Stride.Engine;

namespace ComicBook.Windows
{
    class ComicBookApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.GameSystems.Add(new DebugDrawerSystem(game));
                game.Run();
            }
        }
    }
}
