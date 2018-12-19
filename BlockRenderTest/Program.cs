using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;

namespace BlockRenderTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length >= 3)
            {
                string MinecraftPath = args[0];
                string MinecraftJarPath = args[1];
                string OutputDirectory = args[2];

                string OptionsPath = MinecraftPath + @"\options.txt";
                string[] PackList = GetResourcePacks(OptionsPath).ToArray();
                //string[] PackList = GetResourcePacks(OptionsPath).Reverse().ToArray();
                List<ResourcePack> ResourcePacks = new List<ResourcePack>();
                foreach (string PackName in PackList)
                {
                    if(!PackName.StartsWith(@"JAR\"))
                    {
                        ResourcePacks.Add(new ResourcePack(PackName, MinecraftPath));
                    } else
                    {
                        string[] SplitJarPath = MinecraftJarPath.Split('\\');
                        ResourcePacks.Add(new ResourcePack(PackName.Split('\\')[1] + "_" + SplitJarPath[SplitJarPath.Length - 1], MinecraftJarPath));
                    }
                }

                foreach (ResourcePack Pack in ResourcePacks)
                {
                    Console.WriteLine(Pack.Name + " " + Pack.Path + " " + Pack.Type.ToString());
                }
            }
            else
            {
                Console.WriteLine("Command line arguments:");
                Console.WriteLine("1. Location of .minecraft");
                Console.WriteLine("2. Location of Minecraft .jar");
                Console.WriteLine("3. Output Directory");
            }
            Console.WriteLine();
            Console.WriteLine("Done");
            while (true)
            {
                
            }
        }

        static IEnumerable<string> GetResourcePacks(string OptionsPath)
        {
            using (StreamReader sr = new StreamReader(OptionsPath))
            {
                char[] RemoveChars = { '[', ']'};
                string Line;
                string[] LineParts;
                while(!sr.EndOfStream)
                {
                    Line = sr.ReadLine();
                    if (!string.IsNullOrEmpty((LineParts = Line.StartsWith("resourcePacks") ? Line.Split(':')[1].Trim(RemoveChars).Split(',') : null)?[0]))
                    {
                        foreach(string stringy in LineParts)
                        {
                            yield return stringy.Contains('/') ? stringy.Split('/')[1].Trim('"') : @"JAR\" + stringy.Trim('"');
                        }
                        break;
                    }
                }
            }
        }
    }

    public class ResourcePack
    {
        public string Name;
        public string Path;
        public PackType Type;
        public enum PackType {Zip, Folder, Jar, Mod};

        public ResourcePack(string PackName, string PackPath)
        {
            Name = PackName;
            Type = (Name.EndsWith(".zip") ? PackType.Zip : (Name.EndsWith(".jar") ? PackType.Jar : PackType.Folder));
            Path = GetPath(PackPath);
        }

        public ResourcePack(string PackName, string PackPath, PackType TypeInput)
        {
            PackName = Name;
            Type = TypeInput;
            Path = GetPath(PackPath);
        }

        private string GetPath(string PackPath)
        {
            switch (Type)
            {
                case PackType.Zip:
                case PackType.Folder:
                    return PackPath + @"\resourcepacks\" + Name;
                case PackType.Mod:
                    return PackPath + @"\mods\" + Name;
                case PackType.Jar:
                default:
                    return PackPath;
            }
        }
    }
}
