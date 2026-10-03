using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using RotdToolkit2.Services;

namespace RotdToolkit2
{
    public partial class MainWindow
    {
        private readonly MemoryTrainer _trainer = new MemoryTrainer();
        private AutoCheat _auto;
        private DispatcherTimer _autoTimer;
        private bool _locating;
        private bool _locatePlayersDue = true;
        private bool _suppressCheatToggle;
        private string _cheatStatus = "等待游戏进程…（启动游戏后会自动连接）";
        private bool _cheatAvailable;

        public string CheatStatus
        {
            get => _cheatStatus;
            set { _cheatStatus = value; Raise(nameof(CheatStatus)); }
        }

        public bool CheatAvailable
        {
            get => _cheatAvailable;
            private set
            {
                if (_cheatAvailable == value)
                {
                    return;
                }
                _cheatAvailable = value;
                Raise(nameof(CheatAvailable));
            }
        }

        private void InitTrainer()
        {
            _auto = new AutoCheat(_trainer);
            _autoTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _autoTimer.Tick += (_, __) => AutoConnectCheats();
            _autoTimer.Start();
            AutoConnectCheats();
        }

        protected override void OnClosed(EventArgs e)
        {
            _trainer.Dispose();
            base.OnClosed(e);
        }

        private void CheatPageActivated()
        {
            _locatePlayersDue = true;
            AutoConnectCheats();
        }

        private void RelocateCheatsAsync()
        {
            if (!_trainer.IsAttached)
            {
                CheatStatus = "等待游戏进程…（启动游戏后会自动连接）";
                return;
            }
            if (_locating)
            {
                return;
            }

            _locating = true;
            CheatStatus = "正在定位…";
            Task.Run(() =>
            {
                try
                {
                    bool pi = _auto.LocatePlayerInfo();
                    bool pl = _auto.HasPlayers || _auto.LocatePlayers();
                    Dispatcher.Invoke(() =>
                    {
                        CheatStatus = $"存档对象：{(pi ? "已定位" : "未找到")}　玩家对象：{(pl ? _auto.PlayerSpeedAddresses.Count + " 个" : "未找到（进入车内画面后自动识别）")}";
                        UpdateCheatAvailability();
                    });
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    _locating = false;
                }
            });
        }

        private static readonly string[] GameProcessHints = { "flash", "road-of-the-dead", "ejecutador" };

