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

}
