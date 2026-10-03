using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace RotdToolkit2.Services
{
    public sealed class BackupItem
    {
        public string Key;          // "cn" / "en"
        public string SourcePath;   // original .sol location
    }

    public sealed class BackupRecord
    {
        public string Directory;
        public DateTime Time;
        public int FileCount;
        public long Size;
        public string Note;
    }

    public static class BackupService
    {
        public static string DefaultRoot =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RotdToolkit2Backups");

        /// <summary>Create a timestamped backup folder; returns the folder path.</summary>
        public static string CreateBackup(string backupRoot, IEnumerable<BackupItem> items, string note = null)
        {
            Directory.CreateDirectory(backupRoot);

            string name = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string dir = Path.Combine(backupRoot, name);
            int suffix = 1;
            while (Directory.Exists(dir))
            {
                dir = Path.Combine(backupRoot, name + "_" + suffix++);
            }
            Directory.CreateDirectory(dir);

            var manifest = new StringBuilder();
            foreach (var it in items)
            {
                if (string.IsNullOrEmpty(it.SourcePath) || !File.Exists(it.SourcePath))
                {
                    continue;
                }

                string target = Path.Combine(dir, it.Key + ".sol");
                File.Copy(it.SourcePath, target, true);
                manifest.Append(it.Key).Append('\t').Append(it.SourcePath).Append('\n');
            }

            File.WriteAllText(Path.Combine(dir, "_manifest.txt"), manifest.ToString(), Encoding.UTF8);
            if (!string.IsNullOrEmpty(note))
            {
                File.WriteAllText(Path.Combine(dir, "_note.txt"), note, Encoding.UTF8);
            }

            return dir;
        }

        public static List<BackupRecord> List(string backupRoot)
        {
            var list = new List<BackupRecord>();
            if (string.IsNullOrEmpty(backupRoot) || !Directory.Exists(backupRoot))
            {
                return list;
            }

            foreach (var dir in Directory.GetDirectories(backupRoot))
            {
                var files = Directory.GetFiles(dir);
                string noteFile = Path.Combine(dir, "_note.txt");
                var record = new BackupRecord
                {
                    Directory = dir,
                    Time = Directory.GetCreationTime(dir),
                    FileCount = files.Count(f => f.EndsWith(".sol", StringComparison.OrdinalIgnoreCase)),
                    Size = files.Sum(f => new FileInfo(f).Length),
                    Note = File.Exists(noteFile) ? File.ReadAllText(noteFile, Encoding.UTF8).Trim() : ""
                };

                // Prefer timestamp parsed from folder name.
                var leaf = Path.GetFileName(dir);
                if (leaf.Length >= 15 &&
                    DateTime.TryParseExact(leaf.Substring(0, 15), "yyyyMMdd_HHmmss",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                {
                    record.Time = t;
                }

                list.Add(record);
            }

            return list.OrderByDescending(r => r.Time).ToList();
        }

        public static void Restore(string backupDirectory)
        {
            string manifestPath = Path.Combine(backupDirectory, "_manifest.txt");
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException("备份清单不存在。", manifestPath);
            }

            foreach (var line in File.ReadAllLines(manifestPath, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split('\t');
                if (parts.Length < 2)
                {
                    continue;
                }

                string key = parts[0];
                string target = parts[1];
                string source = Path.Combine(backupDirectory, key + ".sol");
                if (!File.Exists(source))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.Copy(source, target, true);
            }
        }

        public static void Delete(string backupDirectory)
        {
            if (Directory.Exists(backupDirectory))
            {
                Directory.Delete(backupDirectory, true);
            }
        }
    }
}

