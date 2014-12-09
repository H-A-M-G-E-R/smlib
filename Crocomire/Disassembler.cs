using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crocomire
{
    class Disassembler
    {
        private int acc = 0;
        private int index = 0;

        // REP = 0xC2 (2)
        // SEP = 0xE2 (2)

        private byte[] twoByte = new byte[] {   0x61, 0x63, 0x65, 0x67, 0x71, 0x72, 0x73, 0x75, 0x77, 0x21, 0x23, 
                                                0x25, 0x27, 0x31, 0x32, 0x33, 0x35, 0x37, 0x06, 0x16, 0x90, 0xB0, 
                                                0xF0, 0x24, 0x34, 0x30, 0xD0, 0x10, 0x80, 0x50, 0x70, 0xC1, 0xC3,
                                                0xC5, 0xC7, 0xD1, 0xD2, 0xD3, 0xD5, 0xD7, 0xE4, 0xC4, 0xC6, 0xD6,
                                                0x41, 0x43, 0x45, 0x47, 0x51, 0x52, 0x53, 0x55, 0x57, 0xE6, 0xF6,
                                                0xA1, 0xA3, 0xA5, 0xA7, 0xB1, 0xB2, 0xB3, 0xB5, 0xB7, 0xA6, 0xB6,
                                                0xA4, 0xB4, 0x46, 0x56, 0x01, 0x03, 0x05, 0x07, 0x11, 0x12, 0x13,
                                                0x15, 0x17, 0xD4, 0x26, 0x36, 0x66, 0x76, 0xE1, 0xE3, 0xE5, 0xE7,
                                                0xF1, 0xF2, 0xF3, 0xF5, 0xF7, 0x81, 0x83, 0x85, 0x87, 0x91, 0x92,
                                                0x93, 0x95, 0x97, 0x86, 0x96, 0x84, 0x94, 0x64, 0x74, 0x14, 0x04,
                                                0x42};
        
        private byte[] threeByte = new byte[] { 0x79, 0x7D, 0x2D, 0x39, 0x3D, 0x0E, 0x1E, 0x2C, 0x3C, 0x82, 0xCD,
                                                0xD9, 0xDD, 0xEC, 0xCC, 0xCE, 0xDE, 0x4D, 0x59, 0x5D, 0xEE, 0xFE,
                                                0x4C, 0x6C, 0x7C, 0xDC, 0x20, 0xFC, 0xAD, 0xB9, 0xBD, 0xAE, 0xBE,
                                                0xAC, 0xBC, 0x4E, 0x5E, 0x54, 0x44, 0x0D, 0x19, 0x1D, 0xF4, 0x62,
                                                0x2E, 0x3E, 0x6E, 0x7E, 0xED, 0xF9, 0xFD, 0x8D, 0x99, 0x9D, 0x8E,
                                                0x8C, 0x9C, 0x9E, 0x1C, 0x0C};
        
        private byte[] fourByte = new byte[] {  0x7F, 0x2F, 0x3F, 0xCF, 0xDF, 0x4F, 0x5F, 0x5C, 0x22, 0xAF, 0xBF,
                                                0x0F, 0x1F, 0xEF, 0xFF, 0x8F, 0x9F};


        private byte[] twoByteAcc = new byte[] { 0x69, 0x29, 0x89, 0xC9, 0x49, 0xA9, 0x09, 0xE9 };

        private byte[] twoByteIdx = new byte[] { 0xE0, 0xC0, 0xA2, 0xA0 };

        public byte[] Disassemble(byte[] code)
        {
            int l;
            
            index = 1;
            acc = 1;

            for(l = 0; l < code.Length; l++)
            {
                var o = code[l];
                
                if(o == 0xC2)
                {
                    // REP (clear bits for 16 bit registers)
                    if((code[l+1] & 0x10) > 0)
                    {
                        index = 1;
                    }
                    if((code[l+1] & 0x20) > 0)
                    {
                        acc = 1;
                    }
                    l++;
                }
                else if(o == 0xE2)
                {
                    // SEP (clear bits for 16 bit registers)
                    if ((code[l + 1] & 0x10) > 0)
                    {
                        index = 0;
                    }
                    if ((code[l + 1] & 0x20) > 0)
                    {
                        acc = 0;
                    }
                    l++;
                }
                else if(twoByteAcc.Contains(o))
                {
                    l++;
                    if (acc == 1)
                        l++;
                }
                else if(twoByteIdx.Contains(o))
                {
                    l++;
                    if (index == 1)
                        l++;
                }
                else if(twoByte.Contains(o))
                {
                    l++;
                }
                else if(threeByte.Contains(o))
                {
                    l += 2;
                }
                else if(fourByte.Contains(o))
                {
                    l += 3;
                }
                else if(o == 0x60)
                {
                    break;
                }
            }

            byte[] dest = new byte[l + 1];
            Array.Copy(code, dest, l + 1);
            return dest;
        }
    }
}
