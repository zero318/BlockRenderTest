using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BlockRenderTest
{
    /*
    This class is used to store information about each resource pack.
    */
    public class ResourcePack
    {
        public string Name;
        public string Path;
        public PackType Type;

        public enum PackType { Zip, Folder, Jar, Mod };

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
                /*
                Once a StreamReader has run out of lines to process, this condition will exit the while loop.
                Of course, that shouldn't actually happen, so it only prevents an infinite loop. The loop
                will exit anyway once it finds the "resourcePacks" line, so it'll only run out if the file
                doesn't actually contain that, which means the user did something dumb.
                */
                while (!sr.EndOfStream)
                {
                    /*
                    I have to use a temporary variable here since calling sr.ReadLine() multiple times
                    in the following if statement would end up reading multiple different lines into
                    the condition rather than referencing the same line multiple times.
                    */
                    Line = sr.ReadLine();
                    if (Line.StartsWith("resourcePacks"))
                    {
                        foreach (string LineSegment in Line.Split(':')[1].Trim(RemoveChars).Split(','))
                        {
                            /*
                            This line is a bit weird since I was messing around with two C# features I've
                            never used before, the "conditional operator" and "yield return".

                            The conditional operator is that stuff with the ? : weirdness. What it does is
                            evaluates a condition and then returns separate values depending on whether it's
                            true or false. In this case, it checks the string for a / to determine whether or not
                            the string is a resource pack or the default textures. If it's a resource pack, it
                            separates out the pack name from the "file/" crap. If it's the default textures, it
                            marks it with "JAR\" so that the code doesn't get confused in case some moron names
                            a resource pack "vanilla". Here's a link if you want to read more about the conditional operator:
                            https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator

                            What yield return does is a lot simpler. Instead of merely returning a single value
                            as soon as the code reaches a return statement, it adds that value to a list that will be
                            returned at the end.
                            */
                            yield return LineSegment.Contains('/')      //Condition
                                ? LineSegment.Split('/')[1].Trim('"')   //If true
                                : @"JAR\" + LineSegment.Trim('"');      //If false
                        }
                        yield break;  //This statement merely tells the code "Yo, you just got all the data. You can go back to other stuff now."
                    }
                }
                /*
                Remember how I mentioned that the while loop shouldn't actually exit? In order to prevent the
                code doing anything weird, this will manually throw an exception. Why do that you may ask?
                The code calling this method can be enclosed inside of a try/catch block specifically to catch
                this exception, then respond by gracefully exiting rather than just killing eveything.
                */
                throw new TrashMonkeyException("Could not find resource pack data in options file!");
            }
        }
    }
}
