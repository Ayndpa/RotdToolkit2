using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace RotdToolkit2.Services
{
    public enum ScanType
    {
        Int32,
        Int64,
        Float,
        Double
    }

    public sealed class ScanEntry
    {
        public long Address;
        public double Value;
    }

    public sealed class FrozenEntry
    {
        public long Address;
        public byte[] Bytes;
        public double Value;
        public ScanType Type;
    }

    public sealed class MemoryTrainer : IDisposable
    {
        private IntPtr _handle = IntPtr.Zero;
        private readonly object _lock = new object();
        private readonly List<FrozenEntry> _frozen = new List<FrozenEntry>();
        private Thread _freezeThread;
        private volatile bool _freezeRunning;

        public Process Process { get; private set; }
        public bool IsAttached => _handle != IntPtr.Zero;

        public void Attach(Process process)
        {
            Detach();
            Process = process;
            _handle = Native.OpenProcess(
                Native.PROCESS_QUERY_INFORMATION | Native.PROCESS_VM_OPERATION | Native.PROCESS_VM_READ | Native.PROCESS_VM_WRITE,
                false, process.Id);
            if (_handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("无法打开目标进程（可能需要以管理员身份运行）。错误码：" + Marshal.GetLastWin32Error());
            }
            StartFreeze();
        }

        public void Detach()
        {
            StopFreeze();
            lock (_lock)
            {
                _frozen.Clear();
            }
            if (_handle != IntPtr.Zero)
            {
                Native.CloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
            Process = null;
        }

        // ---------------- memory ----------------

        public List<Region> Regions()
        {
            var list = new List<Region>();
            if (!IsAttached)
            {
                return list;
            }

            int mbiSize = Marshal.SizeOf(typeof(Native.MEMORY_BASIC_INFORMATION));
            long addr = 0;
            while (addr >= 0)
            {
                IntPtr q = Native.VirtualQueryEx(_handle, new IntPtr(addr), out var mbi, (IntPtr)mbiSize);
                if (q == IntPtr.Zero)
                {
                    break;
                }

                long baseAddr = mbi.BaseAddress.ToInt64();
                long size = mbi.RegionSize.ToInt64();
                if (size <= 0)
                {
                    break;
                }

                if (mbi.State == Native.MEM_COMMIT &&
                    (mbi.Protect & (Native.PAGE_NOACCESS | Native.PAGE_GUARD)) == 0)
                {
                    uint prot = mbi.Protect;
                    bool readable = (prot & 0x02) != 0 || (prot & 0x04) != 0 || (prot & 0x08) != 0 ||
                                    (prot & 0x20) != 0 || (prot & 0x40) != 0 || (prot & 0x80) != 0;
                    if (readable)
                    {
                        list.Add(new Region { Base = baseAddr, Size = size });
                    }
                }

                long next = baseAddr + size;
                if (next <= addr)
                {
                    break;
                }
                addr = next;
            }

            return list;
        }

        public byte[] ReadBytes(long address, int size)
        {
            if (!IsAttached)
            {
                return null;
            }
            var buf = new byte[size];
            if (!Native.ReadProcessMemory(_handle, new IntPtr(address), buf, (IntPtr)size, out var read) || read.ToInt64() < size)
            {
                return null;
            }
            return buf;
        }

        public bool WriteBytes(long address, byte[] bytes)
        {
            if (!IsAttached)
            {
                return false;
            }
            return Native.WriteProcessMemory(_handle, new IntPtr(address), bytes, (IntPtr)bytes.Length, out _);
        }

        // ---------------- scan ----------------

        public List<ScanEntry> FindBytes(byte[] pattern, int maxResults, CancellationToken ct)
        {
            var result = new List<ScanEntry>();
            int size = pattern.Length;
            var buf = new byte[1 << 20];

            foreach (var region in Regions())
            {
                long pos = region.Base;
                long end = region.Base + region.Size;
                while (pos < end)
                {
                    ct.ThrowIfCancellationRequested();
                    int chunk = (int)Math.Min((long)buf.Length, end - pos);
                    if (Native.ReadProcessMemory(_handle, new IntPtr(pos), buf, (IntPtr)chunk, out var readPtr))
                    {
                        int read = (int)readPtr.ToInt64();
                        for (int i = 0; i + size <= read; i++)
                        {
                            if (Matches(buf, i, pattern, size))
                            {
                                result.Add(new ScanEntry { Address = pos + i });
                                if (result.Count >= maxResults)
                                {
                                    return result;
                                }
                            }
                        }
                    }

                    int advance = chunk - (size - 1);
                    if (advance <= 0)
                    {
                        advance = chunk;
                    }
                    pos += advance;
                }
            }

            return result;
        }

        // ---------------- freeze ----------------

        public void SetFrozen(long address, ScanType type, double value, bool frozen)
        {
            lock (_lock)
            {
                _frozen.RemoveAll(f => f.Address == address);
                if (frozen)
                {
                    _frozen.Add(new FrozenEntry
                    {
                        Address = address,
                        Type = type,
                        Value = value,
                        Bytes = ToBytes(type, value)
                    });
                }
            }
        }

        public IReadOnlyList<FrozenEntry> Frozen
        {
            get { lock (_lock) { return _frozen.ToArray(); } }
        }

        private void StartFreeze()
        {
            if (_freezeRunning)
            {
                return;
            }
            _freezeRunning = true;
            _freezeThread = new Thread(() =>
            {
                while (_freezeRunning)
                {
                    try
                    {
                        List<FrozenEntry> snapshot;
                        lock (_lock)
                        {
                            snapshot = new List<FrozenEntry>(_frozen);
                        }
                        foreach (var f in snapshot)
                        {
                            if (_handle != IntPtr.Zero)
                            {
                                Native.WriteProcessMemory(_handle, new IntPtr(f.Address), f.Bytes, (IntPtr)f.Bytes.Length, out _);
                            }
                        }
                    }
                    catch
                    {
                        // ignore transient errors
                    }
                    Thread.Sleep(50);
                }
            })
            { IsBackground = true };
            _freezeThread.Start();
        }

        private void StopFreeze()
        {
            _freezeRunning = false;
            try
            {
                _freezeThread?.Join(500);
            }
            catch
            {
                // ignore
            }
            _freezeThread = null;
        }

        // ---------------- helpers ----------------

        public static int SizeOf(ScanType type)
        {
            switch (type)
            {
                case ScanType.Int32: return 4;
                case ScanType.Float: return 4;
                case ScanType.Int64: return 8;
                default: return 8;
            }
        }

        public static byte[] ToBytes(ScanType type, double value)
        {
            switch (type)
            {
                case ScanType.Int32:
                    return BitConverter.GetBytes((int)Math.Round(value));
                case ScanType.Float:
                    return BitConverter.GetBytes((float)value);
                case ScanType.Int64:
                    return BitConverter.GetBytes((long)Math.Round(value));
                default:
                    return BitConverter.GetBytes(value);
            }
        }

        public static double FromBytes(ScanType type, byte[] b)
        {
            switch (type)
            {
                case ScanType.Int32: return BitConverter.ToInt32(b, 0);
                case ScanType.Float: return BitConverter.ToSingle(b, 0);
                case ScanType.Int64: return BitConverter.ToInt64(b, 0);
                default: return BitConverter.ToDouble(b, 0);
            }
        }

        private static bool Matches(byte[] buffer, int offset, byte[] pattern, int size)
        {
            for (int k = 0; k < size; k++)
            {
                if (buffer[offset + k] != pattern[k])
                {
                    return false;
                }
            }
            return true;
        }

        public void Dispose()
        {
            Detach();
        }
    }
}

