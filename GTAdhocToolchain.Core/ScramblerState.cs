using PDTools.Crypto;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Intrinsics;

namespace GTAdhocToolchain.Core
{
    public class ScramblerState
    {
        public static ChaCha20 CreateFromHash(byte[] hash)
        {
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
                init[0] = (0x6C078965 * seed + 1) ^ ((0x6C078965 * seed + 1) << 13) ^ (((0x6C078965 * seed + 1) ^ ((0x6C078965 * seed + 1) << 13)) >> 17);
                init[1] = (0x6C078965 * init[0] + 1) ^ ((0x6C078965 * init[0] + 1) << 13) ^ (((0x6C078965 * init[0] + 1) ^ ((0x6C078965 * init[0] + 1) << 13)) >> 17);
                init[2] = (0x6C078965 * init[1] + 1) ^ ((0x6C078965 * init[1] + 1) << 13) ^ (((0x6C078965 * init[1] + 1) ^ (uint)((0x6C078965 * init[1] + 1) << 13)) >> 17);
                init[3] = (0x6C078965 * init[2] + 1) ^ ((0x6C078965 * init[2] + 1) << 13) ^ (((0x6C078965 * init[2] + 1) ^ (uint)((0x6C078965 * init[2] + 1) << 13)) >> 17);

                init[0] ^= 0x75BED14;
                init[1] ^= 0xCD82D08F;
                init[2] ^= 0xAA705DD7;
                init[3] ^= 0x2D6A657;
            }

            uint[] key = new uint[8];
            uint[] iv = new uint[3];
            uint[] unk = new uint[4];

            uint c0 = Mix(init[0]) ^ init[3];
            uint v20 = c0 ^ (init[3] >> 19);

            uint c1 = Mix(init[1]);
            key[0] = (c0 >> 19) ^ c1;

            uint v22 = v20 ^ key[0];

            c0 = Mix(init[2]) ^ v22;
            uint v24 = c0 ^ ((v20 ^ c1) >> 19);
            key[1] = v24 ^ key[0];

            c1 = Mix(init[3]) ^ v24;
            uint v26 = c1 ^ (c0 >> 19);
            key[2] = v26 ^ key[1];

            c0 = Mix(v20) ^ v26;
            uint v28 = c0 ^ (c1 >> 19);
            key[3] = v28 ^ key[2];

            c1 = Mix(v22) ^ v28;
            uint v30 = c1 ^ (c0 >> 19);
            key[4] = v30 ^ key[3];

            c0 = Mix(v24) ^ v30;
            uint v32 = c0 ^ (c1 >> 19);
            key[5] = v32 ^ key[4];

            c1 = Mix(v26) ^ v32;
            uint v34 = c1 ^ (c0 >> 19);
            key[6] = v34 ^ key[5];

            c0 = Mix(v28) ^ v34;
            uint v36 = c0 ^ (c1 >> 19);
            key[7] = v36 ^ key[6];

            c1 = Mix(v30) ^ v36;
            uint v39 = c1 ^ (c0 >> 19);

            unk[0] = v36;
            unk[1] = v39;

            c0 = Mix(v32) ^ v39;
            iv[0] = v39 ^ key[7];
            uint v42 = c0 ^ (c1 >> 19);

            unk[2] = v42;

            c1 = Mix(v34) ^ v42;
            iv[1] = iv[0] ^ v42;
            uint v45 = c1 ^ (c0 >> 19);

            unk[3] = v45;

            iv[2] = iv[1] ^ v45;

            return new ChaCha20(
                MemoryMarshal.Cast<uint, byte>(key).ToArray(),
                MemoryMarshal.Cast<uint, byte>(iv).ToArray(),
                0);
        }
    }
}
