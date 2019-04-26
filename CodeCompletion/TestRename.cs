using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PascalABCCompiler.PascalABCNewParser;
using PascalABCCompiler.SyntaxTree;
using Position = PascalABCCompiler.Parsers.Position;

namespace CodeCompletion
{
    public class RenameTester
    {
        public static void TestRename(string dir)
        {
            var files = Directory.GetFiles(dir, "*.pas");
            foreach (var FileName in files)
            {
                var testSuit = File.ReadAllText(Path.ChangeExtension(FileName, ".txt"))
                    .Split(';').Select(ts =>
                        ts.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    .Where(ts => ts.Length > 1)
                    .Select(ts =>
                    {
                        var tsIn = ts[0].Split(' ');
                        var tsOut = ts[1].Split(',').Select(p =>
                                p.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(e => int.Parse(e)))
                            .Select(p => new Pos(p.First(), p.Last()));
                        return new
                        {
                            Expr = tsIn[0],
                            Name = tsIn[1],
                            Line = int.Parse(tsIn[2]),
                            Col = int.Parse(tsIn[3]),
                            Positions = tsOut
                        };
                    }).ToArray();

                for (int i = 0; i < testSuit.Length; ++i)
                {
                    var ts = testSuit[i];
                    var positions = FindPositions(ts.Expr, ts.Name, FileName,
                        ts.Line - 1, ts.Col - 1).Select(p => new Pos(p.line, p.column));
                    string fname = Path.GetFileNameWithoutExtension(FileName).Split('\\').Last();
                    assert(positions.Count() == ts.Positions.Count() &&
                        positions.Zip(ts.Positions, (p1, p2) => p1 == p2).All(e => e),
                            $"Test {fname}-{i} failed.\n\n" +
                            $"{string.Join(" ", ts.Expr, ts.Name, ts.Line, ts.Col)}\n" +
                            $"{string.Join(", ", positions)}\n\n" +
                            $"Should:\n{string.Join(", ", ts.Positions)}");
                }
            }
        }

        private static List<Position> FindPositions(string expr, string name, string fileName, int line, int column)
        {
            string text = File.ReadAllText(fileName);
            PascalABCNewLanguageParser parser = new PascalABCNewLanguageParser();
            compilation_unit cu = parser.BuildTreeInNormalMode(fileName, text) as compilation_unit;
            expression e = parser.BuildTreeInExprMode(fileName, expr) as expression;

            var cv = CollectLightSymInfoVisitor.New;
            cv.ProcessNode(cu);
            var rf1 = new ReferenceFinder1(e, cv.Root, cu);
            rf1.FindPositions(name, line, column, cu);

            return rf1.Positions;
        }

        private static void assert(bool cond, string message = null)
        {
            if (message != null)
                System.Diagnostics.Debug.Assert(cond, message);
            else
                System.Diagnostics.Debug.Assert(cond);
        }

        private struct Pos
        {
            public int Line, Col;

            public Pos(int line, int col)
            {
                Line = line;
                Col = col;
            }

            public override string ToString()
                => $"{Line} {Col}";

            public static bool operator ==(Pos p1, Pos p2)
                => p1.Line == p2.Line && p1.Col == p2.Col;

            public static bool operator !=(Pos p1, Pos p2)
                => !(p1 == p2);
        }
    }
}
