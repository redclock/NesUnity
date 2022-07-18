using System.IO;
using System.Reflection;
using System.Text;
using NesUnity;
using NUnit.Framework;
using UnityEngine;

public class TestNes
{
    [Test]
    public void TestNesChrTable()
    {
        byte[] bytes = File.ReadAllBytes( Application.streamingAssetsPath + "/smb.nes");
        var nes = new NesRom();
        Assert.True(nes.ReadFromBytes(bytes));
        var texture = CreatePatternTexture(nes.chrPatternTable);
        byte[] textureBytes = texture.EncodeToPNG();
        File.WriteAllBytes("Assets/chr.png", textureBytes);
    }

    private Texture2D CreatePatternTexture(PatternTable patternTable)
    {
        
        byte[] pattern = patternTable.GetPatternBuffer();
        Color32[] buffer = new Color32[128 * 128];
        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false, false);

        for (int iPat = 0; iPat < 256; iPat++)
        {
            int patternOffset = iPat * 64;
            
            for (int iPixel = 0; iPixel < 64; iPixel++)
            {
                byte p = pattern[patternOffset + iPixel];
                byte c = (byte) (p * 64);
                Color32 color = new Color32(c, c, c, 255);
                int row = (15 - iPat / 16) * 8 + iPixel / 8;
                int col = iPat % 16 * 8 + iPixel % 8;
                buffer[row * 128 + col] = color;
            }

        }
        
        texture.SetPixels32(buffer);

        return texture;
    }
    
    [Test]
    public void TestNesMemory()
    {
        byte[] bytes = File.ReadAllBytes( Application.streamingAssetsPath + "/nestest.nes");
        var nes = new NesRom();
        Assert.True(nes.ReadFromBytes(bytes));
        CpuMemory cpuMemory = new CpuMemory(nes.mapper);
        int nmi = cpuMemory.GetInterruptVector(Interrupt.Nmi);
        int reset = cpuMemory.GetInterruptVector(Interrupt.Reset);
        int irq = cpuMemory.GetInterruptVector(Interrupt.Irq);
        
        Debug.LogFormat("NMI = ${0:X4} RST = ${1:X4} IRQ = ${2:X4}", nmi, reset, irq);
    }

    [Test]
    public void TestCpuHasAllInstructions()
    {
        Cpu.OpcodeDef[] codes = new Cpu.OpcodeDef[256];
        
        var methods = typeof(Cpu).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var m in methods)
        {
            var instructions = m.GetCustomAttributes<Cpu.OpcodeDef>(false);
            foreach (var inst in instructions)
            {
                int index = inst.Opcode;
                if (codes[index] != null)
                {
                    Debug.LogErrorFormat("Duplicated instruction {0:X2}", index);
                }

                codes[index] = inst;
            }
        }

        for (int i = 0; i < 256; i++)
        {
            if (codes[i] == null)
            {
                Debug.LogErrorFormat("Unsupported instruction {0:X2}", i);
            }
        }
    }

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
