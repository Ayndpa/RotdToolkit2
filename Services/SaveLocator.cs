using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RotdToolkit2.Services
{
    public sealed class SaveLocation
    {
        public string Root;
        public string SeedDirectory;
        public string CnPath;      // Road of the Dead 2：localhost 沙箱
        public string EnPath;      // Road of the Dead 2：#localWithNet 沙箱
        public List<string> AllPaths = new List<string>();
    }

    public static class SaveLocator
    {
        public const string SaveFileName = "RoadOfTheDead2_Beta1.sol";
        public const string SandboxA = "localhost";
        public const string SandboxB = "#localWithNet";

        public static string Root =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Macromedia", "Flash Player", "#SharedObjects");

        public static SaveLocation Locate()
        {
            var result = new SaveLocation { Root = Root };

            var all = new List<string>();
            try
            {
                if (Directory.Exists(Root))
                {
                    all.AddRange(Directory.GetFiles(Root, SaveFileName, SearchOption.AllDirectories));
                }
            }
            catch
            {
                // ignore inaccessible folders
            }

            result.AllPaths = all
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToList();

            string Pick(string sandbox)
            {
                return all
                    .Where(p => string.Equals(Path.GetFileName(Path.GetDirectoryName(p)), sandbox, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }

            result.CnPath = Pick(SandboxA);
            result.EnPath = Pick(SandboxB);

            // Determine a seed directory: from any found file, else the newest subfolder.
            string any = result.CnPath ?? result.EnPath
                         ?? all.OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
            if (any != null)
            {
                // ...\<seed>\<sandbox>\file  -> seed
                result.SeedDirectory = Path.GetDirectoryName(Path.GetDirectoryName(any));
            }
            else if (Directory.Exists(Root))
            {
                result.SeedDirectory = Directory.GetDirectories(Root)
                    .OrderByDescending(Directory.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }

            // Fall back to conventional paths so backup still has targets.
            if (result.SeedDirectory != null)
            {
                if (string.IsNullOrEmpty(result.CnPath))
                {
                    result.CnPath = Path.Combine(result.SeedDirectory, SandboxA, SaveFileName);
                }
                if (string.IsNullOrEmpty(result.EnPath))
                {
                    result.EnPath = Path.Combine(result.SeedDirectory, SandboxB, SaveFileName);
                }
            }

            return result;
        }

        public static void OpenInExplorer(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string dir = File.Exists(path) ? Path.GetDirectoryName(path) : path;
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                return;
            }

            System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + (File.Exists(path) ? path : dir) + "\"");
        }
    }
}
