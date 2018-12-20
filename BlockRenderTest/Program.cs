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
                List<Block> Blocks = new List<Block>();
                //List<BlockState> BlockStates = new List<BlockState>();
                using (ZipArchive MinecraftJar = ZipFile.OpenRead(MinecraftJarPath))
                {
                    IEnumerable<ZipArchiveEntry> BlockStateEntries = MinecraftJar.Entries.Where(Entry => (Entry.Name.EndsWith(".json")) && (Entry.FullName.EndsWith(@"minecraft/blockstates/" + Entry.Name)));
                    //JsonSerializerSettings JsonSettings = new JsonSerializerSettings
                    //{
                    //    CheckAdditionalContent = true
                    //};
                    //dynamic JsonOutput;
                    //var variants
                    foreach (ZipArchiveEntry Entry in BlockStateEntries)
                    {
                        List<BlockState> BlockStates = new List<BlockState>();
                        List<Model> ModelList = new List<Model>();
                        using (StreamReader sr = new StreamReader(Entry.Open()))
                        {
                            try
                            {
                                JObject JsonOutput = JObject.Parse(sr.ReadToEnd());
                                foreach (KeyValuePair<string, JToken> Token in JsonOutput)
                                {
                                    if (Token.Key == "variants")
                                    {
                                        Console.WriteLine(Token.Key);
                                        foreach (KeyValuePair<string, JToken> Variants in Token.Value.ToObject<JObject>())
                                        {
                                            BlockStates = new List<BlockState>();
                                            foreach (KeyValuePair<string, JToken> StateToken in Variants.Value.ToObject<JObject>())
                                            {
                                                ModelList = new List<Model>();
                                                foreach (JToken ModelToken in StateToken.Value)
                                                {
                                                    foreach (JToken Model in ModelToken)
                                                    {
                                                        ModelList.Add(new Model(Model.Value<string>()));
                                                    }
                                                }
                                            }
                                        }

                                        //foreach (JToken Variants in Token.Value)
                                        //{
                                        //    BlockStates = new List<BlockState>();
                                        //    foreach (JToken StateToken in Variants)
                                        //    {
                                        //        ModelList = new List<Model>();
                                        //        foreach (JToken ModelToken in StateToken)
                                        //        {
                                        //            foreach (JToken Model in ModelToken)
                                        //            {
                                        //                ModelList.Add(new Model(Model.Value<string>()));
                                        //            }
                                        //        }
                                        //        StateToken.ToObject<JObject>();
                                        //    }
                                        //    BlockStates.Add(new BlockState(Variants.Value<string>(), ModelList));
                                        //}

                                    }
                                }

                                //foreach ()
                                //JsonOutput = JsonConvert.DeserializeObject<dynamic>(sr.ReadToEnd());
                                //dynamic variants = JsonOutput.variants;
                                //////dynamic variant1 = variants.;
                                //foreach (dynamic Token in variants.Children())
                                //{
                                ////    foreach (dynamic Value in Token.Value)
                                ////    {

                                ////    }
                                ////    //BlockState.AddVariant(Token.Name, );
                                //}
                                //NestLoop(JsonOutput);

                                //foreach(var)
                                //BlockStates.Add(JsonConvert.DeserializeObject<BlockState>(sr.ReadToEnd()));
                                //Blocks.Add(new Block(Entry.Name.Split('.')[0], BlockStates));
                                Console.Write("");
                            }
                            catch
                            {
                                Console.Write("");
                                //For some reason Mojang has a crappy blockstate for acacia_wall_sign. It has a random 'n' after all the data, which gives my code AIDS. :P
                            }
                        }
                    }
                }

                foreach (Block block in Blocks)
                {
                    foreach (BlockState State in block.BlockStates)
                    {
                        foreach (object data in State.variants)
                        {
                            Console.WriteLine(block.Name);
                            Console.WriteLine(data.ToString());
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

        static void NestLoop(dynamic Input)
        {
            if (Input.HasValues)
            {
                foreach (dynamic Token in Input.ChildrenTokens)
                {
                    NestLoop(Token);
                }
            } else
            {
                Console.WriteLine(Input.Value);
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
        public List<Model> Models = new List<Model>();

        public IList<object> variants;
        public IDictionary<string, List<Model>> Variants;

        public void AddVariant(string Name, List<Model> InputModel)
        {
            Variants.Add(Name, InputModel);
        }

        public BlockState(string BlockStateName, List<Model> BlockStateModels)
        {
            Name = BlockStateName;
            Models = BlockStateModels;
        }
    }

    public class Model
    {
        public string Name;
        public string Path;

        public Model(string ModelPath)
        {
            Path = ModelPath;
        }
    }
}
