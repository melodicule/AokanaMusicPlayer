using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AokanaMusicPlayer
{
    class Music
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public int LoopFrom { get; set; }

        public Music(string name, string fileName, int loopFrom)
        {
            Name = name;
            FileName = fileName;
            LoopFrom = loopFrom;
        }

        public Music()
        {
            Name = string.Empty;
            FileName = string.Empty;
            LoopFrom = 0;
        }
          
    }
}
