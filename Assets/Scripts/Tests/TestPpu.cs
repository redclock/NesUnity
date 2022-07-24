using System.Collections.Generic;
using System.IO;
using System.Text;
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
        byte[] bytes = File.ReadAllBytes( Application.streamingAssetsPath + "/smb.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        
        TickCpu(nes, 2000000);

        Texture2D texture = CreateScreenTexture(nes);
        byte[] textureBytes = texture.EncodeToPNG();
        File.WriteAllBytes("screen.png", textureBytes);

        OutputNameTable(nes);
    }

    private static void OutputNameTable(Nes nes)
    {
        StringBuilder sb = new StringBuilder(1024);
        int address = nes.ppu.Memory.GetNameTableAddress(0);

        for (int row = 0; row < 30; row++)
        {
            for (int col = 0; col < 32; col++)
            {
                byte b = nes.ppu.Memory.Vram[address++];

                sb.Append(b.ToString("X2"));
            }

            sb.AppendLine();
        }

        File.WriteAllText("nametable.txt", sb.ToString());
    }

    private static Texture2D CreateScreenTexture(Nes nes)
    {
        int address = nes.ppu.Memory.GetNameTableAddress(0);
        int addressAttrBase = address + 0x3C0;
        Color32[] pixels = new Color32[Ppu.Y_PIXELS * Ppu.X_PIXELS];
        int pixelIndex = (Ppu.Y_PIXELS - 1) * Ppu.X_PIXELS;
        
        for (int y = 0; y < Ppu.Y_PIXELS; y++)
        {
            int coarseY = y >> 3;
            int fineY = y & 0b111;
            for (int coarseX = 0; coarseX < 32; coarseX++)
            {
                int addressName = coarseY * 32 + coarseX;
                int tileIndex = nes.ppu.Memory.Vram[address + addressName];
                int tileAddress = nes.ppu.PpuCtrl.BackgroundChrAddress + tileIndex * 16 + fineY;
                byte byte1 = nes.ppu.Memory.ReadByte(tileAddress);
                byte byte2 = nes.ppu.Memory.ReadByte(tileAddress + 8);

                int addressAttr = coarseY / 4 * 8 + coarseX / 4;
                byte attr = nes.ppu.Memory.Vram[addressAttrBase + addressAttr];

                // left-top: 0, right-top: 2, left-bottom: 4, right-bottom: 6
                int attrBit = ((coarseY & 0b10) << 1) | (coarseX & 0b10);
                int attrByte = ((attr >> attrBit) & 0b11) << 2;
                // interleave every bit to 2 bits
                int interleaved = Utils.Interleave8To16(byte1) | (Utils.Interleave8To16(byte2) << 1);
                int shift = 14;
                for (int offset = 0; offset < 8; offset++)
                {
                    int chr = (interleaved >> shift) & 0b11;
                    shift -= 2;
                    int paletteIndex = attrByte | chr;
                     if (paletteIndex % 4 == 0) 
                          paletteIndex = 0;
                    byte colorIndex = nes.ppu.Memory.Palette[paletteIndex];
                    pixels[pixelIndex + coarseX * 8 + offset] = Ppu.rgbaPalette[colorIndex & 0x3F];
                }
            }

            pixelIndex -= Ppu.X_PIXELS;
        }

        Texture2D texture = new Texture2D(Ppu.X_PIXELS, Ppu.Y_PIXELS, TextureFormat.RGBA32, false, false);
        texture.SetPixels32(pixels);
        return texture;
    }

    private static void TickCpu(Nes nes, int tickCount)
    {
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

        while (!cpu.Halted && cpu.TotalCycle < tickCount)
        {
            nes.Tick();
        }
        fs.Close();
    }

    [Test]
    public void TestPalette()
    {

        Texture2D texture = new Texture2D(16 * 16, 4 * 8, TextureFormat.RGBA32, false, false);
        var colors = texture.GetPixels32(0);
        int index = 0;
        for (int y = 0; y < 4 * 8; y++)
        {
            for (int x = 0; x < 16 * 16; x++)
            {
                int i = (3 - y / 8) * 16 + x / 16;
                colors[index++] = Ppu.rgbaPalette[i];
            }
        }
        texture.SetPixels32(colors);
        File.WriteAllBytes("p.png", texture.EncodeToPNG());
    }

}
