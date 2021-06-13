using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

namespace LibGit2Sharp.NativeMethodBuilder {
    class Program {
        static void Main(string[] args) {
            var implicitPath = @"D:\Projects\libgit2sharp.nativebinaries\libgit2";
            if(args.Length == 0 && Directory.Exists(implicitPath)) {
                Build(implicitPath);
            } else {
                var libgit2Directory = args[0];
                Build(libgit2Directory);

            }
        }

        static void Build(string libgit2Directory) {
            var headerFilePaths = Directory.EnumerateFiles(libgit2Directory, "*.h", SearchOption.AllDirectories);

            var gitExternPattern = new Regex(@"(?<Documantation>\/\*\*?\n(?: \*[^\n]*\n)+ \*\/)?\r?\n?GIT_EXTERN\((?<ReturnType>[^\)]+)\) ?(?<MethodName>git[^\(]+)\s*\(\s*(?<Arguments>[^\)]*)\)", RegexOptions.Compiled);
            var gitArgSpacePattern = new Regex(@"[\(,][ \t]+", RegexOptions.Compiled);
            var gitArgPattern = new Regex(@"(?<Type>(?:const )?[^ ]+) (?<Name>\**[\w]+),? ?|(?<Type>\.\.\.)", RegexOptions.Compiled);



            ImmutableArray<(string Type, string Name)> ToArguments(string argumentStr) {
                if(argumentStr.Equals("void")) return ImmutableArray<(string, string)>.Empty;

                return gitArgPattern.Matches(argumentStr).Select(m => (m.Groups["Type"].Value, m.Groups["Name"].Value)).ToImmutableArray();


                //return argumentStr.Split(',').Select(x => x.Trim().Split(' ', 2)).Select(x => !x[0].Equals("...") ? (x[0], x[1].Trim()) : (x[0], null)).ToImmutableArray();
            };


            var gitExterns = headerFilePaths
                .SelectMany(x => gitExternPattern.Matches(File.ReadAllText(x).Replace("\r", "")))
                .Select(m => new {
                    MethodName = m.Groups["MethodName"].Value,
                    Arguments = ToArguments(gitArgSpacePattern.Replace(m.Groups["Arguments"].Value.Replace("\n", ""), m => m.Value[0] + " ")),
                    Documentation = m.Groups["Documantation"].Value, ReturnType = m.Groups["ReturnType"].Value,
                })
                .ToArray();


            var exports = string.Join("\n", gitExterns.OrderBy(x => x.MethodName).Select(x => x.MethodName + " " + x.ReturnType + " " + x.MethodName + "(" + string.Join(", ", x.Arguments.Select(a => a.Type + " " + a.Name)) + ")"));



            var exports2 = File.ReadAllLines(@"D:\Projects\libgit2sharp\LibGit2Sharp.NativeMethodBuilder\Exports.txt").Select(l => l.Split(' ', 2)).Select(x => new { Name = x[0], Code = x[1] }).ToArray();
            var imports2 = File.ReadAllLines(@"D:\Projects\libgit2sharp\LibGit2Sharp.NativeMethodBuilder\CurrentDllImports.txt").Select(l => l.Split(' ', 2)).Select(x => new { Name = x[0], Code = x[1] }).ToArray();


            var diff = imports2.Select(i => (i, e: exports2.FirstOrDefault(e => e.Name.Equals(i.Name)))).Where(x => x.e != null);

            var seart = string.Join("\n\n", diff.Select(x => x.e.Code + "\n" + x.i.Code));

        }
    }
}
