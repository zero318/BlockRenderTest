using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockRenderTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                foreach(Object argument in args)
                {
                    Console.WriteLine(argument);
                }
            } else
            {
                Console.WriteLine("Command line arguments:");
                Console.WriteLine("1. Location of .minecraft");
                Console.WriteLine("2. Location of Minecraft .jar");
            }
            while(1==1)
            {

            }
        }
    }
}
