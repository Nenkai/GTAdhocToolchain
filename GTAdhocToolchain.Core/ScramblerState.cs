//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

using PDTools.Crypto;

namespace GTAdhocToolchain.Core;

public class ScramblerState
{
    public static ChaCha20 CreateFromHash(byte[] hash)
    {
        /* game:
        unsafe
        {
            fixed (byte* pHash = hash)
            {
                var ogBytes = Avx.LoadVector128((int*)pHash);
                var xmm1 = Avx.Shuffle(ogBytes, 0xEE); // vpshufd
                ogBytes = Avx.Xor(ogBytes, xmm1);
                xmm1 = Avx.Shuffle(ogBytes, 0x55);
                var final = Avx.Xor(ogBytes, xmm1);
            
                uint seed = final.AsUInt32().GetElement(0);
                return Create(seed);
            }
        }
        */

        Span<uint> hashInts = MemoryMarshal.Cast<byte, uint>(hash);
        uint[] vec = new uint[4];
        for (int i = 0; i < 4; i++)
            vec[i] = hashInts[i];

        Span<uint> shuf = [vec[2], vec[3], vec[2], vec[3]];
        for (int i = 0; i < 4; i++)
            vec[i] ^= shuf[i];

        uint[] vec1 = [vec[1], vec[1], vec[1], vec[1]];
        for (int i = 0; i < 4; i++)
            vec[i] ^= vec1[i];

        return Create(vec[0]);
    }

    static uint Mix(uint x)
    {
        uint t = x ^ (x << 11);
        return t ^ (t >> 8);
    }

    private static ChaCha20 Create(uint seed)
    {
        Span<uint> init = stackalloc uint[4];
        while (init[0] == 0 && init[1] == 0 && init[2] == 0 && init[3] == 0)
        {
            uint s = seed;
            for (int i = 0; i < 4; i++)
            {
                s = s * 0x6C078965 + 1;
                s ^= s << 13;
                s ^= s >> 17;
                init[i] = s;
            }

            init[0] ^= 0x75BED14;
            init[1] ^= 0xCD82D08F;
            init[2] ^= 0xAA705DD7;
            init[3] ^= 0x2D6A657;
        }

        uint[] key = new uint[8];
        uint[] iv = new uint[3];
        uint[] unk = new uint[4];

        uint[] p = new uint[12];

        uint c0 = Mix(init[0]) ^ init[3];
        p[0]    = c0 ^ (init[3] >> 19);

        uint c1 = Mix(init[1]);
        key[0]  = (c0 >> 19) ^ c1;
        p[1]    = p[0] ^ key[0];

        c0      = Mix(init[2]) ^ p[1];
        p[2]    = c0 ^ ((p[0] ^ c1) >> 19);
        key[1]  = p[2] ^ key[0];

        for (int i = 3; i <= 11; i++)
        {
            uint input = i < 4 ? init[i] : p[i - 4];

            if (i % 2 == 1)
            {
                c1   = Mix(input) ^ p[i - 1];
                p[i] = c1 ^ (c0 >> 19);
            }
            else
            {
                c0   = Mix(input) ^ p[i - 1];
                p[i] = c0 ^ (c1 >> 19);
            }

            if (i <= 8)
                key[i - 1] = p[i] ^ key[i - 2];
        }

        unk[0] = p[8];
        unk[1] = p[9];
        unk[2] = p[10];
        unk[3] = p[11];

        iv[0] = p[9] ^ key[7];
        iv[1] = iv[0] ^ p[10];
        iv[2] = iv[1] ^ p[11];

        return new ChaCha20(
            MemoryMarshal.Cast<uint, byte>(key).ToArray(),
            MemoryMarshal.Cast<uint, byte>(iv).ToArray(),
            0);
    }
}