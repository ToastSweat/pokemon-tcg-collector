using System.IO;
using VintageCollector.Runtime;

namespace VintageCollector.EditorTools
{
    // Pure admission policy; does not mutate scenes, builds or collections.
    public static class Phase3BuildPolicy
    {
        public static string Resolve(string projectRoot)
        {
            string output = Path.GetFullPath(Path.Combine(projectRoot, "Builds", "Phase3Windows"));
            GamePaths.Resolve(projectRoot, Path.Combine(output, "VintageCardCollector_Data"), false, new string[0]);
            string saves = Path.Combine(output, "Saves");
            if (Directory.Exists(saves) && Directory.GetFileSystemEntries(saves).Length != 0)
                throw new System.InvalidOperationException("Refusing to rebuild over populated candidate saves. Close the game and preserve/move the whole save directory first.");
            return output;
        }
    }
}
