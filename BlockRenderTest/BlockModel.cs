using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockRenderTest
{
    public class BlockModel
    {
        public string Name;
        public string Parent;
        public Dictionary<string, Image> Textures;
    }
}
