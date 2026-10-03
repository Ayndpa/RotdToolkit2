using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RotdToolkit2.Lso;
using RotdToolkit2.Services;

namespace RotdToolkit2
{
    public abstract class Observable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Raise([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value))
            {
                return false;
            }
            field = value;
            Raise(name);
            return true;
        }
    }

    public sealed class SaveSlotVm : Observable
    {
        private bool _exists;
        private string _stateText = "—";
        private string _path;
        private DateTime? _modified;

        public string Label { get; set; }
        public string Sandbox { get; set; }
        public string Tag { get; set; }
        public string OverwriteLabel { get; set; }

        public string Path
        {
            get => _path;
            set => Set(ref _path, value);
        }

        public bool Exists
        {
            get => _exists;
            private set => Set(ref _exists, value);
        }

        public string StateText
        {
            get => _stateText;
            private set => Set(ref _stateText, value);
        }

        public DateTime? Modified
        {
            get => _modified;
            private set => Set(ref _modified, value);
        }

        public long Money { get; private set; }
        public long Story { get; private set; }
        public long Upgrades { get; private set; }
        public long Killed { get; private set; }
        public long Civilians { get; private set; }
        public long Splatter { get; private set; }
        public int Achieved { get; private set; }
        public int Total { get; private set; }
        public long SizeBytes { get; private set; }

        public string MoneyText => Exists ? Money.ToString("N0") : "—";
        public string StoryText => Exists ? Story.ToString("N0") : "—";
        public string UpgradesText => Exists ? Upgrades.ToString("N0") : "—";
        public string AchievementText => Exists && Total > 0 ? $"{Achieved} / {Total}" : "—";
        public string StatsText => Exists ? $"击杀 {Killed} · 平民 {Civilians} · 撞击 {Splatter}" : "—";
        public string SizeText => Exists ? FormatSize(SizeBytes) : "—";
        public string RelativeTimeText => Exists ? Humanize(Modified) : "—";
        public string ModifiedFullText => Exists && Modified.HasValue ? Modified.Value.ToString("yyyy-MM-dd HH:mm") : "—";

        public void Refresh()
        {
            try
            {
                var fi = new System.IO.FileInfo(Path);
                Exists = fi.Exists;
                if (fi.Exists)
                {
                    StateText = "已存在";
                    SizeBytes = fi.Length;
                    Modified = fi.LastWriteTime;
                    Analyze(Path);
                }
                else
                {
                    Reset("缺失");
                }
            }
            catch
            {
                Reset("读取失败");
            }

            Raise(null);
        }

        private void Reset(string state)
        {
            Exists = false;
            StateText = state;
            SizeBytes = 0;
            Modified = null;
            Money = Story = Upgrades = Killed = Civilians = Splatter = 0;
            Achieved = Total = 0;
        }

        private void Analyze(string path)
        {
            try
            {
                var doc = LsoDocument.Load(path);

                foreach (var f in doc.Fields)
                {
                    if (f.Name == "m_completedAchievements" && f.Value.Kind == LsoValueKind.Array && f.Value.Array != null)
                    {
                        int achieved = 0, total = 0;
                        foreach (var item in f.Value.Array)
                        {
                            if (item.Kind == LsoValueKind.Bool)
                            {
                                total++;
                                if (item.Bool)
                                {
                                    achieved++;
                                }
                            }
                        }
                        Achieved = achieved;
                        Total = total;
                        continue;
                    }

                    if (!f.Value.IsNumeric)
                    {
                        continue;
                    }

                    switch (f.Name)
                    {
                        case "m_iRP": Money = (long)f.Value.AsDouble(); break;
                        case "m_iStoryLevel": Story = (long)f.Value.AsDouble(); break;
                        case "m_iEngineLevel":
                        case "m_iBumperLevel":
                        case "m_iTiresLevel":
                        case "m_iDamageLevel":
                        case "m_iHandlingLevel":
                        case "m_iAccuracyLevel":
                        case "m_iAimingLevel":
                        case "m_iReloadingLevel":
                        case "m_iCapacityLevel":
                        case "m_iAmmoPickupLevel":
                        case "m_iLowerCostLevel":
                        case "m_iChopperSupportLevel":
                        case "m_iDriverSwitchLevel":
                        case "m_iTaskSpeedLevel":
                        case "m_iLevelUpLevel":
                        case "m_iDianeDrivingLevel":
                        case "m_iCochetaDrivingLevel":
                        case "m_iDianeToughnessLevel":
                        case "m_iCochetaToughnessLevel":
                            Upgrades += (long)f.Value.AsDouble();
                            break;
                        case "m_iKills": Killed = (long)f.Value.AsDouble(); break;
                        case "m_iNbKilledCivilians": Civilians = (long)f.Value.AsDouble(); break;
                        case "m_iNbZombieSplatterHits": Splatter = (long)f.Value.AsDouble(); break;
                    }
                }
            }
            catch
            {
                Money = Story = Upgrades = Killed = Civilians = Splatter = 0;
                Achieved = Total = 0;
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)
            {
                return bytes + " B";
            }

            double kb = bytes / 1024.0;
            if (kb < 1024.0)
            {
                return kb.ToString("0.#") + " KB";
            }

            return (kb / 1024.0).ToString("0.##") + " MB";
        }

        private static string Humanize(DateTime? time)
        {
            if (!time.HasValue)
            {
                return "—";
            }

            var span = DateTime.Now - time.Value;
            if (span.TotalSeconds < 45)
            {
                return "刚刚";
            }
            if (span.TotalMinutes < 60)
            {
                return $"{(int)span.TotalMinutes} 分钟前";
            }
            if (span.TotalHours < 24)
            {
                return $"{(int)span.TotalHours} 小时前";
            }
            if (span.TotalDays < 30)
            {
                return $"{(int)span.TotalDays} 天前";
            }
            return time.Value.ToString("yyyy-MM-dd");
        }
    }

    public sealed class BackupVm
    {
        public string Directory { get; set; }
        public string TimeText { get; set; }
        public string InfoText { get; set; }
        public string Note { get; set; }
        public string Display => $"{TimeText}    {InfoText}" + (string.IsNullOrEmpty(Note) ? "" : $"    ({Note})");
    }

    public enum FieldKind
    {
        Bool,
        Number,
        Other
    }

    public sealed class FieldVm : Observable
    {
        private bool _boolValue;
        private string _textValue;

        public string Name { get; set; }
        public string Label { get; set; }
        public string Group { get; set; }
        public FieldKind Kind { get; set; }
        public string OtherText { get; set; }
        public LsoValue Source { get; set; }

        public bool BoolValue
        {
            get => _boolValue;
            set => Set(ref _boolValue, value);
        }

        public string TextValue
        {
            get => _textValue;
            set => Set(ref _textValue, value);
        }

        public static FieldVm From(LsoField field)
        {
            var meta = FieldCatalog.Get(field.Name);
            var vm = new FieldVm
            {
                Name = field.Name,
                Label = meta.Label,
                Group = meta.Group,
                Source = field.Value
            };

            switch (field.Value.Kind)
            {
                case LsoValueKind.Bool:
                    vm.Kind = FieldKind.Bool;
                    vm.BoolValue = field.Value.Bool;
                    break;
                case LsoValueKind.Int:
                    vm.Kind = FieldKind.Number;
                    vm.TextValue = field.Value.Int.ToString();
                    break;
                case LsoValueKind.Double:
                    vm.Kind = FieldKind.Number;
                    vm.TextValue = field.Value.Double.ToString("0.####");
                    break;
                case LsoValueKind.Array:
                    vm.Kind = FieldKind.Other;
                    vm.OtherText = $"[数组 {field.Value.Array?.Count ?? 0} 项]（暂不支持编辑）";
                    break;
                case LsoValueKind.String:
                    vm.Kind = FieldKind.Other;
                    vm.OtherText = field.Value.String;
                    break;
                default:
                    vm.Kind = FieldKind.Other;
                    vm.OtherText = field.Value.Kind.ToString();
                    break;
            }

            return vm;
        }
    }
}

