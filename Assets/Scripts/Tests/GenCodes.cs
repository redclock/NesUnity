using System.IO;
using System.Reflection;
using System.Text;
using NesUnity;
using NUnit.Framework;

public class GenCodes
{
    private static void AppendSpaces(StringBuilder sb, int n)
    {
        for (int i = 0; i < n; i++)
            sb.Append(' ');
    }
    
    [Test]
    public void GenCpuInstructionsMap()
    {
        const string FILE_NAME = "Assets/Scripts/NesUnity/Cpu.Instruction.Map.cs";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(@"//Auto Generated Code
namespace NesUnity
{
    using static AddressingMode;
    public partial class Cpu
    {
        private void InitInstructions()
        {");
        var methods = typeof(Cpu).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var m in methods)
        {
            var instructions = m.GetCustomAttributes<Cpu.OpcodeDef>(false);
            foreach (var inst in instructions)
            {
                AppendSpaces(sb, 12);
                sb.Append($@"Op(""{m.Name}"", 0x{inst.Opcode.ToString("X2")}, {m.Name}, {inst.Mode},");
                AppendSpaces(sb, 11 - inst.Mode.ToString().Length);
                sb.Append($@"{inst.Cycles}");
                if (inst.PageBoundary && inst.RMW)
                {
                    sb.AppendLine($@", {inst.PageBoundary.ToString().ToLower()}, {inst.RMW.ToString().ToLower()});");
                } else if (inst.PageBoundary)
                {
                    sb.AppendLine($@", pageBoundary: {inst.PageBoundary.ToString().ToLower()});");
                } else if (inst.RMW)
                {
                    sb.AppendLine($@", rmw: {inst.RMW.ToString().ToLower()});");
                }
                else
                {
                    sb.AppendLine(");");
                }
            }
        }

        sb.AppendLine(@"
        }
    }
}");
        File.WriteAllText(FILE_NAME, sb.ToString());
    }
}