        private Process FindGameProcess()
        {
            try
            {
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        string name = p.ProcessName.ToLowerInvariant();
                        if (!GameProcessHints.Any(h => name.Contains(h)))
                        {
                            continue;
                        }
                        if (p.MainWindowHandle == IntPtr.Zero)
                        {
                            continue;
                        }
                        return p;
                    }
                    catch
                    {
                        // ignore protected processes
                    }
                }
            }
            catch
            {
                // ignore
            }
            return null;
        }

        private void AutoConnectCheats()
        {
            try
            {
                if (_trainer.IsAttached)
                {
                    bool alive;
                    try { alive = _trainer.Process != null && !_trainer.Process.HasExited; }
                    catch { alive = false; }

                    if (!alive)
                    {
                        _trainer.Detach();
                        _auto.Reset();
                        ResetAllToggles();
                        CheatStatus = "游戏已退出。";
                        UpdateCheatAvailability();
                        return;
                    }

                    UpdateCheatAvailability();

                    if (!_auto.HasPlayerInfo)
                    {
                        RelocateCheatsAsync();
                    }
                    else if (Nav.SelectedIndex == 1 && !_auto.HasPlayers && _locatePlayersDue)
                    {
                        _locatePlayersDue = false;
                        RelocateCheatsAsync();
                    }
                    return;
                }

                var target = FindGameProcess();
                if (target == null)
                {
                    ResetAllToggles();
                    CheatStatus = "等待游戏进程…（启动游戏后会自动连接）";
                    UpdateCheatAvailability();
                    return;
                }

                _trainer.Attach(target);
                _locatePlayersDue = true;
                UpdateCheatAvailability();
                RelocateCheatsAsync();
            }
            catch
            {
                ResetAllToggles();
                CheatStatus = "自动连接失败（可尝试以管理员身份运行）。";
                UpdateCheatAvailability();
            }
        }

        private void UpdateCheatAvailability()
        {
            bool available = _trainer.IsAttached && _auto.HasPlayerInfo;
            if (_cheatAvailable == available)
            {
                return;
            }
            CheatAvailable = available;
            if (!available)
            {
                ResetAllToggles();
            }
        }

        private bool RequireAttached()
        {
            if (!_trainer.IsAttached)
            {
                AutoConnectCheats();
            }
            if (!_trainer.IsAttached)
            {
                CheatStatus = "未找到游戏进程：请先启动游戏（启动后会自动连接）。";
                return false;
            }
            return true;
        }

        // ---------------- one-click memory cheats ----------------

        private void OnGodToggle(object sender, RoutedEventArgs e)
        {
            if (_suppressCheatToggle || !(sender is ToggleButton ts)) return;
            if (ts.IsChecked == true)
            {
                if (!RequireAttached()) { RevertToggle(ts); return; }
                if (!_auto.HasPlayers) _auto.LocatePlayers();
                if (!_auto.HasPlayers)
                {
                    CheatStatus = "未定位到玩家对象：请先进入游戏画面（车内视角）再开。";
                    RevertToggle(ts); return;
                }
                _auto.ApplyGod(true);
            }
            else
            {
                if (_trainer.IsAttached) _auto.ApplyGod(false);
            }
        }

        private void OnSpeedToggle(object sender, RoutedEventArgs e)
        {
            if (_suppressCheatToggle || !(sender is ToggleButton ts)) return;
            if (ts.IsChecked == true)
            {
                if (!RequireAttached()) { RevertToggle(ts); return; }
                if (!_auto.HasPlayers) _auto.LocatePlayers();
                if (!_auto.HasPlayers)
                {
                    CheatStatus = "未定位到玩家对象：请先进入游戏画面（车内视角）再开。";
                    RevertToggle(ts); return;
                }
                _auto.ApplySpeed(true);
            }
            else
            {
                if (_trainer.IsAttached) _auto.ApplySpeed(false);
            }
        }

        private void OnMoneyToggle(object sender, RoutedEventArgs e)
        {
            if (_suppressCheatToggle || !(sender is ToggleButton ts)) return;
            if (ts.IsChecked == true)
            {
                if (!RequireAttached()) { RevertToggle(ts); return; }
                if (!_auto.HasPlayerInfo && !_auto.LocatePlayerInfo())
                {
                    CheatStatus = "未定位到存档对象。";
                    RevertToggle(ts); return;
                }
                _auto.ApplyMoney(true);
            }
            else
            {
                if (_trainer.IsAttached) _auto.ApplyMoney(false);
            }
        }

        private void OnMaxUpToggle(object sender, RoutedEventArgs e)
        {
            if (_suppressCheatToggle || !(sender is ToggleButton ts)) return;
            if (ts.IsChecked == true)
            {
                if (!RequireAttached()) { RevertToggle(ts); return; }
                if (!_auto.HasPlayerInfo && !_auto.LocatePlayerInfo())
                {
                    CheatStatus = "未定位到存档对象。";
                    RevertToggle(ts); return;
                }
                _auto.ApplyMaxUpgrade(true);
            }
            else
            {
                if (_trainer.IsAttached) _auto.ApplyMaxUpgrade(false);
            }
        }

        private void OnUnlockToggle(object sender, RoutedEventArgs e)
        {
            if (_suppressCheatToggle || !(sender is ToggleButton ts)) return;
            if (ts.IsChecked == true)
            {
                if (!RequireAttached()) { RevertToggle(ts); return; }
                if (!_auto.HasPlayerInfo && !_auto.LocatePlayerInfo())
                {
                    CheatStatus = "未定位到存档对象。";
                    RevertToggle(ts); return;
                }
                _auto.ApplyUnlock(true);
            }
            else
            {
                if (_trainer.IsAttached) _auto.ApplyUnlock(false);
            }
        }

        private void OnAmmoToggle(object sender, RoutedEventArgs e)
        {
            if (_suppressCheatToggle || !(sender is ToggleButton ts)) return;
            if (ts.IsChecked == true)
            {
                if (!RequireAttached()) { RevertToggle(ts); return; }
                if (!_auto.HasPlayerInfo && !_auto.LocatePlayerInfo())
                {
                    CheatStatus = "未定位到存档对象。";
                    RevertToggle(ts); return;
                }
                _auto.ApplyInfiniteAmmo(true);
            }
            else
            {
                if (_trainer.IsAttached) _auto.ApplyInfiniteAmmo(false);
            }
        }

        private void OnOneHitToggle(object sender, RoutedEventArgs e)
        {
            if (_suppressCheatToggle || !(sender is ToggleButton ts)) return;
            if (ts.IsChecked == true)
            {
                if (!RequireAttached()) { RevertToggle(ts); return; }
                if (!_auto.HasPlayerInfo && !_auto.LocatePlayerInfo())
                {
                    CheatStatus = "未定位到存档对象。";
                    RevertToggle(ts); return;
                }
                _auto.ApplyOneHitKill(true);
            }
            else
            {
                if (_trainer.IsAttached) _auto.ApplyOneHitKill(false);
            }
        }

        private void OnCheatOff(object sender, RoutedEventArgs e)
        {
            ResetAllToggles();
            if (_trainer.IsAttached)
            {
                _auto.ClearAll();
            }
            CheatStatus = "已关闭全部作弊。";
        }

        private void RevertToggle(ToggleButton toggle)
        {
            _suppressCheatToggle = true;
            toggle.IsChecked = false;
            _suppressCheatToggle = false;
        }

        private void ResetAllToggles()
        {
            _suppressCheatToggle = true;
            TogGod.IsChecked = false;
            TogSpeed.IsChecked = false;
            TogAmmo.IsChecked = false;
            TogOneHit.IsChecked = false;
            TogMoney.IsChecked = false;
            TogMaxUp.IsChecked = false;
            TogUnlock.IsChecked = false;
            _suppressCheatToggle = false;
        }
    }
}


