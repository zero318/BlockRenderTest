using System;
using System.Collections;
using System.Collections.Generic;
//using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
//using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlockRenderTest
{
    class Program
    {
        /*
        This project is a command line application! That means instead of screwing with a GUI, it's
        meant to be used as part of a batch script. In order to do that, the program will have arguments
        passed to it. Once finished, it will hopefully be using somewhat like this:

        BlockRenderTest "F:\My Minecraft Expansion\.minecraft" "C:\Users\zero318\AppData\Roaming\.minecraft\versions\18w50a\18w50a.jar" "F:\My Minecraft Expansion\_BlockRenderTempDirectory"

        What that does in a command line environment is feed external data into the program. Notice how
        the three things are separated with quotes and spaces? That's how the program is able to tell each
        argument apart.
        */
        static void Main(string[] args) //<-- In C#, the program always starts at Main()
        {
            if (args.Length >= 3)
            {
                string MinecraftPath = args[0];     //"F:\My Minecraft Expansion\.minecraft"
                string MinecraftJarPath = args[1];  //"C:\Users\zero318\AppData\Roaming\.minecraft\versions\18w50a\18w50a.jar"
                string OutputDirectory = args[2];   //"F:\My Minecraft Expansion\_BlockRenderTempDirectory"

                string OptionsPath = MinecraftPath + @"\options.txt";
                /*
                This function is *really* important! Rather than just blindly load every resource pack in
                the folder (because some people like myself have 50 or something stupid), this function loads
                the list of currently enabled resource packs from Minecraft itself. :D

                The code of the 
                */
                string[] PackList = ResourcePack.GetResourcePacks(OptionsPath).ToArray();
                
                
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
                List<Block> Blocks = new List<Block>(); //This list has to be declared outside of the using block so that it can be referenced later
                using (ZipArchive MinecraftJar = ZipFile.OpenRead(MinecraftJarPath))
                {
                    IEnumerable<ZipArchiveEntry> BlockStateEntries = MinecraftJar.Entries.Where(Entry => (Entry.Name.EndsWith(".json")) && (Entry.FullName.EndsWith(@"minecraft/blockstates/" + Entry.Name)));
                    foreach (ZipArchiveEntry Entry in BlockStateEntries)
                    {
                        /*
                        This whole section is just a tangled mess to parse the blockstate JSON files.
                        
                        Rather than even try to parse the JSON myself with string operators (cancer),
                        I decided to use an external function library.
                        */
                        List<BlockState> BlockStates = new List<BlockState>();          //Generate the lists
                        List<ModelReference> ModelList = new List<ModelReference>();    //before using them
                        using (StreamReader sr = new StreamReader(Entry.Open()))
                        {
                            try
                            {
                                JObject JsonOutput = JObject.Parse(sr.ReadToEnd());
                                foreach (KeyValuePair<string, JToken> Token in JsonOutput)
                                {
                                    if (Token.Key == "variants")
                                    {
                                        BlockStates = new List<BlockState>();
                                        foreach (JToken Variants in Token.Value)
                                        {
                                            foreach (JToken StateToken in Variants.Values<JToken>())
                                            {
                                                ModelList = new List<ModelReference>();
                                                foreach (JToken ModelToken in StateToken.Children())
                                                {
                                                    string Model = ModelToken.ToObject<string>();
                                                    ModelList.Add(new ModelReference(Model));
                                                }
                                                BlockStates.Add(new BlockState(Variants.ToObject<JProperty>().Name, ModelList));
                                            }
                                            
                                        }
                                        Blocks.Add(new Block(Entry.Name.Split('.')[0], BlockStates));
                                    }
                                }
                            }
                            catch
                            {
                                //For some reason Mojang has a crappy blockstate for acacia_wall_sign. It has a random 'n' after all the data, which gives my code AIDS. :P
                            }
                        }
                    }

                    List<ZipArchiveEntry> BlockModelEntries = new List<ZipArchiveEntry>();

                    foreach (Block block in Blocks)
                    {
                        BlockModelEntries.Add(MinecraftJar.GetEntry("assets/minecraft/models/" + block.BlockStates[0].Models[0].Path + ".json"));
                    }

                    BlockModelEntries = BlockModelEntries.Distinct().ToList();

                    List<BlockModel> BlockModels = new List<BlockModel>();

                    foreach (ZipArchiveEntry Entry in BlockModelEntries)
                    {

                    }


                    Console.Write("");

                    //IEnumerable<ZipArchiveEntry> BlockModelEntries = MinecraftJar.Entries.Where(Entry => (Entry.Name.EndsWith(".json")) && (Entry.FullName.EndsWith(@"minecraft/models/block/" + Entry.Name)));
                    //foreach (ZipArchiveEntry Entry in BlockModelEntries)
                    //{

                    //}

                }

                foreach (Block block in Blocks)
                {
                    Console.WriteLine(block.Name);
                    foreach (BlockState State in block.BlockStates)
                    {
                        Console.WriteLine(State.Name);
                        foreach (ModelReference Model in State.Models)
                        {
                            Console.WriteLine(Model.Path);
                        }
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Done");
                while (true)
                {

                }
            }
            else
            {
                Console.WriteLine("Command line arguments:");
                Console.WriteLine("1. Location of .minecraft");
                Console.WriteLine("2. Location of Minecraft .jar");
                Console.WriteLine("3. Output Directory");
            }
            //Console.WriteLine();
            //Console.WriteLine("Done");
            //while (true)
            //{
                
            //}
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

        public static IEnumerable<string> GetResourcePacks(string OptionsPath)
        {
            /*
            StreamReader is the glorious thing that can read the contents of external files.

            Every time "sr.ReadLine()" is run, the StreamReader will retrievea new line from the
            file as a string. I then use some fancy string operators to parse out the data I want.
            */
            using (StreamReader sr = new StreamReader(OptionsPath))
            {
                char[] RemoveChars = { '[', ']' };  //Declaring this out here prevents some derpy syntax errors.
                string Line;
                string[] LineSplits;
                while (!sr.EndOfStream)
                {
                    /*
                    I have to use a temporary variable here since calling sr.ReadLine() multiple times
                    in the following if statement would end up reading multiple different lines into
                    the condition rather than referencing the same line multiple times.
                    */
                    Line = sr.ReadLine();
                    /*
                    I'll freely admit, this part of the code is a mess. I was messing around with
                    the "conditional operator", which is what all that weird ? : stuff is. The code is almost
                    unreadable because of it though, so I'm not impressed. Here's a link if you want to read
                    about it: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator

                    Anyway, all that this section does is read each line of the options file to see if
                    it starts with "resourcePacks" since that's the line we're interested in.

                    Here's an example of the resourcePacks line:
                    resourcePacks:["file/ZeroServerSoundPack13","file/ToxicCavesTextures","vanilla","file/1.13.2_Default_Textures","file/CleaverPack3","file/HighContrastMaps","file/No_Vignette","file/No_Underwater_Vignette"]
                    
                    Once it finds that line, it splits it at the colon to separate out the data,
                    removes the brackets, and then splits the data at the commas.
                    */
                    //if (Line.StartsWith("resourcePacks"))
                    if (!string.IsNullOrEmpty((LineSplits = Line.StartsWith("resourcePacks") ? Line.Split(':')[1].Trim(RemoveChars).Split(',') : null)?[0]))
                    {
                        //foreach(string LineSegment in Line.Split(':')[1].Trim(RemoveChars).Split(','))
                        foreach (string LineSegment in LineSplits)
                        {
                            yield return LineSegment.Contains('/') ? LineSegment.Split('/')[1].Trim('"') : @"JAR\" + LineSegment.Trim('"');
                        }
                        break;
                    }
                }
            }
        }
    }

    public class Block
    {
        public string Name;
        public List<BlockState> BlockStates = new List<BlockState>();

        public Block(string BlockName, List<BlockState> States)
        {
            Name = BlockName;
            BlockStates = States;
        }
    }

    public class BlockState
    {
        public string Name;
        public List<ModelReference> Models = new List<ModelReference>();

        public BlockState(string BlockStateName, List<ModelReference> BlockStateModels)
        {
            Name = BlockStateName;
            Models = BlockStateModels;
        }
    }

    public class ModelReference
    {
        public string Path;

        public ModelReference(string ModelPath)
        {
            Path = ModelPath;
        }
    }

    public class BlockModel
    {
        public string Name;
        public string Parent;
        public Dictionary<string, Image> Textures;
    }
}
