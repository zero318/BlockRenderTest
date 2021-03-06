﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
    In order to make it easier for anyone to understand what is going on with all of this code,
    I have split it into multiple files and added a lot of comments to describe it. In an attempt to make
    everything clearer, multiple files are only used to separate classes. Make sure you pay
    attention to the colors of the text to determine when classes in other files are being referenced!

    Classes:                Files:
    ResourcePack:           ResourcePack.cs
    Block:                  Block.cs
    BlockState:             Block.cs
    ModelReference:         Block.cs
    BlockModel:             BlockModel.cs
    TrashMonkeyException:   CustomExceptions.cs
*/ 

namespace BlockRenderTest
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
    class Program
    {
        //In C#, the program always starts at Main()
        static void Main(string[] args)
        {
            if (args.Length >= 3)
            {
                string MinecraftPath = args[0];     //"F:\My Minecraft Expansion\.minecraft"
                string MinecraftJarPath = args[1];  //"C:\Users\zero318\AppData\Roaming\.minecraft\versions\18w50a\18w50a.jar"
                string OutputDirectory = args[2];   //"F:\My Minecraft Expansion\_BlockRenderTempDirectory"

                string OptionsPath = MinecraftPath + @"\options.txt";

                //The program complains about initializing variables inside a try/catch
                string[] PackList = null;
                try
                {
                    /*
                    This function is *really* important! Rather than just blindly loading every resource pack in
                    the folder (because some people like myself have 50 or something stupid), this function loads
                    the list of currently enabled resource packs directly from Minecraft's options. :D

                    The code of the method is located in the ResourcePack class at line 50.
                    */
                    PackList = ResourcePack.GetResourcePacks(OptionsPath).ToArray();
                    //PackList = GetResourcePacks(OptionsPath).Reverse().ToArray();
                }
                catch (TrashMonkeyException e)
                {
                    /*
                    This prints an error message and then terminates the program. An exit code of 1 is used
                    so that a calling batch file can tell there was an error.
                    */
                    Console.WriteLine(e.Message);
                    Environment.Exit(1);
                }
                
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
                //This list has to be declared outside of the using block so that it can be referenced later
                Dictionary<string, Block> Blocks = new Dictionary<string, Block>();
                foreach (ResourcePack Pack in ResourcePacks)
                {
                    List<Block> PackBlocks = new List<Block>();
                    switch (Pack.Type)
                    {
                        case ResourcePack.PackType.Mod:
                        case ResourcePack.PackType.Jar:
                        case ResourcePack.PackType.Zip:
                            using (ZipArchive ZipFolder = ZipFile.OpenRead(Pack.Path))
                            {
                                IEnumerable<ZipArchiveEntry> BlockStateEntries = ZipFolder.Entries.Where(Entry => Entry.Name.EndsWith(".json") && Entry.FullName.EndsWith(@"blockstates/" + Entry.Name));
                                foreach (ZipArchiveEntry Entry in BlockStateEntries)
                                {
                                    try
                                    {
                                        PackBlocks.Add(new Block(Entry.Name.Split('.')[0], GetBlockData(Entry.Open())));
                                    }
                                    catch
                                    {
                                        /*
                                        For some reason Mojang has a crappy blockstate for acacia_wall_sign.
                                        It has a random 'n' after all the data, which gives my code AIDS
                                        unless I put it all in a try/catch. :P
                                        */
                                    }
                                }
                            }
                            break;
                        case ResourcePack.PackType.Folder:
                            string AssetsPath = Pack.Path + @"\assets";
                            string BlockStatesPath;
                            if (Directory.Exists(AssetsPath))
                            {
                                foreach(string Namespace in Directory.GetDirectories(AssetsPath))
                                {
                                    Console.Write("");
                                    BlockStatesPath = Namespace + @"\blockstates";
                                    if (Directory.Exists(BlockStatesPath))
                                    {
                                        foreach (string BlockStateFile in Directory.GetFiles(BlockStatesPath))
                                        {
                                            using (StreamReader sr = new StreamReader(BlockStateFile))
                                            {
                                                try
                                                {
                                                    PackBlocks.Add(new Block(BlockStateFile.Split('\\').Reverse().ToArray()[0].Split('.')[0], GetBlockData(sr.BaseStream)));
                                                    Console.Write("");
                                                }
                                                catch
                                                {

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    Console.Write("");
                    foreach (Block block in PackBlocks)
                    {
                        Blocks[block.Name] = block;
                    }
                }

                foreach (Block block in Blocks.Values)
                {
                    Console.WriteLine(block.Name);
                    foreach (BlockState State in block.BlockStates)
                    {
                        Console.WriteLine(State.Name);
                        foreach (ModelReference Model in State.Models)
                        {
                            Console.WriteLine(Model.ModelData["model"]);
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

        static List<BlockState> GetBlockData(Stream InputStream)
        {
            /*
            This whole section is just a tangled mess to parse the blockstate JSON files.

            Rather than even try to parse the JSON myself with string operators (cancer),
            I decided to use an external function library.
             */
            List<BlockState> BlockStates = new List<BlockState>();          //Generate the lists
            List<ModelReference> ModelList = new List<ModelReference>();    //before using them
            Dictionary<string, string> ModelDictionary = new Dictionary<string, string>();
            using (StreamReader sr = new StreamReader(InputStream))
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
                                ModelDictionary = new Dictionary<string, string>();
                                foreach (JToken ModelToken in StateToken.Children())
                                {
                                    ModelDictionary.Add(ModelToken.ToObject<JProperty>().Name, ModelToken.ToObject<string>());
                                }
                                ModelList.Add(new ModelReference(ModelDictionary));
                                BlockStates.Add(new BlockState(Variants.ToObject<JProperty>().Name, ModelList));
                            }
                        }
                        //PackBlocks.Add(new Block(Entry.Name.Split('.')[0], BlockStates));
                    }
                    else
                    {
                        throw new TrashMonkeyException("Model is multipart!");
                    }
                }
            }
            return BlockStates;
        }

        static void GetBlockStates(string PathInput)
        {
            List<Block> Blocks = new List<Block>();
            using (ZipArchive MinecraftJar = ZipFile.OpenRead(PathInput))
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
                    Dictionary<string, string> ModelDictionary = new Dictionary<string, string>();
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
                                            ModelDictionary = new Dictionary<string, string>();
                                            foreach (JToken ModelToken in StateToken.Children())
                                            {
                                                ModelDictionary.Add(ModelToken.ToObject<JProperty>().Name, ModelToken.ToObject<string>());
                                            }
                                            ModelList.Add(new ModelReference(ModelDictionary));
                                            BlockStates.Add(new BlockState(Variants.ToObject<JProperty>().Name, ModelList));
                                        }
                                    }
                                    Blocks.Add(new Block(Entry.Name.Split('.')[0], BlockStates));
                                }
                            }
                        }
                        catch
                        {
                            /*
                            For some reason Mojang has a crappy blockstate for acacia_wall_sign.
                            It has a random 'n' after all the data, which gives my code AIDS
                            unless I put it all in a try/catch. :P
                            */
                        }
                    }
                }
                ResourcePack[] ResourcePacks = null;
                foreach (ResourcePack Pack in ResourcePacks)
                {
                    switch (Pack.Type)
                    {
                        case ResourcePack.PackType.Mod:
                        case ResourcePack.PackType.Jar:
                        case ResourcePack.PackType.Zip:

                            break;
                        case ResourcePack.PackType.Folder:

                            break;
                        default:
                            break;
                    }
                }

                List<ZipArchiveEntry> BlockModelEntries = new List<ZipArchiveEntry>();

                foreach (Block block in Blocks)
                {
                    BlockModelEntries.Add(MinecraftJar.GetEntry("assets/minecraft/models/" + block.BlockStates[0].Models[0].ModelData["model"] + ".json"));
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
        }
        
    }
}
