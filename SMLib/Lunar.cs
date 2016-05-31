using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace SMLib
{
    public class Lunar
    {
        [DllImport("Lunar Compress.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool LunarOpenFile(string FileName, uint FileMode);

        [DllImport("Lunar Compress.dll", CallingConvention = CallingConvention.StdCall)]
        private unsafe static extern byte* LunarOpenRAMFile(void* data, uint FileMode, uint size);

        [DllImport("Lunar Compress.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool LunarCloseFile();

        [DllImport("Lunar Compress.dll", CallingConvention = CallingConvention.StdCall)]
        private unsafe static extern uint LunarDecompress(void* destination, uint AddressToStart, uint MaxDataSize, uint Format, uint Format2, out uint LastRomPosition);

        [DllImport("Lunar Compress.dll", CallingConvention = CallingConvention.StdCall)]
        private unsafe static extern uint LunarRecompress(void* source, void* destination, uint DataSize, uint MaxDataSize, uint Format, uint Format2);

        [DllImport("Lunar Compress.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern uint LunarExpandROM(uint Mbits);

        [DllImport("Lunar Compress.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern uint LunarSNEStoPC(uint Pointer, uint ROMType, uint Header);

        [DllImport("Lunar Compress.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern uint LunarPCtoSNES(uint Pointer, uint ROMType, uint Header);

        public static void ExpandROM(string fileName, uint size)
        {
            LunarOpenFile(fileName, 1);
            LunarExpandROM(size);
            LunarCloseFile();
        }

        public static uint ToPC(uint pointer)
        {
            return LunarSNEStoPC(pointer, 1, 0);
        }

        public static uint ToSNES(uint pointer)
        {
            return 0x800000 + LunarPCtoSNES(pointer, 1, 0);
        }

        public static byte[] Compress(byte[] data)
        {
            byte[] dest = new byte[0x10000];
            uint size;

            unsafe
            {
                fixed (byte* destPtr = dest)
                {
                    fixed(byte* sourcePtr = data)
                    {
                        size = LunarRecompress(sourcePtr, destPtr, (uint)data.LongLength, 0x10000, 4, 0);
                    }
                }
            }

            byte[] compressed = new byte[size];
            Array.Copy(dest, compressed, size);
            return compressed;
        }

        public static byte[] Decompress(byte[] data)
        {
            uint size;
            byte[] dest = new byte[0x10000];

            unsafe
            {
                byte* ret;
                uint LastRomPosition;
                fixed (byte* dataPtr = data)
                {
                    ret = LunarOpenRAMFile(dataPtr, 0, (uint)data.Length);
                    if((int)ret == 0)
                    {
                        return new byte[0];
                    }

                    fixed(byte* destPtr = dest)
                    {
                        size = LunarDecompress(destPtr, 0, 0x10000, 4, 0, out LastRomPosition);
                        LunarCloseFile();
                    }
                }
            }

            byte[] decompressed = new byte[size];
            Array.Copy(dest, decompressed, size);
            return decompressed;
        }
    }
}
