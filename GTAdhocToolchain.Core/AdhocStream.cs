//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using PDTools.Crypto;

using Syroot.BinaryData;
using Syroot.BinaryData.Core;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core;

public class AdhocStream : Stream
{
    public override bool CanRead => BaseStream.CanRead;

    public override bool CanSeek => BaseStream.CanSeek;

    public override bool CanWrite => BaseStream.CanWrite;

    public override long Length => BaseStream.Length;

    public override long Position { get => BaseStream.Position; set => BaseStream.Position = value; }

    public Encoding Encoding { get; set; } = Encoding.UTF8;

    public bool BigEndian { get; set; }

    // V12 = GT5-Sport
    // V14 = GT7
    // V15 = GT7 1.29+, encrypted
    public AdhocVersion Version { get; set; }

    public List<AdhocSymbol> Symbols { get; set; } = [];

    public ChaCha20 ChachaScramblerState { get; private set; }

    private MD5 _currentScriptMD5 { get; set; }
    private MD5 _compiledFileMD5 { get; set; }
    public Stream BaseStream { get; }
    private long _cryptStartOffset { get; set; }

    static AdhocStream()
    {
        // Needed for EUC-JP encoding
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public AdhocStream(Stream baseStream, AdhocVersion version)
    {
        BaseStream = baseStream;
        Version = version;


        /* In GT4, the encoding for the compiler is set to EUC-JP
         * "ぐわあああ\n" in boot/CheckRoot.ad
         * 
         * So set the encoding to it if it's earlier than EUC-JP 
         * 12/09/2025: Removed, as GT4's photo_shoot/SteeringDialog.ad has a " °" in UTF-8
         */
        //if (Version.VersionNumber < 10)
        //    Encoding = Encoding.GetEncoding("EUC-JP");
    }

    public override void Flush()
    {
        BaseStream.Flush();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return BaseStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        BaseStream.SetLength(value);
    }

    public override int Read(Span<byte> buffer)
    {
        return base.Read(buffer);
    }

    public override int ReadByte()
    {
        Span<byte> span = stackalloc byte[1];
        ReadExactly(span);
        return span[0];
    }

    public byte Read1Byte()
    {
        Span<byte> span = stackalloc byte[1];
        Read(span);
        return span[0];
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = BaseStream.Read(buffer, offset, count);
        if (ChachaScramblerState != null)
            ChachaScramblerState.DecryptBytes(buffer.AsSpan(offset), count, (ulong)(Position - (_cryptStartOffset + count))); // Header + hash size

        return read;
    }

    public sbyte ReadSByte()
    {
        Span<byte> data = stackalloc byte[1];
        ReadExactly(data);
        return (sbyte)data[0];
    }

    public bool ReadBoolean()
    {
        Span<byte> data = stackalloc byte[1];
        ReadExactly(data);
        return data[0] != 0;
    }

    public short ReadInt16()
    {
        Span<byte> data = stackalloc byte[2];
        ReadExactly(data);
        return BigEndian ? BinaryPrimitives.ReadInt16BigEndian(data) : BinaryPrimitives.ReadInt16LittleEndian(data);
    }

    public ushort ReadUInt16()
    {
        Span<byte> data = stackalloc byte[2];
        ReadExactly(data);
        return BigEndian ? BinaryPrimitives.ReadUInt16BigEndian(data) : BinaryPrimitives.ReadUInt16LittleEndian(data);
    }

    public int ReadInt32()
    {
        Span<byte> data = stackalloc byte[4];
        ReadExactly(data);
        return BigEndian ? BinaryPrimitives.ReadInt32BigEndian(data) : BinaryPrimitives.ReadInt32LittleEndian(data);
    }

    public uint ReadUInt32()
    {
        Span<byte> data = stackalloc byte[4];
        ReadExactly(data);
        return BigEndian ? BinaryPrimitives.ReadUInt32BigEndian(data) : BinaryPrimitives.ReadUInt32LittleEndian(data);
    }

    public long ReadInt64()
    {
        Span<byte> data = stackalloc byte[8];
        ReadExactly(data);
        return BigEndian ? BinaryPrimitives.ReadInt64BigEndian(data) : BinaryPrimitives.ReadInt64LittleEndian(data);
    }

    public ulong ReadUInt64()
    {
        Span<byte> data = stackalloc byte[8];
        ReadExactly(data);
        return BigEndian ? BinaryPrimitives.ReadUInt64BigEndian(data) : BinaryPrimitives.ReadUInt64LittleEndian(data);
    }

    public float ReadSingle()
    {
        Span<byte> data = stackalloc byte[4];
        ReadExactly(data);
        return BigEndian ? BinaryPrimitives.ReadSingleBigEndian(data) : BinaryPrimitives.ReadSingleLittleEndian(data);
    }

    public double ReadDouble()
    {
        Span<byte> data = stackalloc byte[8];
        ReadExactly(data);
        return BigEndian ? BinaryPrimitives.ReadDoubleBigEndian(data) : BinaryPrimitives.ReadDoubleLittleEndian(data);
    }

    public List<AdhocSymbol> ReadSymbols()
    {
        uint symbCount = ReadUInt32();
        List<AdhocSymbol> list = new List<AdhocSymbol>((int)symbCount);

        for (int i = 0; i < symbCount; i++)
        {
            AdhocSymbol symbol = ReadSymbol();
            list.Add(symbol);
        }

        return list;
    }

    public AdhocSymbol ReadSymbol()
    {
        if (Version.VersionNumber >= 13)
        {
            bool hasIndex = ReadByte() != 0;
            if (hasIndex)
            {
                Position -= 1;
                uint symbolTableIdx = (uint)DecodeBitsAndAdvance();
                var newSymbol = Symbols[(int)symbolTableIdx - 1];
                return newSymbol;
            }
            else
            {
                int strLen = (int)DecodeBitsAndAdvance();

                var strBytes = new byte[strLen];
                ReadExactly(strBytes);
                var symbStr = Encoding.UTF8.GetString(strBytes);

                var newSymbol = new AdhocSymbol(symbStr);
                Symbols.Add(newSymbol);
                return newSymbol;
            }
        }
        else if (Version.VersionNumber >= 9)
        {
            uint symbolTableIdx = (uint)DecodeBitsAndAdvance();
            return Symbols[(int)symbolTableIdx];
        }
        else
        {
            short strLen = ReadInt16();
            var strBytes = new byte[strLen];
            ReadExactly(strBytes);
            var symbStr = Encoding.UTF8.GetString(strBytes);
            return new AdhocSymbol(symbStr);
        }
    }

    public void Reset()
    {
        Symbols.Clear();
    }

    public void ReadSymbolTable()
    {
        uint entryCount = (uint)DecodeBitsAndAdvance();
        Symbols = new List<AdhocSymbol>((int)entryCount);
        for (var i = 0; i < entryCount; i++)
        {
            int strLen = (int)DecodeBitsAndAdvance();

            var strBytes = new byte[strLen];
            ReadExactly(strBytes);
            Symbols.Add(new AdhocSymbol(Encoding.GetString(strBytes)));
        }
    }

    public ulong DecodeBitsAndAdvance()
    {
        ulong value = (ulong)ReadByte();
        ulong mask = 0x80;

        while ((value & mask) != 0)
        {
            value = ((value - mask) << 8) | ((byte)this.ReadByte());
            mask <<= 7;
        }
        return value;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _currentScriptMD5?.TransformBlock(buffer, offset, count, null, 0);
        _compiledFileMD5?.TransformBlock(buffer, offset, count, null, 0);
        BaseStream.Write(buffer, offset, count);
    }

    public override void WriteByte(byte value)
    {
        _currentScriptMD5?.TransformBlock([value], 0, 1, null, 0);
        _compiledFileMD5?.TransformBlock([value], 0, 1, null, 0);
        BaseStream.WriteByte(value);
    }

    public void WriteBoolean(bool value)
    {
        Span<byte> data = [(byte)(value ? 1 : 0)];
        Write(data);
    }

    public void WriteSByte(sbyte value)
    {
        Span<byte> data = [(byte)value];
        Write(data);
    }

    public void WriteInt16(short value)
    {
        Span<byte> data = stackalloc byte[2];
        if (BigEndian)
            BinaryPrimitives.WriteInt16BigEndian(data, value);
        else
            BinaryPrimitives.WriteInt16LittleEndian(data, value);

        Write(data);
    }

    public void WriteUInt16(ushort value)
    {
        Span<byte> data = stackalloc byte[2];
        if (BigEndian)
            BinaryPrimitives.WriteUInt16BigEndian(data, value);
        else
            BinaryPrimitives.WriteUInt16LittleEndian(data, value);

        Write(data);
    }

    public void WriteInt32(int value)
    {
        Span<byte> data = stackalloc byte[4];
        if (BigEndian)
            BinaryPrimitives.WriteInt32BigEndian(data, value);
        else
            BinaryPrimitives.WriteInt32LittleEndian(data, value);

        Write(data);
    }

    public void WriteUInt32(uint value)
    {
        Span<byte> data = stackalloc byte[4];
        if (BigEndian)
            BinaryPrimitives.WriteUInt32BigEndian(data, value);
        else
            BinaryPrimitives.WriteUInt32LittleEndian(data, value);

        Write(data);
    }

    public void WriteInt64(long value)
    {
        Span<byte> data = stackalloc byte[8];
        if (BigEndian)
            BinaryPrimitives.WriteInt64BigEndian(data, value);
        else
            BinaryPrimitives.WriteInt64LittleEndian(data, value);

        Write(data);
    }

    public void WriteUInt64(ulong value)
    {
        Span<byte> data = stackalloc byte[8];
        if (BigEndian)
            BinaryPrimitives.WriteUInt64BigEndian(data, value);
        else
            BinaryPrimitives.WriteUInt64LittleEndian(data, value);

        Write(data);
    }

    public void WriteSingle(float value)
    {
        Span<byte> data = stackalloc byte[4];
        if (BigEndian)
            BinaryPrimitives.WriteSingleBigEndian(data, value);
        else
            BinaryPrimitives.WriteSingleLittleEndian(data, value);

        Write(data);
    }

    public void WriteDouble(double value)
    {
        Span<byte> data = stackalloc byte[8];
        if (BigEndian)
            BinaryPrimitives.WriteDoubleBigEndian(data, value);
        else
            BinaryPrimitives.WriteDoubleLittleEndian(data, value);

        Write(data);
    }

    public void WriteSymbols(IEnumerable<AdhocSymbol> symbols)
    {
        WriteInt32(symbols.Count());
        foreach (var symb in symbols)
            WriteSymbol(symb);
    }

    public void WriteSymbol(AdhocSymbol symbol)
    {
        if (Version.VersionNumber >= 13)
        {
            var registeredSymbol = Symbols.Find(e => e.Name == symbol.Name);
            if (registeredSymbol != null)
            {
                WriteVarInt(registeredSymbol.Id);
            }
            else
            {
                Symbols.Add(new AdhocSymbol(Symbols.Count + 1, symbol.Name));
                WriteByte(0);
                WriteVarString(symbol.Name);
            }
        }
        else if (Version.VersionNumber >= 9)
        {
            WriteVarInt(symbol.Id);
        }
        else
        {
            WriteInt16((short)Encoding.GetByteCount(symbol.Name));
            Write(Encoding.GetBytes(symbol.Name));
        }


    }

    public void WriteVarString(string str, bool asUtf8 = true)
    {
        // HACK: Hex sequences may not be only hex escapes, they may be incorrectly written
        // Best way to handle it for now (?) is just to treat anything with a hex escaped character as full ascii

        if (asUtf8)
        {
            // Must convert, has some utf8 chars, i.e japanese
            WriteVarInt(Encoding.UTF8.GetByteCount(str));
            Write(Encoding.UTF8.GetBytes(str));
        }
        else
        {
            // Non UTF8 operation, incase the string is a escaped byte array as string
            WriteVarInt(str.Length);
            byte[] data = new byte[str.Length];
            for (int i = 0; i < str.Length; i++)
                data[i] = (byte)str[i];

            this.Write(data);
        }
    }




    public bool IsAscii(string str)
    {
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] < 0 || str[i] > 0xFF)
                return false;
        }
        return true;
    }

    // Credits xfileFin
    public void WriteVarInt(int value)
    {
        if ((value & 0xFFFFFF80) == 0)
        {
            WriteByte((byte)value);
            return;
        }

        var bytesToWrite = 1;
        uint mask = 0x80;
        long retVal = 0;
        do
        {
            retVal = (retVal + mask) << 8;
            mask <<= 7;
            bytesToWrite++;
        } while ((value & -mask) > 0);

        var finalValue = retVal | value;
        for (var i = bytesToWrite; i > 0; i--)
            WriteByte((byte)(finalValue >> (i - 1) * 8));
    }

    public static byte[] EncodeAndAdvance(uint value)
    {
        uint mask = 0x80;
        Span<byte> buffer = Array.Empty<byte>();

        if (value <= 0x7F)
        {
            return new[] { (byte)value };
        }
        else if (value <= 0x3FFF)
        {
            Span<byte> tempBuf = BitConverter.GetBytes(value).AsSpan();
            tempBuf.Reverse();
            buffer = tempBuf.Slice(2, 2);
        }
        else if (value <= 0x1FFFFF)
        {
            Span<byte> tempBuf = BitConverter.GetBytes(value).AsSpan();
            tempBuf.Reverse();
            buffer = tempBuf.Slice(1, 3);
        }
        else if (value <= 0xFFFFFFF)
        {
            buffer = BitConverter.GetBytes(value);
            buffer.Reverse();
        }
        else if (value <= 0xFFFFFFFF)
        {
            buffer = BitConverter.GetBytes(value);
            buffer.Reverse();
            buffer = new byte[] { 0, buffer[0], buffer[1], buffer[2], buffer[3] };
        }
        else
            throw new Exception("????");

        for (int i = 1; i < buffer.Length; i++)
        {
            buffer[0] += (byte)mask;
            mask >>= 1;
        }

        return buffer.ToArray();
    }

    public void InitScrambler(byte[] hash)
    {
        if (hash.Length != 0x10)
            throw new Exception($"Expected hash to be 0x10 in length, got 0x{hash.Length:X}");

        ChachaScramblerState = ScramblerState.CreateFromHash(hash);
        _cryptStartOffset = Position;
    }

    public void StartCurrentScriptMD5()
    {
        _currentScriptMD5 = MD5.Create();
        _currentScriptMD5.Initialize();
    }

    public void StartCompiledFileMD5()
    {
        _compiledFileMD5 = MD5.Create();
        _compiledFileMD5.Initialize();
    }

    public byte[] FinishCurrentScriptMD5()
    {
        _currentScriptMD5.TransformFinalBlock([], 0, 0);
        byte[] hash = _currentScriptMD5.Hash;

        _currentScriptMD5.Dispose();
        _currentScriptMD5 = null;

        return hash;
    }

    public byte[] FinishCompiledFileMD5()
    {
        _compiledFileMD5.TransformFinalBlock([], 0, 0);
        byte[] hash = _compiledFileMD5.Hash;

        _compiledFileMD5.Dispose();
        _compiledFileMD5 = null;
        return hash;
    }
}
