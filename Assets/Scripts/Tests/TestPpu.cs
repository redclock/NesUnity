using System.Collections.Generic;
using System.IO;
using NesUnity;
using NUnit.Framework;
using UnityEngine;

public class TestPpu
{
    [Test]
    public void TestPpuCtrl()
    {
        byte[] bytes = File.ReadAllBytes( Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        
        Assert.True(nes.PowerOn(bytes));
        nes.cpu.Memory.WriteByte(0x2000, 0b10010111);

        Assert.False(nes.ppu.PpuCtrl.IsMaster);
        Assert.True(nes.ppu.PpuCtrl.NmiEnabled);
        Assert.AreEqual(8, nes.ppu.PpuCtrl.SpritesSize);
        Assert.AreEqual(0x1000, nes.ppu.PpuCtrl.BackgroundChrAddress);
        Assert.AreEqual(0x0, nes.ppu.PpuCtrl.SpriteChrAddress);
        Assert.AreEqual(32, nes.ppu.PpuCtrl.VRamIncrement);
        Assert.AreEqual(0b000110000000000, nes.ppu.TempAddress);
    }
    
    [Test]
    public void TestPpuMask()
    {
        byte[] bytes = File.ReadAllBytes( Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        
        Assert.True(nes.PowerOn(bytes));
        nes.cpu.Memory.WriteByte(0x2201, 0b10010111);

        Assert.True(nes.ppu.PpuMask.Greyscale);
        Assert.True(nes.ppu.PpuMask.ShowLeft8Background);
        Assert.True(nes.ppu.PpuMask.ShowLeft8Sprite);
        Assert.False(nes.ppu.PpuMask.ShowBackground);
        Assert.True(nes.ppu.PpuMask.ShowSprites);
    }

    [Test]
    public void TestOam()
    {
        byte[] bytes = File.ReadAllBytes( Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        
        Assert.True(nes.PowerOn(bytes));
        nes.cpu.Memory.WriteByte(0x2203, 12);
        nes.cpu.Memory.WriteByte(0x2204, 31);
        nes.cpu.Memory.WriteByte(0x2204, 32);
        nes.cpu.Memory.WriteByte(0x2204, 44);
        
        nes.cpu.Memory.WriteByte(0x2203, 12);

        Assert.AreEqual(31, (int)nes.cpu.Memory.ReadByte(0x2204));
        Assert.AreEqual(31, (int)nes.cpu.Memory.ReadByte(0x2204));
        nes.cpu.Memory.WriteByte(0x2203, 13);
        Assert.AreEqual(32, (int)nes.cpu.Memory.ReadByte(0x2204));
        nes.cpu.Memory.WriteByte(0x2203, 14);
        Assert.AreEqual(44, (int)nes.cpu.Memory.ReadByte(0x2204));
    }

    [Test]
    public void TestAddress()
    {
        byte[] bytes = File.ReadAllBytes( Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        
        Assert.True(nes.PowerOn(bytes));
        nes.cpu.Memory.WriteByte(0x2200, 0b10);
        Assert.AreEqual(0b000100000000000, nes.ppu.TempAddress);
        nes.cpu.Memory.WriteByte(0x2205, 0b10110110);
        Assert.AreEqual(0b000100000010110, nes.ppu.TempAddress);
        nes.cpu.Memory.WriteByte(0x2205, 0b01100010);
        Assert.AreEqual(0b010100110010110, nes.ppu.TempAddress);
        
        nes.cpu.Memory.ReadByte(0x2202);
        
        nes.cpu.Memory.WriteByte(0x2206, 0b01111011);
        Assert.AreEqual(0b011101110010110, nes.ppu.TempAddress);
        nes.cpu.Memory.WriteByte(0x2206, 0b11100001);
        Assert.AreEqual(0b011101111100001, nes.ppu.TempAddress);
    }

    [Test]
    public void TestNameTable()
    {
        byte[] bytes = File.ReadAllBytes( Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        
        using StreamWriter fs = File.CreateText("result.txt");
        
        Cpu cpu = nes.cpu;

        HashSet<int> addressCache = new HashSet<int>(); 
        
        cpu.OnBeforeExecute = () =>
        {
            if (addressCache.Contains(cpu.PC))
                return;
            addressCache.Add(cpu.PC);
            string code = cpu.GetDisassembly(cpu.PC);
            fs.Write($"{cpu.PC:X4} {cpu.GetCurOp().Code:X2} {code}");
            for (int i = 0; i < 12 - code.Length; i++)
            {
                fs.Write($" ");
            }
            fs.WriteLine($"A:{cpu.A:X2} X:{cpu.X:X2} Y:{cpu.Y:X2} P:{cpu.P.ToByte():X2} SP:{cpu.SP:X2}");
            fs.Flush();
        };
        
        while (!cpu.Halted && cpu.TotalCycle < 1000000)
        {
            nes.Tick();
        }

        fs.Close();
    }
}
