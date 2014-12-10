using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crocomire
{
    class MemoryManager
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
        }

        public ushort Allocate(int bank, int size)
        {
            ushort pointer = 0;
            foreach(var segment in FreeMemory[bank].ToList())
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
            return pointer;
        }

        public void Free(int bank, ushort address, int size)
        {
            ushort start = address;
            ushort end = (ushort)(address + (ushort)size);

            /* check if this address falls within an existing bank first */
            foreach(var segment in FreeMemory[bank].ToList())
            {
                if((start >= (segment.Start - 1) && start <= (segment.End + 1)) || (end >= (segment.Start -1) && end <= (segment.End -1)) || (start < segment.Start && end > segment.End))
                {
                    /* the freed segment falls over, within or next to an existing segment */

                    /* make sure we don't extend this segment into other segments */
                    foreach(var testSegment in FreeMemory[bank].Where(x => x != segment).ToList())
                    {
                        if(testSegment.Start <= end)
                        {
                            /* move endpoint to include this segment */
                            end = testSegment.End;
                            FreeMemory[bank].Remove(testSegment);
                        }
                    }

                    if (start < segment.Start)
                        segment.Start = start;

                    if (end > segment.End)
                        segment.End = end;

                    segment.Size = segment.End - segment.Start;
                    return;
                }
            }

            /* if we're here no existing block could be extended */
            if(!FreeMemory.ContainsKey(bank))
            {
                FreeMemory.Add(bank, new List<Segment>());
            }

            var newSegment = new Segment(start, end);
            FreeMemory[bank].Add(newSegment);
        }
    }

    class Segment
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
