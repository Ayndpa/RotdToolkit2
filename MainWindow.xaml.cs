using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using RotdToolkit2.Lso;
using RotdToolkit2.Services;

namespace RotdToolkit2
{
    public partial class MainWindow : HandyControl.Controls.Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private SaveLocation _location;
        private LsoDocument _editingDoc;
        private string _editingPath;

        public SaveSlotVm CnSlot { get; } = new SaveSlotVm
        {
            Label = "U77 汉化版",
            Sandbox = @"…\<seed>\localhost\RoadOfTheDead2_Beta1.sol",
            Tag = "cn",
            OverwriteLabel = "同步到 SWF 版"
        };

        public SaveSlotVm EnSlot { get; } = new SaveSlotVm
        {
            Label = "SWF 版",
            Sandbox = @"…\<seed>\#localWithNet\RoadOfTheDead2_Beta1.sol",
            Tag = "en",
            OverwriteLabel = "同步到 U77 版"
        };

        public ObservableCollection<BackupVm> Backups { get; } = new ObservableCollection<BackupVm>();
        public ObservableCollection<FieldVm> Fields { get; } = new ObservableCollection<FieldVm>();

        private string _backupRoot = BackupService.DefaultRoot;
        public string BackupRoot
        {
            get => _backupRoot;
            set { _backupRoot = value; Raise(nameof(BackupRoot)); }
        }

        private BackupVm _selectedBackup;
        public BackupVm SelectedBackup
        {
            get => _selectedBackup;
            set { _selectedBackup = value; Raise(nameof(SelectedBackup)); }
        }

