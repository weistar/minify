using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Minify
{
    /// <summary>
    /// 重命名android内部资源变量，在编译前进行代码混淆
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var directory = new DirectoryInfo(@"F:\tmp006\savory-app-sentence\app");

            var layoutDirectory = new DirectoryInfo(Path.Combine(directory.FullName, "src/main/res/layout"));
            var javaDirectory = new DirectoryInfo(Path.Combine(directory.FullName, "src/main/java"));
            var drawableDirectory = new DirectoryInfo(Path.Combine(directory.FullName, "src/main/res/drawable"));

            var javaFiles = directory.GetFiles("*.java", SearchOption.AllDirectories);
            var layoutFiles = layoutDirectory.GetFiles("*.xml", SearchOption.TopDirectoryOnly);
            var drawableFiles = drawableDirectory.GetFiles("*.xml", SearchOption.TopDirectoryOnly);
            var manifestFile = new FileInfo(Path.Combine(directory.FullName, "src/main/AndroidManifest.xml"));
            var styleFile = new FileInfo(Path.Combine(directory.FullName, "src/main/res/values/styles.xml"));
            var stringFile = new FileInfo(Path.Combine(directory.FullName, "src/main/res/values/strings.xml"));
            var colorFile = new FileInfo(Path.Combine(directory.FullName, "src/main/res/values/colors.xml"));

            Dictionary<string, string> idMap = new Dictionary<string, string>();
            Dictionary<string, string> layoutMap = new Dictionary<string, string>();
            Dictionary<string, string> drawableMap = new Dictionary<string, string>();
            Dictionary<string, string> stringMap = new Dictionary<string, string>();
            Dictionary<string, string> colorMap = new Dictionary<string, string>();

            FillIdMap(layoutFiles, idMap);
            FillLayoutMap(layoutFiles, layoutMap);
            FillDrawableMap(drawableFiles, drawableMap);
            FillValueMap(stringFile, stringMap, "s");
            FillValueMap(colorFile, colorMap, "c");

            ReplaceContent(javaFiles, idMap, "R.id.{0}");
            ReplaceContent(javaFiles, layoutMap, "R.layout.{0}");

            ReplaceContent(layoutFiles, idMap, "@+id/{0}");
            ReplaceContent(layoutFiles, idMap, "@id/{0}");
            ReplaceContent(layoutFiles, drawableMap, "@android:drawable/{0}");
            ReplaceContent(layoutFiles, stringMap, "@string/{0}");
            ReplaceContent(layoutFiles, colorMap, "@color/{0}");

            ReplaceContent(new FileInfo[] { manifestFile }, stringMap, "@string/{0}");

            ReplaceContent(new FileInfo[] { styleFile }, drawableMap, ">@drawable/{0}</");

            ReplaceContent(new FileInfo[] { stringFile }, stringMap, " name=\"{0}\"");

            ReplaceContent(new FileInfo[] { colorFile }, colorMap, " name=\"{0}\"");

            RenameFile(layoutFiles, layoutMap);
            RenameFile(drawableFiles, drawableMap);

            Console.WriteLine("...");
            //Console.Read();
        }

        private static void FillValueMap(FileInfo stringFile, Dictionary<string, string> stringMap, string prefix)
        {
            Regex regex = new Regex(" name=\"(.+)\"");

            var content = File.ReadAllText(stringFile.FullName);

            var matches = regex.Matches(content);
            foreach (Match match in matches)
            {
                stringMap.Add(match.Groups[1].Value, GetNewName(prefix));
            }
        }

        private static void RenameFile(FileInfo[] layoutFiles, Dictionary<string, string> layoutMap)
        {
            foreach (var layoutFile in layoutFiles)
            {
                string key = layoutFile.Name.Replace(layoutFile.Extension, string.Empty);
                if (!layoutMap.ContainsKey(key))
                {
                    continue;
                }

                layoutFile.MoveTo(Path.Combine(layoutFile.Directory.FullName, layoutMap[key] + layoutFile.Extension));
            }
        }

        private static void FillIdMap(FileInfo[] layoutFiles, Dictionary<string, string> idMap)
        {
            Regex regex = new Regex("@\\+?id\\/([a-zA-Z_]+)");

            foreach (var layoutXml in layoutFiles)
            {
                var content = File.ReadAllText(layoutXml.FullName);
                var matchPlus = regex.Matches(content);
                foreach (Match match in matchPlus)
                {
                    var value = match.Groups[1].Value;

                    Console.WriteLine(value);

                    if (!idMap.ContainsKey(value))
                    {
                        idMap.Add(value, GetNewName("i"));
                    }
                }
            }
        }

        private static void FillLayoutMap(FileInfo[] layoutFiles, Dictionary<string, string> layoutMap)
        {
            foreach (var layoutXml in layoutFiles)
            {
                layoutMap.Add(layoutXml.Name.Replace(layoutXml.Extension, string.Empty), GetNewName("l"));
            }
        }

        private static void FillDrawableMap(FileInfo[] drawableFiles, Dictionary<string, string> drawableMap)
        {
            foreach (var drawableFile in drawableFiles)
            {
                if (drawableFile.Name.Equals("ic_launcher_background.xml"))
                {
                    continue;
                }

                drawableMap.Add(drawableFile.Name.Replace(drawableFile.Extension, string.Empty), GetNewName("d"));
            }
        }

        private static void ReplaceContent(FileInfo[] javaFiles, Dictionary<string, string> map, string template)
        {
            foreach (var javaFile in javaFiles)
            {
                var content = File.ReadAllText(javaFile.FullName);
                foreach (var item in map)
                {
                    content = content.Replace(string.Format(template, item.Key), string.Format(template, item.Value));
                }
                File.WriteAllText(javaFile.FullName, content);
            }
        }

        static int index = 1;
        static string GetNewName(string prefix)
        {
            return $"{prefix}{index++}";
        }

        class MyClass
        {
            /// <summary>
            /// android:
            /// </summary>
            public string A { get; set; }

            public string B { get; set; }
        }
    }
}
