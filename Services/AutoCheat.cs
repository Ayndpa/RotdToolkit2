using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RotdToolkit2.Services
{
    /// <summary>
    /// Pure in-memory auto-locator for Road of the Dead 2 (no game file edits).
    ///
    /// Anchors (re-found on every attach, so ASLR/heap layout does not matter):
    ///  * RDPlayerInfo via its default key-binding block (eleven consecutive int32:
    ///    65,68,87,83,32,70,16,82,81,69,80).  Relative to the first key (anchor S),
    ///    verified against the running live process:
    ///       S-0x6C  m_bIsAwareOfStoryMode      S-0x68  m_bStoryModeFinished
    ///       S-0x64  m_bMutantLevelFinished    S-0x50  m_iStoryLevel
    ///       S-0x4C  m_iHardcoreStoryLevel
    ///       S-0x44  m_iBestHitCombo           S-0x40  m_bSavedAmmo
    ///       S-0x3C  m_iDianeDrivingLevel      S-0x38  m_iCochetaDrivingLevel
    ///       S-0x34  m_iDianeToughnessLevel    S-0x30  m_iCochetaToughnessLevel
    ///       S-0x2C  m_iTiresLevel             S-0x28  m_iBumperLevel
    ///       S-0x24  m_iEngineLevel            S-0x20  m_iMedShields
    ///       S-0x1C  m_iMedHeals               S-0x18  m_iChopperSupportLevel
    ///       S-0x14  m_iAmmoPickupLevel        S-0x10  m_iLowerCostLevel
    ///       S-0x0C  m_iDriverSwitchLevel      S-0x08  m_iTaskSpeedLevel
    ///       S-0x04  m_iLevelUpLevel
    ///       S+0x1E8 twelve RDWeaponSaveData refs (4 bytes each, declaration order)
    ///       S+0x22C m_iRP (Number) followed contiguously by the other Number
    ///               fields in declaration order (m_fStoryProgress, ..., distances).
    ///    (verified live: level ints read back 6,1,3,3,2,3,4 and RP read back from
    ///     the save as 227.977899998809.)
    ///  * RDPlayer (the car) via the adjacent Number pair (m_fMaxSpeed,
    ///    m_fXMaxSpeed).  m_fMaxSpeed is one of the six EngineSpeeds constants and
    ///    m_fXMaxSpeed is a small steering limit; structural invariants
    ///    (m_fXAccel==m_fXDeccel, m_fXAccelHandBrake==m_fXDeccelHandBrake) reject the
    ///    static EngineSpeeds constant arrays.  m_fBodyHP sits at +0x68 from it.
    /// </summary>
    public sealed class AutoCheat
    {
        private readonly MemoryTrainer _mem;

        public long PlayerInfoAnchor { get; private set; } = -1;
        public List<long> PlayerSpeedAddresses { get; private set; } = new List<long>();

        private static readonly int[] Keyblock = { 65, 68, 87, 83, 32, 70, 16, 82, 81, 69, 80 };

        // EngineSpeeds from RDPlayer.as (75..90 * 1000/60/60).
        private static readonly double[] EngineSpeeds =
        {
            75.0 * 1000 / 60 / 60,
            78.0 * 1000 / 60 / 60,
            81.0 * 1000 / 60 / 60,
            84.0 * 1000 / 60 / 60,
            87.0 * 1000 / 60 / 60,
            90.0 * 1000 / 60 / 60
        };

        // ---- RDPlayerInfo offsets relative to the first key (anchor) ----
        private const long RpOffset = 0x22C;
        private const long WeaponRefsOffset = 0x1E8;
        private const int WeaponCount = 12;
        private static readonly long[] UnlockBools = { -0x6C, -0x68, -0x64 };
        private static readonly long[] UpgradeLevels =
        {
            -0x3C, -0x38,            // Diane/Cocheta driving (MaxOrganicLevel = 9)
            -0x34, -0x30,            // Diane/Cocheta toughness (MaxOrganicLevel = 9)
            -0x2C, -0x28, -0x24,     // tires, bumper, engine (MaxUpgradeLevel = 5)
            -0x20, -0x1C, -0x18,     // med shields, med heals, chopper (5)
            -0x14, -0x10, -0x0C, -0x08, -0x04 // ammo pickup, lower cost, driver switch, task speed, level up (5)
        };
        private static readonly int[] UpgradeLevelMax =
        {
            9, 9,                    // driving
            9, 9,                    // toughness
            5, 5, 5,                 // tires, bumper, engine
            5, 5, 5,                 // med shields, med heals, chopper
            5, 5, 5, 5, 5            // ammo pickup, lower cost, driver switch, task speed, level up
        };

        // ---- RDWeaponSaveData offsets relative to the object base ----
        private const long WUnlocked = 0x10;
        private const long WAimingLevel = 0x14;
        private const long WHandlingLevel = 0x18;
        private const long WReloadingLevel = 0x1C;
        private const long WDamageLevel = 0x20;
        private const long WAccuracyLevel = 0x24;
        private const long WCapacityLevel = 0x28;
        private const long WAmmoInMag = 0x2C;
        private const long WAmmoInBag = 0x30;
        private const long WKills = 0x34;
        private const long WName = 0x38;
        private const int MaxOrganicLevel = 9;
        private const int MaxUpgradeLevel = 5;

        // ---- RDPlayer (car) offsets relative to m_fMaxSpeed ----
        private const long PMaxSpeed = 0x00;
        private const long PXMaxSpeed = 0x08;
        private const long PXAccel = 0x10;
        private const long PXDeccel = 0x18;
        private const long PXAccelHandBrake = 0x28;
        private const long PXDeccelHandBrake = 0x30;
        private const long PEngineTime = 0x58;
        private const long PBodyHP = 0x68;

        private const double GodHp = 100.0;
        private const double SpeedMax = 80.0;
        private const double SpeedXMax = 24.0;
        private const double MoneyValue = 9999999.0;
        private const int InfiniteAmmo = 9999;
        private const int OneHitDamageLevel = 999999;

        public AutoCheat(MemoryTrainer mem)
        {
            _mem = mem;
        }

        public bool HasPlayerInfo => PlayerInfoAnchor > 0;
        public bool HasPlayers => PlayerSpeedAddresses.Count > 0;

        public void Reset()
        {
            PlayerInfoAnchor = -1;
            PlayerSpeedAddresses = new List<long>();
            _rpOriginal = 0.0;
            _levelOriginal = null;
            _unlockOriginal = null;
            _weaponLevelOriginal = null;
            _weaponUnlockOriginal = null;
            _weaponAmmoOriginal = null;
            _weaponDamageOriginal = null;
            _speedOriginal = null;
            _godOriginal = null;
            _zombieHpFactorAddrs.Clear();
            _zombieHpFactorOriginal.Clear();
        }

        // ---------------- locate ----------------

        public bool LocatePlayerInfo()
        {
            PlayerInfoAnchor = -1;
            var pattern = new byte[Keyblock.Length * 4];
            for (int i = 0; i < Keyblock.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(Keyblock[i]), 0, pattern, i * 4, 4);
            }

            var hits = _mem.FindBytes(pattern, 8, CancellationToken.None);
            if (hits.Count == 0)
            {
                return false;
            }

            PlayerInfoAnchor = hits[0].Address;
            return true;
        }

        public bool LocatePlayers()
        {
            PlayerSpeedAddresses = new List<long>();
            foreach (var e in EngineSpeeds)
            {
                foreach (var hit in _mem.FindBytes(BitConverter.GetBytes(e), 400, CancellationToken.None))
                {
                    long a = hit.Address;

                    double xmax = ReadDouble(a + PXMaxSpeed);
                    if (double.IsNaN(xmax) || xmax <= 1.0 || xmax >= 15.0)
                    {
                        continue;
                    }

                    // The static EngineSpeeds array has the previous engine speed at -8.
                    double prev = ReadDouble(a - 8);
                    if (EngineSpeeds.Any(v => Math.Abs(v - prev) < 1e-6))
                    {
                        continue;
                    }

                    double engineTime = ReadDouble(a + PEngineTime);
                    if (double.IsNaN(engineTime) || engineTime < 0 || engineTime > 100000)
                    {
                        continue;
                    }

                    double bodyHp = ReadDouble(a + PBodyHP);
                    if (double.IsNaN(bodyHp) || bodyHp < 0 || bodyHp > 1000)
                    {
                        continue;
                    }

                    double xacc = ReadDouble(a + PXAccel);
                    double xdec = ReadDouble(a + PXDeccel);
                    double xahb = ReadDouble(a + PXAccelHandBrake);
                    double xdhb = ReadDouble(a + PXDeccelHandBrake);
                    if (Math.Abs(xacc - xdec) > 1e-6 || Math.Abs(xahb - xdhb) > 1e-6)
                    {
                        continue;
                    }
                    if (xacc <= 0 || xacc > 200 || xahb <= 0 || xahb > 400)
                    {
                        continue;
                    }

                    PlayerSpeedAddresses.Add(a);
                }
            }

            PlayerSpeedAddresses = PlayerSpeedAddresses.Distinct().OrderBy(a => a).ToList();
            return PlayerSpeedAddresses.Count > 0;
        }

        // ---------------- RDPlayerInfo cheats ----------------

        private double _rpOriginal;
        private int[] _levelOriginal;
        private int[] _unlockOriginal;
        private int[] _weaponLevelOriginal;
        private int[] _weaponUnlockOriginal;
        private int[] _weaponAmmoOriginal;
        private int[] _weaponDamageOriginal;
        private double[] _speedOriginal;
        private double[] _godOriginal;
        private readonly List<long> _zombieHpFactorAddrs = new List<long>();
        private readonly List<double> _zombieHpFactorOriginal = new List<double>();

        public bool ApplyMoney(bool on)
        {
            if (!EnsureInfo()) return false;
            long addr = PlayerInfoAnchor + RpOffset;
            if (on)
            {
                _rpOriginal = ReadDouble(addr);
                _mem.WriteBytes(addr, MemoryTrainer.ToBytes(ScanType.Double, MoneyValue));
                _mem.SetFrozen(addr, ScanType.Double, MoneyValue, true);
            }
            else
            {
                _mem.SetFrozen(addr, ScanType.Double, 0, false);
                _mem.WriteBytes(addr, MemoryTrainer.ToBytes(ScanType.Double, _rpOriginal));
            }
            return true;
        }

        public bool ApplyMaxUpgrade(bool on)
        {
            if (!EnsureInfo()) return false;

            if (on)
            {
                _levelOriginal = new int[UpgradeLevels.Length];
                for (int i = 0; i < UpgradeLevels.Length; i++)
                {
                    long addr = PlayerInfoAnchor + UpgradeLevels[i];
                    _levelOriginal[i] = ReadInt(addr);
                    _mem.WriteBytes(addr, MemoryTrainer.ToBytes(ScanType.Int32, UpgradeLevelMax[i]));
                }

                var weapons = FindWeapons();
                _weaponLevelOriginal = new int[weapons.Count * 6];
                int k = 0;
                foreach (var w in weapons)
                {
                    long[] levelAddrs = { w + WAimingLevel, w + WHandlingLevel, w + WReloadingLevel,
                                          w + WDamageLevel, w + WAccuracyLevel, w + WCapacityLevel };
                    for (int j = 0; j < levelAddrs.Length; j++)
                    {
                        _weaponLevelOriginal[k++] = ReadInt(levelAddrs[j]);
                        int max = j < 3 ? MaxOrganicLevel : MaxUpgradeLevel;
                        _mem.WriteBytes(levelAddrs[j], MemoryTrainer.ToBytes(ScanType.Int32, max));
                    }
                }
            }
            else
            {
                if (_levelOriginal != null)
                {
                    for (int i = 0; i < UpgradeLevels.Length; i++)
                    {
                        _mem.WriteBytes(PlayerInfoAnchor + UpgradeLevels[i], MemoryTrainer.ToBytes(ScanType.Int32, _levelOriginal[i]));
                    }
                }

                if (_weaponLevelOriginal != null)
                {
                    var weapons = FindWeapons();
                    int k = 0;
                    foreach (var w in weapons)
                    {
                        long[] levelAddrs = { w + WAimingLevel, w + WHandlingLevel, w + WReloadingLevel,
                                              w + WDamageLevel, w + WAccuracyLevel, w + WCapacityLevel };
                        for (int j = 0; j < levelAddrs.Length && k < _weaponLevelOriginal.Length; j++)
                        {
                            _mem.WriteBytes(levelAddrs[j], MemoryTrainer.ToBytes(ScanType.Int32, _weaponLevelOriginal[k++]));
                        }
                    }
                }
            }
            return true;
        }

        public bool ApplyUnlock(bool on)
        {
            if (!EnsureInfo()) return false;

            if (on)
            {
                _unlockOriginal = new int[UnlockBools.Length];
                for (int i = 0; i < UnlockBools.Length; i++)
                {
                    long addr = PlayerInfoAnchor + UnlockBools[i];
                    _unlockOriginal[i] = ReadInt(addr);
                    _mem.WriteBytes(addr, MemoryTrainer.ToBytes(ScanType.Int32, 1));
                }

                var weapons = FindWeapons();
                _weaponUnlockOriginal = new int[weapons.Count];
                for (int i = 0; i < weapons.Count; i++)
                {
                    _weaponUnlockOriginal[i] = ReadInt(weapons[i] + WUnlocked);
                    _mem.WriteBytes(weapons[i] + WUnlocked, MemoryTrainer.ToBytes(ScanType.Int32, 1));
                }
            }
            else
            {
                if (_unlockOriginal != null)
                {
                    for (int i = 0; i < UnlockBools.Length; i++)
                    {
                        _mem.WriteBytes(PlayerInfoAnchor + UnlockBools[i], MemoryTrainer.ToBytes(ScanType.Int32, _unlockOriginal[i]));
                    }
                }

                if (_weaponUnlockOriginal != null)
                {
                    var weapons = FindWeapons();
                    for (int i = 0; i < weapons.Count && i < _weaponUnlockOriginal.Length; i++)
                    {
                        _mem.WriteBytes(weapons[i] + WUnlocked, MemoryTrainer.ToBytes(ScanType.Int32, _weaponUnlockOriginal[i]));
                    }
                }
            }
            return true;
        }

        // ---------------- RDPlayer (car) cheats ----------------

        public bool ApplySpeed(bool on)
        {
            if (!EnsurePlayers()) return false;
            if (on)
            {
                _speedOriginal = new double[PlayerSpeedAddresses.Count * 2];
                for (int i = 0; i < PlayerSpeedAddresses.Count; i++)
                {
                    long a = PlayerSpeedAddresses[i];
                    _speedOriginal[i * 2] = ReadDouble(a + PMaxSpeed);
                    _speedOriginal[i * 2 + 1] = ReadDouble(a + PXMaxSpeed);
                    _mem.SetFrozen(a + PMaxSpeed, ScanType.Double, SpeedMax, true);
                    _mem.SetFrozen(a + PXMaxSpeed, ScanType.Double, SpeedXMax, true);
                }
            }
            else
            {
                for (int i = 0; i < PlayerSpeedAddresses.Count; i++)
                {
                    long a = PlayerSpeedAddresses[i];
                    _mem.SetFrozen(a + PMaxSpeed, ScanType.Double, 0, false);
                    _mem.SetFrozen(a + PXMaxSpeed, ScanType.Double, 0, false);
                    if (_speedOriginal != null && i * 2 + 1 < _speedOriginal.Length)
                    {
                        _mem.WriteBytes(a + PMaxSpeed, MemoryTrainer.ToBytes(ScanType.Double, _speedOriginal[i * 2]));
                        _mem.WriteBytes(a + PXMaxSpeed, MemoryTrainer.ToBytes(ScanType.Double, _speedOriginal[i * 2 + 1]));
                    }
                }
            }
            return true;
        }

        public bool ApplyGod(bool on)
        {
            if (!EnsurePlayers()) return false;
            if (on)
            {
                _godOriginal = new double[PlayerSpeedAddresses.Count];
                for (int i = 0; i < PlayerSpeedAddresses.Count; i++)
                {
                    long hp = PlayerSpeedAddresses[i] + PBodyHP;
                    _godOriginal[i] = ReadDouble(hp);
                    _mem.WriteBytes(hp, MemoryTrainer.ToBytes(ScanType.Double, GodHp));
                    _mem.SetFrozen(hp, ScanType.Double, GodHp, true);
                }
            }
            else
            {
                for (int i = 0; i < PlayerSpeedAddresses.Count; i++)
                {
                    long hp = PlayerSpeedAddresses[i] + PBodyHP;
                    _mem.SetFrozen(hp, ScanType.Double, 0, false);
                    if (_godOriginal != null && i < _godOriginal.Length)
                    {
                        _mem.WriteBytes(hp, MemoryTrainer.ToBytes(ScanType.Double, _godOriginal[i]));
                    }
                }
            }
            return true;
        }

        // ---------------- weapon cheats ----------------

        public bool ApplyInfiniteAmmo(bool on)
        {
            if (!EnsureInfo()) return false;
            var weapons = FindWeapons();
            if (weapons.Count == 0) return false;

            if (on)
            {
                _weaponAmmoOriginal = new int[weapons.Count * 2];
                for (int i = 0; i < weapons.Count; i++)
                {
                    long mag = weapons[i] + WAmmoInMag;
                    long bag = weapons[i] + WAmmoInBag;
                    _weaponAmmoOriginal[i * 2] = ReadInt(mag);
                    _weaponAmmoOriginal[i * 2 + 1] = ReadInt(bag);
                    _mem.SetFrozen(mag, ScanType.Int32, InfiniteAmmo, true);
                    _mem.SetFrozen(bag, ScanType.Int32, InfiniteAmmo, true);
                }
            }
            else
            {
                for (int i = 0; i < weapons.Count; i++)
                {
                    long mag = weapons[i] + WAmmoInMag;
                    long bag = weapons[i] + WAmmoInBag;
                    _mem.SetFrozen(mag, ScanType.Int32, 0, false);
                    _mem.SetFrozen(bag, ScanType.Int32, 0, false);
                    if (_weaponAmmoOriginal != null && i * 2 + 1 < _weaponAmmoOriginal.Length)
                    {
                        _mem.WriteBytes(mag, MemoryTrainer.ToBytes(ScanType.Int32, _weaponAmmoOriginal[i * 2]));
                        _mem.WriteBytes(bag, MemoryTrainer.ToBytes(ScanType.Int32, _weaponAmmoOriginal[i * 2 + 1]));
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// One-hit-kill. Two complementary levers:
        ///  1) Ramming: zero every RDGameModeGameplay.m_fZombieHPFactor, so spawned
        ///     zombies have body HP below m_fGibHP and gib on any car contact.
        ///  2) Guns: raise every RDWeaponSaveData.m_iDamageLevel so a single hit
        ///     is lethal (the live damage formula scales with this level).
        /// </summary>
        public bool ApplyOneHitKill(bool on)
        {
            if (!EnsureInfo()) return false;

            bool any = false;

            if (on)
            {
                // 1) zombie HP factor -> 0 (ramming kills in one hit)
                if (LocateZombieHpFactors())
                {
                    _zombieHpFactorOriginal.Clear();
                    foreach (var addr in _zombieHpFactorAddrs)
                    {
                        _zombieHpFactorOriginal.Add(ReadDouble(addr));
                        _mem.SetFrozen(addr, ScanType.Double, 0.0, true);
                    }
                    any = true;
                }

                // 2) weapon damage level -> huge (guns kill in one hit)
                var weapons = FindWeapons();
                if (weapons.Count > 0)
                {
                    _weaponDamageOriginal = new int[weapons.Count];
                    for (int i = 0; i < weapons.Count; i++)
                    {
                        long dmg = weapons[i] + WDamageLevel;
                        _weaponDamageOriginal[i] = ReadInt(dmg);
                        _mem.WriteBytes(dmg, MemoryTrainer.ToBytes(ScanType.Int32, OneHitDamageLevel));
                    }
                    any = true;
                }
            }
            else
            {
                for (int i = 0; i < _zombieHpFactorAddrs.Count && i < _zombieHpFactorOriginal.Count; i++)
                {
                    _mem.SetFrozen(_zombieHpFactorAddrs[i], ScanType.Double, 0, false);
                    _mem.WriteBytes(_zombieHpFactorAddrs[i], MemoryTrainer.ToBytes(ScanType.Double, _zombieHpFactorOriginal[i]));
                }

                var weapons = FindWeapons();
                if (_weaponDamageOriginal != null)
                {
                    for (int i = 0; i < weapons.Count && i < _weaponDamageOriginal.Length; i++)
                    {
                        _mem.WriteBytes(weapons[i] + WDamageLevel, MemoryTrainer.ToBytes(ScanType.Int32, _weaponDamageOriginal[i]));
                    }
                }
            }
            return on ? any : true;
        }

        public void ClearAll()
        {
            ApplyMoney(false);
            ApplyMaxUpgrade(false);
            ApplyUnlock(false);
            ApplyInfiniteAmmo(false);
            ApplyOneHitKill(false);
            ApplyGod(false);
            ApplySpeed(false);
        }

        // ---------------- helpers ----------------

        private bool EnsureInfo()
        {
            return HasPlayerInfo || LocatePlayerInfo();
        }

        private bool EnsurePlayers()
        {
            return HasPlayers || LocatePlayers();
        }

        /// <summary>
        /// Locates RDGameModeGameplay.m_fZombieHPFactor in every mode instance.
        /// The class declares five consecutive Number fields initialised to 1:
        /// m_fZombieSpeedFactor, m_fZombieHPFactor, m_fOilSlipperinessFactor,
        /// m_fImpactSpeedLossFactor, m_fVehicleHPFactor.  The run is validated by
        /// the nearby m_fMinSpawnRate (1.0, +0x38) and m_fMaxSpawnRate (1.5, +0x40).
        /// Returns the addresses of the HP factor (run start + 0x08).
        /// </summary>
        private bool LocateZombieHpFactors()
        {
            _zombieHpFactorAddrs.Clear();

            var one = BitConverter.GetBytes(1.0);
            var pattern = new byte[40];
            for (int i = 0; i < 5; i++)
            {
                Array.Copy(one, 0, pattern, i * 8, 8);
            }

            foreach (var hit in _mem.FindBytes(pattern, 64, CancellationToken.None))
            {
                if (Math.Abs(ReadDouble(hit.Address + 0x38) - 1.0) > 1e-9)
                {
                    continue;
                }
                if (Math.Abs(ReadDouble(hit.Address + 0x40) - 1.5) > 1e-9)
                {
                    continue;
                }
                _zombieHpFactorAddrs.Add(hit.Address + 0x08);
            }

            return _zombieHpFactorAddrs.Count > 0;
        }

        /// <summary>Reads the twelve RDWeaponSaveData pointers and keeps only valid ones.</summary>
        private List<long> FindWeapons()
        {
            var result = new List<long>();
            if (!HasPlayerInfo)
            {
                return result;
            }

            for (int i = 0; i < WeaponCount; i++)
            {
                long slot = PlayerInfoAnchor + WeaponRefsOffset + i * 4;
                int raw = ReadInt(slot);
                if (raw <= 0) continue;
                long candidate = (uint)raw;
                if (!IsPlausibleWeapon(candidate))
                {
                    candidate &= ~7L; // tolerate AVM2 atom tagging
                }
                if (IsPlausibleWeapon(candidate))
                {
                    result.Add(candidate);
                }
            }
            return result;
        }

        private bool IsPlausibleWeapon(long baseAddr)
        {
            if (baseAddr < 0x01000000 || baseAddr > 0x7FFFFFFF)
            {
                return false;
            }

            int unlocked = ReadInt(baseAddr + WUnlocked);
            if (unlocked < 0 || unlocked > 1)
            {
                return false;
            }

            int namePtr = ReadInt(baseAddr + WName);
            long name = (uint)namePtr;
            if (name < 0x01000000 || name > 0x7FFFFFFF)
            {
                return false;
            }

            return true;
        }

        private double ReadDouble(long addr)
        {
            var b = _mem.ReadBytes(addr, 8);
            return b == null ? double.NaN : BitConverter.ToDouble(b, 0);
        }

        private int ReadInt(long addr)
        {
            var b = _mem.ReadBytes(addr, 4);
            return b == null ? -1 : BitConverter.ToInt32(b, 0);
        }
    }
}
