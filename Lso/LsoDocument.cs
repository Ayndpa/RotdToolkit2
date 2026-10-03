using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RotdToolkit2.Lso
{
    public enum LsoValueKind
    {
        Undefined,
        Null,
        Bool,
        Int,
        Double,
        String,
        Array,
        Object,
        ByteArray,
        Date,
        Xml,
        XmlString,
        Reference
    }

    /// <summary>AMF3 字符串/名称，记录它是内联写入还是引用已有字符串。</summary>
    public sealed class LsoName
    {
        public string Text;
        public bool IsRef;
        public int RefIndex = -1;
    }

    /// <summary>对象动态成员（名称可以是字符串引用）。</summary>
    public sealed class LsoMember
    {
        public LsoName Name;
        public LsoValue Value;
    }

    /// <summary>数组的关联项（键名称可以是字符串引用）。</summary>
    public sealed class LsoAssocEntry
    {
        public LsoName Key;
        public LsoValue Value;
    }

    /// <summary>AMF3 对象（可含 traits/静态成员/动态成员/外部化）。</summary>
    public sealed class LsoObject
    {
        public bool UsesTraitsRef;
        public int TraitsRefIndex = -1;
        public string ClassName;
        public bool ClassNameIsRef;
        public int ClassNameRefIndex = -1;
        public bool Externalizable;
        public bool Dynamic;
        public List<LsoName> SealedNames = new List<LsoName>();
        public List<LsoValue> SealedValues = new List<LsoValue>();
        public List<LsoMember> DynamicMembers = new List<LsoMember>();
        public LsoValue ExternalizableBody;
    }

    public sealed class LsoValue
    {
        public LsoValueKind Kind;
        public bool Bool;
        public long Int;
        public double Double;
        public string String;
        public bool StringIsRef;
        public int StringRefIndex = -1;
        public List<LsoValue> Array;
        public List<LsoAssocEntry> Assoc;
        public LsoObject Object;
        public byte[] Bytes;
        public double DateMilliseconds;
        public string Xml;
        public byte RefMarker;
        public int RefIndex;

        public static LsoValue FromBool(bool v) => new LsoValue { Kind = LsoValueKind.Bool, Bool = v };
        public static LsoValue FromInt(long v) => new LsoValue { Kind = LsoValueKind.Int, Int = v };
        public static LsoValue FromDouble(double v) => new LsoValue { Kind = LsoValueKind.Double, Double = v };

        public bool IsNumeric => Kind == LsoValueKind.Int || Kind == LsoValueKind.Double;

        public double AsDouble() => Kind == LsoValueKind.Int ? Int : Double;
    }

    public sealed class LsoField
    {
        public string Name;
        public LsoValue Value;
    }

    /// <summary>
    /// Road of the Dead (Flash) local shared object (.sol) reader/writer.
    /// Format: [00 BF][u32 length]["TCSO"][u16 a][u32 b][u16 nameLen][name][u32 version]
    ///         then repeated: [u29 (nameLen&lt;&lt;1|1)][name][AMF3 value][00]
    /// 支持 AMF3 基本类型、数组（含关联数组）、对象（traits/静态/动态/外部化）、
    /// 字符串/对象/traits 引用，并保持原样以便字节级往返。
    /// </summary>
    public sealed class LsoDocument
    {
        public string ObjectName { get; private set; }
        public uint Version { get; private set; }
        public List<LsoField> Fields { get; } = new List<LsoField>();
        public byte[] Extra1 { get; private set; } = new byte[] { 0x00, 0x04 };
        public byte[] Extra2 { get; private set; } = new byte[] { 0x00, 0x00, 0x00, 0x00 };

        private static readonly byte[] TCSO = { (byte)'T', (byte)'C', (byte)'S', (byte)'O' };

        public static LsoDocument Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static LsoDocument Load(byte[] b)
        {
            if (b.Length < 20 || b[0] != 0x00 || b[1] != 0xBF)
            {
                throw new InvalidDataException("不是有效的 Flash 本地存档 (.sol) 文件。");
            }

            var doc = new LsoDocument();
            int i = 6;
            for (int k = 0; k < 4; k++)
            {
                if (b[i + k] != TCSO[k])
                {
                    throw new InvalidDataException("缺少 TCSO 签名，可能不是 .sol 文件。");
                }
            }
            i += 4;

            doc.Extra1 = new byte[] { b[i], b[i + 1] };
            i += 2;
            doc.Extra2 = new byte[] { b[i], b[i + 1], b[i + 2], b[i + 3] };
            i += 4;

            int nameLen = (b[i] << 8) | b[i + 1];
            i += 2;
            doc.ObjectName = Encoding.UTF8.GetString(b, i, nameLen);
            i += nameLen;

            doc.Version = ReadU32(b, ref i);

            var reader = new AmfReader(b) { I = i };
            while (reader.I < b.Length)
            {
                int raw = ReadU29(b, ref reader.I);
                int len = raw >> 1;
                if (len == 0)
                {
                    break;
                }

                string name = Encoding.UTF8.GetString(b, reader.I, len);
                reader.I += len;

                var value = reader.ReadValue();
                doc.Fields.Add(new LsoField { Name = name, Value = value });

                if (reader.I < b.Length && b[reader.I] == 0x00)
                {
                    reader.I++;
                }
            }

            return doc;
        }

        public void Save(string path)
        {
            File.WriteAllBytes(path, Serialize());
        }

        public byte[] Serialize()
        {
            var rest = new MemoryStream();
            rest.Write(TCSO, 0, TCSO.Length);
            rest.Write(Extra1, 0, Extra1.Length);
            rest.Write(Extra2, 0, Extra2.Length);

            var nameBytes = Encoding.UTF8.GetBytes(ObjectName);
            rest.WriteByte((byte)(nameBytes.Length >> 8));
            rest.WriteByte((byte)(nameBytes.Length & 0xFF));
            rest.Write(nameBytes, 0, nameBytes.Length);

            WriteU32(rest, Version);

            foreach (var f in Fields)
            {
                var fb = Encoding.UTF8.GetBytes(f.Name);
                WriteU29(rest, (fb.Length << 1) | 1);
                rest.Write(fb, 0, fb.Length);
                WriteValue(rest, f.Value);
                rest.WriteByte(0x00);
            }

            var restBytes = rest.ToArray();
            var outBytes = new byte[6 + restBytes.Length];
            outBytes[0] = 0x00;
            outBytes[1] = 0xBF;
            WriteU32Big(outBytes, 2, (uint)restBytes.Length);
            Buffer.BlockCopy(restBytes, 0, outBytes, 6, restBytes.Length);
            return outBytes;
        }

        // ---- AMF3 reader ----

        private sealed class TraitInfo
        {
            public string ClassName;
            public bool ClassNameIsRef;
            public int ClassNameRefIndex = -1;
            public bool Externalizable;
            public bool Dynamic;
            public readonly List<LsoName> Sealed = new List<LsoName>();
        }

        private sealed class AmfReader
        {
            public readonly byte[] B;
            public int I;
            public readonly List<string> Strings = new List<string>();
            public readonly List<TraitInfo> Traits = new List<TraitInfo>();

            public AmfReader(byte[] b)
            {
                B = b;
            }

            private int ReadU29()
            {
                return LsoDocument.ReadU29(B, ref I);
            }

            private LsoName ReadName()
            {
                int u = ReadU29();
                if ((u & 1) == 0)
                {
                    int idx = u >> 1;
                    string text = idx >= 0 && idx < Strings.Count ? Strings[idx] : string.Empty;
                    return new LsoName { Text = text, IsRef = true, RefIndex = idx };
                }

                int len = u >> 1;
                string s = len == 0 ? string.Empty : Encoding.UTF8.GetString(B, I, len);
                I += len;
                if (len > 0)
                {
                    Strings.Add(s);
                }
                return new LsoName { Text = s, IsRef = false, RefIndex = -1 };
            }

            private double ReadDoubleBE()
            {
                double d = BitConverter.ToDouble(
                    new[] { B[I + 7], B[I + 6], B[I + 5], B[I + 4], B[I + 3], B[I + 2], B[I + 1], B[I] }, 0);
                I += 8;
                return d;
            }

            public LsoValue ReadValue()
            {
                byte t = B[I++];
                switch (t)
                {
                    case 0x00: return new LsoValue { Kind = LsoValueKind.Undefined };
                    case 0x01: return new LsoValue { Kind = LsoValueKind.Null };
                    case 0x02: return LsoValue.FromBool(false);
                    case 0x03: return LsoValue.FromBool(true);
                    case 0x04:
                        {
                            int raw = ReadU29();
                            return LsoValue.FromInt(SignExtend29(raw));
                        }
                    case 0x05:
                        return LsoValue.FromDouble(ReadDoubleBE());
                    case 0x06:
                        {
                            var n = ReadName();
                            return new LsoValue
                            {
                                Kind = LsoValueKind.String,
                                String = n.Text,
                                StringIsRef = n.IsRef,
                                StringRefIndex = n.RefIndex
                            };
                        }
                    case 0x07:
                        {
                            var n = ReadName();
                            return new LsoValue
                            {
                                Kind = LsoValueKind.Xml,
                                Xml = n.Text,
                                StringIsRef = n.IsRef,
                                StringRefIndex = n.RefIndex
                            };
                        }
                    case 0x08:
                        {
                            int u = ReadU29();
                            if ((u & 1) == 0)
                            {
                                return MakeReference(0x08, u >> 1);
                            }
                            return new LsoValue { Kind = LsoValueKind.Date, DateMilliseconds = ReadDoubleBE() };
                        }
                    case 0x09:
                        return ReadArray();
                    case 0x0A:
                        return ReadObject();
                    case 0x0B:
                        {
                            var n = ReadName();
                            return new LsoValue
                            {
                                Kind = LsoValueKind.XmlString,
                                Xml = n.Text,
                                StringIsRef = n.IsRef,
                                StringRefIndex = n.RefIndex
                            };
                        }
                    case 0x0C:
                        {
                            int u = ReadU29();
                            if ((u & 1) == 0)
                            {
                                return MakeReference(0x0C, u >> 1);
                            }
                            int len = u >> 1;
                            var buf = new byte[len];
                            Array.Copy(B, I, buf, 0, len);
                            I += len;
                            return new LsoValue { Kind = LsoValueKind.ByteArray, Bytes = buf };
                        }
                    default:
                        throw new InvalidDataException($"不支持的 AMF3 标记 0x{t:X2} (偏移 {I - 1})。");
                }
            }

            private LsoValue ReadArray()
            {
                int u = ReadU29();
                if ((u & 1) == 0)
                {
                    return MakeReference(0x09, u >> 1);
                }

                int count = u >> 1;
                int assocCount = ReadU29() >> 1;

                var value = new LsoValue
                {
                    Kind = LsoValueKind.Array,
                    Array = new List<LsoValue>(count),
                    Assoc = new List<LsoAssocEntry>(assocCount)
                };

                for (int k = 0; k < count; k++)
                {
                    value.Array.Add(ReadValue());
                }
                for (int k = 0; k < assocCount; k++)
                {
                    var key = ReadName();
                    var val = ReadValue();
                    value.Assoc.Add(new LsoAssocEntry { Key = key, Value = val });
                }
                return value;
            }

            private LsoValue ReadObject()
            {
                int u = ReadU29();
                if ((u & 1) == 0)
                {
                    return MakeReference(0x0A, u >> 1);
                }

                var obj = new LsoObject();
                int traitsExt = (u >> 1) & 1;
                if (traitsExt == 0)
                {
                    int ti = u >> 2;
                    if (ti < 0 || ti >= Traits.Count)
                    {
                        throw new InvalidDataException($"无效的对象 traits 引用 {ti} (偏移 {I - 1})。");
                    }
                    var trait = Traits[ti];
                    obj.UsesTraitsRef = true;
                    obj.TraitsRefIndex = ti;
                    obj.ClassName = trait.ClassName;
                    obj.Externalizable = trait.Externalizable;
                    obj.Dynamic = trait.Dynamic;
                    obj.SealedNames = trait.Sealed;
                }
                else
                {
                    obj.Externalizable = ((u >> 2) & 1) == 1;
                    obj.Dynamic = ((u >> 3) & 1) == 1;
                    int sealedCount = u >> 4;

                    var className = ReadName();
                    obj.ClassName = className.Text;
                    obj.ClassNameIsRef = className.IsRef;
                    obj.ClassNameRefIndex = className.RefIndex;

                    var trait = new TraitInfo
                    {
                        ClassName = className.Text,
                        ClassNameIsRef = className.IsRef,
                        ClassNameRefIndex = className.RefIndex,
                        Externalizable = obj.Externalizable,
                        Dynamic = obj.Dynamic
                    };
                    for (int k = 0; k < sealedCount; k++)
                    {
                        trait.Sealed.Add(ReadName());
                    }
                    obj.SealedNames = trait.Sealed;
                    Traits.Add(trait);
                }

                int members = obj.SealedNames != null ? obj.SealedNames.Count : 0;
                obj.SealedValues = new List<LsoValue>(members);
                for (int k = 0; k < members; k++)
                {
                    obj.SealedValues.Add(ReadValue());
                }

                if (obj.Externalizable)
                {
                    obj.ExternalizableBody = ReadValue();
                }

                if (obj.Dynamic)
                {
                    while (true)
                    {
                        var name = ReadName();
                        if (!name.IsRef && name.Text.Length == 0)
                        {
                            break;
                        }
                        obj.DynamicMembers.Add(new LsoMember { Name = name, Value = ReadValue() });
                    }
                }

                return new LsoValue { Kind = LsoValueKind.Object, Object = obj };
            }

            private static LsoValue MakeReference(byte marker, int index)
            {
                return new LsoValue { Kind = LsoValueKind.Reference, RefMarker = marker, RefIndex = index };
            }
        }

        // ---- AMF3 writer ----

        private static void WriteValue(Stream s, LsoValue v)
        {
            switch (v.Kind)
            {
                case LsoValueKind.Undefined: s.WriteByte(0x00); break;
                case LsoValueKind.Null: s.WriteByte(0x01); break;
                case LsoValueKind.Bool: s.WriteByte(v.Bool ? (byte)0x03 : (byte)0x02); break;
                case LsoValueKind.Int:
                    s.WriteByte(0x04);
                    WriteU29(s, unchecked((int)(v.Int & 0x1FFFFFFF)));
                    break;
                case LsoValueKind.Double:
                    s.WriteByte(0x05);
                    WriteDoubleBE(s, v.Double);
                    break;
                case LsoValueKind.String:
                    s.WriteByte(0x06);
                    WriteName(s, v.String, v.StringIsRef, v.StringRefIndex);
                    break;
                case LsoValueKind.Array:
                    WriteArray(s, v);
                    break;
                case LsoValueKind.Object:
                    WriteObject(s, v.Object);
                    break;
                case LsoValueKind.ByteArray:
                    {
                        s.WriteByte(0x0C);
                        var bytes = v.Bytes ?? new byte[0];
                        WriteU29(s, (bytes.Length << 1) | 1);
                        s.Write(bytes, 0, bytes.Length);
                        break;
                    }
                case LsoValueKind.Date:
                    s.WriteByte(0x08);
                    WriteU29(s, 1);
                    WriteDoubleBE(s, v.DateMilliseconds);
                    break;
                case LsoValueKind.Xml:
                    s.WriteByte(0x07);
                    WriteName(s, v.Xml, v.StringIsRef, v.StringRefIndex);
                    break;
                case LsoValueKind.XmlString:
                    s.WriteByte(0x0B);
                    WriteName(s, v.Xml, v.StringIsRef, v.StringRefIndex);
                    break;
                case LsoValueKind.Reference:
                    s.WriteByte(v.RefMarker);
                    WriteU29(s, v.RefIndex << 1);
                    break;
            }
        }

        private static void WriteArray(Stream s, LsoValue v)
        {
            s.WriteByte(0x09);
            int dense = v.Array?.Count ?? 0;
            int assoc = v.Assoc?.Count ?? 0;
            WriteU29(s, (dense << 1) | 1);
            WriteU29(s, (assoc << 1) | 1);
            if (v.Array != null)
            {
                foreach (var item in v.Array)
                {
                    WriteValue(s, item);
                }
            }
            if (v.Assoc != null)
            {
                foreach (var entry in v.Assoc)
                {
                    WriteName(s, entry.Key);
                    WriteValue(s, entry.Value);
                }
            }
        }

        private static void WriteObject(Stream s, LsoObject o)
        {
            if (o == null)
            {
                s.WriteByte(0x0A);
                WriteU29(s, 0x0B);
                WriteName(s, string.Empty, false, -1);
                WriteName(s, string.Empty, false, -1);
                return;
            }

            s.WriteByte(0x0A);
            if (o.UsesTraitsRef)
            {
                WriteU29(s, (o.TraitsRefIndex << 2) | 1);
            }
            else
            {
                int count = o.SealedNames?.Count ?? 0;
                int header = (count << 4)
                    | (o.Dynamic ? (1 << 3) : 0)
                    | (o.Externalizable ? (1 << 2) : 0)
                    | (1 << 1)
                    | 1;
                WriteU29(s, header);

                WriteName(s, o.ClassName, o.ClassNameIsRef, o.ClassNameRefIndex);
                if (o.SealedNames != null)
                {
                    foreach (var name in o.SealedNames)
                    {
                        WriteName(s, name);
                    }
                }
            }

            if (o.SealedValues != null)
            {
                foreach (var value in o.SealedValues)
                {
                    WriteValue(s, value);
                }
            }

            if (o.Externalizable)
            {
                WriteValue(s, o.ExternalizableBody);
            }

            if (o.Dynamic)
            {
                if (o.DynamicMembers != null)
                {
                    foreach (var member in o.DynamicMembers)
                    {
                        WriteName(s, member.Name);
                        WriteValue(s, member.Value);
                    }
                }
                WriteName(s, string.Empty, false, -1);
            }
        }

        private static void WriteName(Stream s, LsoName name)
        {
            WriteName(s, name.Text, name.IsRef, name.RefIndex);
        }

        private static void WriteName(Stream s, string text, bool isRef, int refIndex)
        {
            if (isRef)
            {
                WriteU29(s, refIndex << 1);
                return;
            }
            var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            WriteU29(s, (bytes.Length << 1) | 1);
            s.Write(bytes, 0, bytes.Length);
        }

        private static void WriteDoubleBE(Stream s, double value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            s.Write(bytes, 0, 8);
        }

        // ---- helpers ----

        private static int SignExtend29(int raw)
        {
            if ((raw & 0x10000000) != 0)
            {
                return raw | unchecked((int)0xE0000000);
            }
            return raw;
        }

        private static int ReadU29(byte[] b, ref int i)
        {
            int v = 0;
            for (int k = 0; k < 3; k++)
            {
                byte c = b[i++];
                v = (v << 7) | (c & 0x7F);
                if ((c & 0x80) == 0)
                {
                    return v;
                }
            }
            byte last = b[i++];
            return (v << 8) | last;
        }

        private static void WriteU29(Stream s, int value)
        {
            int u = value & 0x1FFFFFFF;
            if (u < 0x80)
            {
                s.WriteByte((byte)u);
            }
            else if (u < 0x4000)
            {
                s.WriteByte((byte)(0x80 | (u >> 7)));
                s.WriteByte((byte)(u & 0x7F));
            }
            else if (u < 0x200000)
            {
                s.WriteByte((byte)(0x80 | (u >> 14)));
                s.WriteByte((byte)(0x80 | ((u >> 7) & 0x7F)));
                s.WriteByte((byte)(u & 0x7F));
            }
            else
            {
                s.WriteByte((byte)(0x80 | (u >> 22)));
                s.WriteByte((byte)(0x80 | ((u >> 15) & 0x7F)));
                s.WriteByte((byte)(0x80 | ((u >> 8) & 0x7F)));
                s.WriteByte((byte)(u & 0xFF));
            }
        }

        private static uint ReadU32(byte[] b, ref int i)
        {
            uint v = (uint)((b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3]);
            i += 4;
            return v;
        }

        private static void WriteU32(Stream s, uint v)
        {
            s.WriteByte((byte)(v >> 24));
            s.WriteByte((byte)(v >> 16));
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)v);
        }

        private static void WriteU32Big(byte[] b, int offset, uint v)
        {
            b[offset] = (byte)(v >> 24);
            b[offset + 1] = (byte)(v >> 16);
            b[offset + 2] = (byte)(v >> 8);
            b[offset + 3] = (byte)v;
        }
    }
}
