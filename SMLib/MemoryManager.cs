using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMLib
{
    public class MemoryManager
    {
        public Dictionary<int, List<Segment>> FreeMemory {get; set;}
        
        public MemoryManager()
        {
            FreeMemory = new Dictionary<int, List<Segment>>();

            /* init with default SM vanilla values */
            FreeMemory.Add(0x8F, new List<Segment>() { new Segment(0xE9A0, 0xFFFF) });
            FreeMemory.Add(0x83, new List<Segment>() { new Segment(0xAD70, 0xFFFF) });
            FreeMemory.Add(0xB4, new List<Segment>() { new Segment(0xF4C0, 0xFFFF) });
            FreeMemory.Add(0xA1, new List<Segment>() { new Segment(0xEBE0, 0xFFFF) });

            FreeMemory.Add(0xCE, new List<Segment>() { new Segment(0xB32D, 0xFFFF) });
            FreeMemory.Add(0xDE, new List<Segment>() { new Segment(0xD1C0, 0xFFFF) });
            FreeMemory.Add(0xDF, new List<Segment>() { new Segment(0xD4DF, 0xFFFF) });

            for(int b = 0xE0; b < 0xFF; b++)
                FreeMemory.Add(b, new List<Segment>() { new Segment(0x0000, 0xFFFF) });
        }

        public MemoryManager(byte[] romData)
        {
            FreeMemory = new Dictionary<int, List<Segment>>();
            uint startPos = 0;
            int length = 0;

            /* Scan the ROM for free space */
            for(uint p = 0; p < (uint)romData.Length; p++)
            {
                byte b = romData[p];
                if(b == 0xFF)
                {
                    if (length == 0)
                    {
                        startPos = p;
                        length = 1;
                    } else
                    {
                        length++;
                    }
                }
                else
                {
                    if(length > 7)
                    {
                        /* only allocate free blocks of 8 bytes or more */
                        uint fullAddr = Lunar.ToSNES(startPos);
                        uint bank = fullAddr >> 16;
                        uint addr = fullAddr & 0xFFFF;
                        if(FreeMemory.ContainsKey((int)bank))
                        {
                            FreeMemory[(int)bank].Add(new Segment((ushort)addr, (ushort)(addr + (length-1))));
                        }
                        else
                        {
                            FreeMemory.Add((int)bank, new List<Segment> { new Segment((ushort)addr, (ushort)(addr + (length - 1))) });
                        }
                    }
                    length = 0;
                }
            }

            if (length > 7)
            {
                /* only allocate free blocks of 8 bytes or more */
                uint fullAddr = Lunar.ToSNES(startPos);
                uint bank = fullAddr >> 16;
                uint addr = fullAddr & 0xFFFF;
                if (FreeMemory.ContainsKey((int)bank))
                {
                    FreeMemory[(int)bank].Add(new Segment((ushort)addr, (ushort)(addr + (length - 1))));
                }
                else
                {
                    FreeMemory.Add((int)bank, new List<Segment> { new Segment((ushort)addr, (ushort)(addr + (length - 1))) });
                }
            }
        }

        /* allocate anywhere */
        public uint Allocate(int size)
        {
            uint pointer = 0;
            foreach(var bank in FreeMemory.Where(f => f.Key > 0xC0).OrderBy(f => f.Key))
            {
                pointer = Allocate(bank.Key, size);
                if(pointer != 0xFFFF)
                {
                    pointer = (uint)((bank.Key << 16) + pointer);
                    return pointer;
                }
            }
            return 0;
        }

        public ushort Allocate(int bank, int size)
        {
            ushort pointer = 0xFFFF;
            foreach(var segment in FreeMemory[bank].OrderBy(s => s.Start).ToList())
            {
                if(segment.Size >= size)
                {
                    pointer = segment.Start;
                    segment.Start += (ushort)size;
                    segment.Size -= size;

                    if(segment.Size == 0)
                    {
                        FreeMemory[bank].Remove(segment);
                    }
                    break;
                }
            }
            if(pointer == 0xFFFF && bank < 0xC0)
            {
                throw new Exception(String.Format("Can't allocate more memory in bank: {0:X}", bank));
            }
            return pointer;
        }

        public void Free(int bank, ushort address, int size)
        {
            ushort start = address;
            ushort end = (ushort)(address + (ushort)size);

            ///* check if this address falls within an existing bank first */
            //if (FreeMemory.ContainsKey(bank))
            //{
            //    foreach (var segment in FreeMemory[bank].ToList())
            //    {
            //        if ((start >= (segment.Start - 1) && start <= (segment.End + 1)) || (end >= (segment.Start - 1) && end <= (segment.End - 1)) || (start < segment.Start && end > segment.End))
            //        {
            //            /* the freed segment falls over, within or next to an existing segment */

            //            /* make sure we don't extend this segment into other segments */
            //            foreach (var testSegment in FreeMemory[bank].Where(x => x != segment).ToList())
            //            {
            //                if (testSegment.Start > start && testSegment.Start <= end)
            //                {
            //                    /* move endpoint to include this segment */
            //                    end = testSegment.End;
            //                    FreeMemory[bank].Remove(testSegment);
            //                }
            //            }

            //            if (start < segment.Start)
            //                segment.Start = start;

            //            if (end > segment.End)
            //                segment.End = end;

            //            segment.Size = segment.End - segment.Start;
            //            return;
            //        }
            //    }
            //}

            /* if we're here no existing block could be extended */
            if(!FreeMemory.ContainsKey(bank))
            {
                FreeMemory.Add(bank, new List<Segment>());
            }

            var newSegment = new Segment(start, end);
            FreeMemory[bank].Add(newSegment);

            mergeSegments(bank);
        }

        private void mergeSegments(int bank)
        {
            foreach(var segment in FreeMemory[bank].ToList())
            {
                if (!FreeMemory[bank].Contains(segment))
                    continue;

                var start = segment.Start;
                var end = segment.End;
                /* find any segment that overlaps with this segment */
                foreach(var testSegment in FreeMemory[bank].Where(f => f != segment).ToList())
                {
                    if((start >= (testSegment.Start - 1) && start <= (testSegment.End + 1)) || (end >= (testSegment.Start - 1) && end <= (testSegment.End - 1)) || (start < testSegment.Start && end > testSegment.End))
                    {
                        /* extend the current segment and remove testsegment */
                        if(testSegment.Start < start)
                        {
                            start = testSegment.Start;
                        }
                        
                        if(testSegment.End > end)
                        {
                            end = testSegment.End;
                        }
                        FreeMemory[bank].Remove(testSegment);
                    }
                }
                segment.Start = start;
                segment.End = end;
                segment.Size = end - start;
            }
        }
    }

    public class Segment
    {
        public ushort Start {get; set;}
        public ushort End {get; set;}
        public int Size {get; set;}

        public Segment(ushort start, ushort end)
        {
            Start = start;
            End = end;
            Size = End - Start;
        }
    }
}