        private string _statusText = "就绪。";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; Raise(nameof(StatusText)); }
        }

        private string _cnTimeTag = "—";
        public string CnTimeTag
        {
            get => _cnTimeTag;
            private set { _cnTimeTag = value; Raise(nameof(CnTimeTag)); }
        }

        private string _enTimeTag = "—";
        public string EnTimeTag
        {
            get => _enTimeTag;
            private set { _enTimeTag = value; Raise(nameof(EnTimeTag)); }
        }

        private string _syncHint = string.Empty;
        public string SyncHint
        {
            get => _syncHint;
            private set { _syncHint = value; Raise(nameof(SyncHint)); }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += (_, __) =>
            {
                Locate();
                RefreshSlots();
                RefreshBackups();
                CmbEditSide.Items.Clear();
                CmbEditSide.Items.Add(CnSlot.Label);
                CmbEditSide.Items.Add(EnSlot.Label);
                CmbEditSide.SelectedIndex = 1;
                InitTrainer();
                ApplyNav(0);
            };
        }

        private void Locate()
        {
            _location = SaveLocator.Locate();
            CnSlot.Path = _location.CnPath;
            EnSlot.Path = _location.EnPath;
        }

        private void RefreshSlots()
        {
            CnSlot.Refresh();
            EnSlot.Refresh();
            UpdateComparison();
        }

        private void UpdateComparison()
        {
            bool cn = CnSlot.Exists;
            bool en = EnSlot.Exists;

            if (!cn && !en)
            {
                CnTimeTag = "缺失";
                EnTimeTag = "缺失";
                SyncHint = "两份存档都不存在。";
                return;
            }

            if (!cn || !en)
            {
                CnTimeTag = cn ? "唯一" : "缺失";
                EnTimeTag = en ? "唯一" : "缺失";
                SyncHint = cn
                    ? "只有 U77 汉化版存在，可同步到 SWF 版。"
                    : "只有 SWF 版存在，可同步到 U77 汉化版。";
                return;
            }

            int cmp = System.Nullable.Compare(CnSlot.Modified, EnSlot.Modified);
            if (cmp == 0)
            {
                CnTimeTag = "相同";
                EnTimeTag = "相同";
            }
            else if (cmp > 0)
            {
                CnTimeTag = "较新";
                EnTimeTag = "较早";
            }
            else
            {
                CnTimeTag = "较早";
                EnTimeTag = "较新";
            }

            bool same =
                CnSlot.Money == EnSlot.Money &&
                CnSlot.Story == EnSlot.Story &&
                CnSlot.Upgrades == EnSlot.Upgrades &&
                CnSlot.Achieved == EnSlot.Achieved &&
                CnSlot.Total == EnSlot.Total &&
                CnSlot.Killed == EnSlot.Killed &&
                CnSlot.Civilians == EnSlot.Civilians &&
                CnSlot.Splatter == EnSlot.Splatter;

            SyncHint = same ? "两份存档数据一致，无需同步。" : "两份存档数据不同，请确认方向后再同步。";
        }

        private void RefreshBackups()
        {
            Backups.Clear();
            foreach (var r in BackupService.List(BackupRoot))
            {
                Backups.Add(new BackupVm
                {
                    Directory = r.Directory,
                    TimeText = r.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                    InfoText = $"{r.FileCount} 个存档，{r.Size} 字节",
                    Note = r.Note
                });
            }
        }

        private void OnNavSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplyNav(Nav.SelectedIndex);
        }

        private void ApplyNav(int index)
        {
            var panels = new[] { PanelSync, PanelCheat, PanelEdit, PanelBackup };
            if (index < 0 || index >= panels.Length || panels.Any(p => p == null))
            {
                return;
            }

            for (int i = 0; i < panels.Length; i++)
            {
                panels[i].Visibility = i == index ? Visibility.Visible : Visibility.Collapsed;
            }

            if (index == 1)
            {
                CheatPageActivated();
            }
        }

        // ============ 同步 ============

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            Locate();
            RefreshSlots();
            StatusText = "已刷新存档位置与状态。";
        }

        private void OnCopyPath(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is SaveSlotVm slot && !string.IsNullOrEmpty(slot.Path))
            {
                try
                {
                    System.Windows.Clipboard.SetText(slot.Path);
                    StatusText = "已复制存档路径。";
                }
                catch
                {
                    // clipboard can be locked by another process; ignore
                }
            }
        }

        private void OnGoBackup(object sender, RoutedEventArgs e)
        {
            Nav.SelectedIndex = 3;
        }

        private void OnSyncCnToEn(object sender, RoutedEventArgs e) => Sync(CnSlot, EnSlot);

        private void OnSyncEnToCn(object sender, RoutedEventArgs e) => Sync(EnSlot, CnSlot);

        private void OnOverwriteSlot(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is SaveSlotVm slot)
            {
                if (slot.Tag == "cn")
                {
                    Sync(CnSlot, EnSlot);
                }
                else
                {
                    Sync(EnSlot, CnSlot);
                }
            }
        }

        private void Sync(SaveSlotVm from, SaveSlotVm to)
        {
            try
            {
                if (string.IsNullOrEmpty(from.Path) || !System.IO.File.Exists(from.Path))
                {
                    StatusText = $"同步失败：来源不存在（{from.Label}）。";
                    return;
                }

                string backupNote = ChkSyncAutoBackup.IsChecked == true
                    ? "覆盖前会自动备份目标存档。"
                    : "当前未开启自动备份。";
                var confirm = System.Windows.MessageBox.Show(
                    $"确定用「{from.Label}」覆盖「{to.Label}」吗？\n{backupNote}",
                    "确认同步",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.OK)
                {
                    StatusText = "已取消同步。";
                    return;
                }

                if (ChkSyncAutoBackup.IsChecked == true)
                {
                    var items = new List<BackupItem>();
                    if (!string.IsNullOrEmpty(to.Path) && System.IO.File.Exists(to.Path))
                    {
                        items.Add(new BackupItem { Key = to.Tag, SourcePath = to.Path });
                    }
                    if (items.Count > 0)
                    {
                        string dir = BackupService.CreateBackup(BackupRoot, items, $"同步前自动备份（{from.Label} → {to.Label}）");
                        StatusText = $"已自动备份目标存档：{dir}";
                    }
                }

                var targetDir = System.IO.Path.GetDirectoryName(to.Path);
                if (!string.IsNullOrEmpty(targetDir))
                {
                    System.IO.Directory.CreateDirectory(targetDir);
                }
                System.IO.File.Copy(from.Path, to.Path, true);

                RefreshSlots();
                RefreshBackups();
                StatusText = (StatusText.StartsWith("已自动备份") ? StatusText + "  " : "") +
                             $"同步完成：{from.Label} → {to.Label}";
            }
            catch (Exception ex)
            {
                StatusText = "同步失败：" + ex.Message;
            }
        }

        // ============ 备份 ============

        private void OnBrowseBackupRoot(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "选择备份目录";
                dialog.SelectedPath = BackupRoot;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    BackupRoot = dialog.SelectedPath;
                }
            }
        }

        private void OnCreateBackup(object sender, RoutedEventArgs e)
        {
            try
            {
                var items = new List<BackupItem>();
                if (CnSlot.Exists) items.Add(new BackupItem { Key = "cn", SourcePath = CnSlot.Path });
                if (EnSlot.Exists) items.Add(new BackupItem { Key = "en", SourcePath = EnSlot.Path });

                if (items.Count == 0)
                {
                    StatusText = "没有可备份的存档。";
                    return;
                }

                string dir = BackupService.CreateBackup(BackupRoot, items, "手动备份");
                RefreshBackups();
                StatusText = "备份完成：" + dir;
            }
            catch (Exception ex)
            {
                StatusText = "备份失败：" + ex.Message;
            }
        }

        private void OnRestoreBackup(object sender, RoutedEventArgs e)
        {
            if (SelectedBackup == null)
            {
                StatusText = "请先在列表中选择一个备份。";
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                "确定要还原该备份吗？\n当前的存档会先被自动备份一次。\n\n" + SelectedBackup.Directory,
                "还原备份", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.OK)
            {
                return;
            }

            try
            {
                // safety backup of current state
                Locate();
                var safe = new List<BackupItem>();
                if (System.IO.File.Exists(CnSlot.Path)) safe.Add(new BackupItem { Key = "cn", SourcePath = CnSlot.Path });
                if (System.IO.File.Exists(EnSlot.Path)) safe.Add(new BackupItem { Key = "en", SourcePath = EnSlot.Path });
                if (safe.Count > 0)
                {
                    BackupService.CreateBackup(BackupRoot, safe, "还原前自动备份");
                }

                BackupService.Restore(SelectedBackup.Directory);
                Locate();
                RefreshSlots();
                RefreshBackups();
                StatusText = "已还原备份：" + SelectedBackup.Directory;
            }
            catch (Exception ex)
            {
                StatusText = "还原失败：" + ex.Message;
            }
        }

        private void OnDeleteBackup(object sender, RoutedEventArgs e)
        {
            if (SelectedBackup == null)
            {
                StatusText = "请先在列表中选择一个备份。";
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                "确定删除该备份文件夹？此操作不可撤销。\n\n" + SelectedBackup.Directory,
                "删除备份", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.OK)
            {
                return;
            }

            try
            {
                BackupService.Delete(SelectedBackup.Directory);
                RefreshBackups();
                StatusText = "已删除备份。";
            }
            catch (Exception ex)
            {
                StatusText = "删除失败：" + ex.Message;
            }
        }

        private void OnOpenBackupRoot(object sender, RoutedEventArgs e)
        {
            SaveLocator.OpenInExplorer(BackupRoot);
        }

        private void OnRefreshBackups(object sender, RoutedEventArgs e)
        {
            RefreshBackups();
            StatusText = "备份列表已刷新。";
        }

        // ============ 修改 ============

        private void OnEditSideChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
        }

        private void OnLoadForEdit(object sender, RoutedEventArgs e) => LoadForEdit();

        private void OnReloadForEdit(object sender, RoutedEventArgs e) => LoadForEdit();

        private void LoadForEdit()
        {
            try
            {
                string path = CmbEditSide.SelectedIndex == 0 ? CnSlot.Path : EnSlot.Path;
                if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                {
                    StatusText = "该存档不存在，无法编辑。";
                    Fields.Clear();
                    _editingDoc = null;
                    _editingPath = null;
                    return;
                }

                _editingDoc = LsoDocument.Load(path);
                _editingPath = path;

                Fields.Clear();
                var ordered = _editingDoc.Fields
                    .OrderBy(f => FieldCatalog.GroupOrder(FieldCatalog.Get(f.Name).Group))
                    .ThenBy(f => FieldCatalog.Get(f.Name).Label, StringComparer.Ordinal);
                foreach (var f in ordered)
                {
                    Fields.Add(FieldVm.From(f));
                }

                StatusText = $"已加载：{path}（{Fields.Count} 个字段）";
            }
            catch (Exception ex)
            {
                _editingDoc = null;
                _editingPath = null;
                Fields.Clear();
                StatusText = "加载失败：" + ex.Message;
            }
        }

        private void OnApplyEdit(object sender, RoutedEventArgs e)
        {
            if (_editingDoc == null || string.IsNullOrEmpty(_editingPath))
            {
                StatusText = "请先加载一个存档。";
                return;
            }

            try
            {
                foreach (var vm in Fields)
                {
                    switch (vm.Kind)
                    {
                        case FieldKind.Bool:
                            vm.Source.Kind = LsoValueKind.Bool;
                            vm.Source.Bool = vm.BoolValue;
                            break;

                        case FieldKind.Number:
                            if (!double.TryParse(vm.TextValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                            {
                                StatusText = $"“{vm.Label}”的值不是有效数字：{vm.TextValue}";
                                return;
                            }
                            if (number == Math.Floor(number) && number >= -268435456 && number <= 268435455)
                            {
                                vm.Source.Kind = LsoValueKind.Int;
                                vm.Source.Int = (long)number;
                            }
                            else
                            {
                                vm.Source.Kind = LsoValueKind.Double;
                                vm.Source.Double = number;
                            }
                            break;
                    }
                }

                if (ChkAutoBackup.IsChecked == true && System.IO.File.Exists(_editingPath))
                {
                    string key = string.Equals(_editingPath, CnSlot.Path, StringComparison.OrdinalIgnoreCase) ? "cn" : "en";
                    BackupService.CreateBackup(BackupRoot,
                        new List<BackupItem> { new BackupItem { Key = key, SourcePath = _editingPath } },
                        "修改前自动备份");
                }

                _editingDoc.Save(_editingPath);
                RefreshSlots();
                RefreshBackups();
                StatusText = "已保存修改：" + _editingPath;
            }
            catch (Exception ex)
            {
                StatusText = "保存失败：" + ex.Message;
            }
        }
    }
}
