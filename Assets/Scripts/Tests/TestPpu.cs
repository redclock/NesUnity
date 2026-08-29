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
    public void TestPpuScrollAndAddressState()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));

        nes.cpu.Memory.WriteByte(0x2005, 0xB6);
        Assert.AreEqual(6, nes.ppu.FineXScroll);
        Assert.True(nes.ppu.AddressWriteToggle);
        Assert.AreEqual(0x16, Ppu.GetCoarseX(nes.ppu.TempAddress));

        nes.cpu.Memory.WriteByte(0x2005, 0x62);
        Assert.False(nes.ppu.AddressWriteToggle);
        Assert.AreEqual(0x0C, Ppu.GetCoarseYValue(nes.ppu.TempAddress));
        Assert.AreEqual(2, Ppu.GetFineYValue(nes.ppu.TempAddress));

        nes.cpu.Memory.WriteByte(0x2006, 0x3F);
        Assert.True(nes.ppu.AddressWriteToggle);
        nes.cpu.Memory.WriteByte(0x2006, 0x10);
        Assert.False(nes.ppu.AddressWriteToggle);
        Assert.AreEqual(0x3F10, nes.ppu.CurrentAddress);
    }

    [Test]
    public void TestPpuDataReadBufferAndIncrement()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        nes.ppu.Memory.WriteByte(0x2000, 0x12);

        nes.cpu.Memory.WriteByte(0x2006, 0x20);
        nes.cpu.Memory.WriteByte(0x2006, 0x00);
        Assert.AreEqual(0x00, nes.cpu.Memory.ReadByte(0x2007));
        Assert.AreEqual(0x12, nes.cpu.Memory.ReadByte(0x2007));
        Assert.AreEqual(0x2002, nes.ppu.CurrentAddress);

        nes.cpu.Memory.WriteByte(0x2000, 0x04);
        nes.cpu.Memory.WriteByte(0x2006, 0x20);
        nes.cpu.Memory.WriteByte(0x2006, 0x00);
        nes.cpu.Memory.WriteByte(0x2007, 0x34);
        Assert.AreEqual(0x2020, nes.ppu.CurrentAddress);
    }

    [Test]
    public void TestPpuStatusReadClearsVblankAndWriteToggle()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));

        nes.cpu.Memory.WriteByte(0x2005, 0x01);
        Assert.True(nes.ppu.AddressWriteToggle);
        nes.ppu.PpuStatus.VBlank = true;
        byte status = nes.cpu.Memory.ReadByte(0x2002);

        Assert.True((status & 0x80) != 0);
        Assert.False(nes.ppu.PpuStatus.VBlank);
        Assert.False(nes.ppu.AddressWriteToggle);
    }

    [Test]
    public void TestPpuGreyscaleMasksPaletteLowBits()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        bytes[5] = 0;
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));

        for (int i = 0; i < 8; i++)
        {
            nes.ppu.Memory.WriteByte(i, 0xFF);
            nes.ppu.Memory.WriteByte(8 + i, 0x00);
        }
        nes.ppu.Memory.WriteByte(0x3F00, 0x0F);
        nes.ppu.Memory.WriteByte(0x3F01, 0x2F);
        nes.cpu.Memory.WriteByte(0x2001, 0x0B);
        nes.ppu.Tick();

        Assert.AreEqual(0x20, nes.ppu.FrameBuffer[(Ppu.Y_PIXELS - 1) * Ppu.X_PIXELS]);
    }

    [Test]
    public void TestNmiLineUsesVblankAndControlEdge()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));

        nes.cpu.Memory.WriteByte(0x2000, 0x80);
        int ticks = 0;
        while (!nes.ppu.PpuStatus.VBlank && ticks++ < Ppu.X_CYCLES * 242)
            nes.ppu.Tick();

        Assert.True(nes.ppu.PpuStatus.VBlank);
        Assert.AreEqual(nes.cpu.Memory.GetInterruptVector(Interrupt.Nmi), nes.cpu.PC);
        int cycleAfterFirstNmi = nes.cpu.Cycle;

        nes.cpu.Memory.WriteByte(0x2000, 0x00);
        nes.cpu.Memory.WriteByte(0x2000, 0x80);
        Assert.AreEqual(cycleAfterFirstNmi + 7, nes.cpu.Cycle);

        nes.cpu.Memory.ReadByte(0x2002);
        Assert.False(nes.ppu.PpuStatus.VBlank);
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
    public void TestOamDmaCopiesPageAndStallsCpu()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));

        for (int i = 0; i < 0x100; i++)
            nes.cpu.Memory.WriteByte(0x0200 + i, (byte)i);

        nes.cpu.Memory.WriteByte(0x2003, 0x20);
        int beforeTotal = nes.cpu.TotalCycle;
        int beforeCycle = nes.cpu.Cycle;
        nes.cpu.Memory.WriteByte(0x4014, 0x02);
        byte[] oam = nes.ppu.GetOamSnapshot();

        Assert.AreEqual(0, oam[0x20]);
        Assert.AreEqual(0x7F, oam[0x9F]);
        Assert.AreEqual(0xFF, oam[0x1F]);
        Assert.AreEqual(beforeCycle + 513 + (beforeTotal & 1), nes.cpu.Cycle);
    }

    [Test]
    public void TestSpriteEvaluationSelectsEightAndSetsOverflow()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));

        nes.cpu.Memory.WriteByte(0x2001, 0x18);
        nes.cpu.Memory.WriteByte(0x2003, 0);
        for (int sprite = 0; sprite < 9; sprite++)
        {
            nes.cpu.Memory.WriteByte(0x2004, 0);
            nes.cpu.Memory.WriteByte(0x2004, 0);
            nes.cpu.Memory.WriteByte(0x2004, 0);
            nes.cpu.Memory.WriteByte(0x2004, (byte)(sprite * 8));
        }

        for (int i = 0; i < Ppu.X_CYCLES + 1; i++)
            nes.ppu.Tick();

        Assert.AreEqual(8, nes.ppu.SelectedSpriteCount);
        Assert.True(nes.ppu.PpuStatus.SpriteOverflow);
    }

    [Test]
    public void TestRunFrameRaisesVblankOnce()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        nes.cpu.Memory.WriteByte(0x2000, 0x80);

        Assert.True(nes.RunFrame());
        Assert.AreEqual(1, nes.FrameCount);
        Assert.True(nes.FrameReady);
        Assert.True(nes.ppu.PpuStatus.VBlank);

        Assert.True(nes.RunFrame());
        Assert.AreEqual(2, nes.FrameCount);
    }

    [Test]
    public void TestPpuUsesNtscFrameTiming()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        nes.cpu.Memory.WriteByte(0x2001, 0x08);

        int ppuTicks = 0;
        while (!nes.isEndScreen && ppuTicks < Ppu.X_CYCLES * Ppu.Y_SCANLINES)
        {
            nes.ppu.Tick();
            ppuTicks++;
        }

        Assert.True(nes.isEndScreen);
        Assert.AreEqual(Ppu.X_CYCLES * 241 + 2, ppuTicks);
    }

    [Test]
    public void TestPpuOddFrameSkipsOneDotWhenRendering()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        nes.cpu.Memory.WriteByte(0x2001, 0x08);

        int firstFrameDots = 0;
        do
        {
            nes.ppu.Tick();
            firstFrameDots++;
        } while (nes.ppu.Scanline != 0 || nes.ppu.Dot != 0);

        int secondFrameDots = 0;
        do
        {
            nes.ppu.Tick();
            secondFrameDots++;
        } while (nes.ppu.Scanline != 0 || nes.ppu.Dot != 0);

        Assert.AreEqual(Ppu.X_CYCLES * Ppu.Y_SCANLINES, firstFrameDots);
        Assert.AreEqual(Ppu.X_CYCLES * Ppu.Y_SCANLINES - 1, secondFrameDots);
    }

    [Test]
    public void TestPpuHorizontalAddressWrapsAtTileBoundary()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        nes.cpu.Memory.WriteByte(0x2001, 0x08);
        nes.cpu.Memory.WriteByte(0x2006, 0x20);
        nes.cpu.Memory.WriteByte(0x2006, 0x1F);

        for (int i = 0; i < 9; i++)
            nes.ppu.Tick();

        Assert.AreEqual(0x2400, nes.ppu.CurrentAddress);
    }

    [Test]
    public void TestRunFrameRendersSmbFrame()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/smb.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));

        for (int i = 0; i < 120; i++)
            Assert.True(nes.RunFrame());

        int visiblePixels = 0;
        for (int i = 0; i < nes.ppu.FrameBuffer.Length; i++)
        {
            if (nes.ppu.FrameBuffer[i] != 0)
                visiblePixels++;
        }

        Assert.Greater(visiblePixels, 1000);
    }

    [Test]
    public void TestSmbFrameBackgroundMatchesStaticRenderer()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/smb.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));

        for (int i = 0; i < 120; i++)
            Assert.True(nes.RunFrame());

        nes.cpu.Memory.WriteByte(0x2001, 0x0A);
        nes.ppu.RenderFrameForTest();
        int[] rendered = (int[])nes.ppu.FrameBuffer.Clone();
        nes.ppu.GenBackground(0);

        int differentPixels = 0;
        for (int i = 0; i < rendered.Length; i++)
        {
            if (rendered[i] != nes.ppu.FrameBuffer[i])
                differentPixels++;
        }

        Assert.AreEqual(0, differentPixels,
            "Frame-level background rendering must match the existing renderer when scroll is zero.");
    }

    [Test]
    public void TestSmbStartAndScrollFrame()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/smb.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));

        for (int i = 0; i < 30; i++)
            Assert.True(nes.RunFrame());
        nes.Controller1.SetButton(NesController.Button.Start, true);
        nes.Controller1.SetButton(NesController.Button.A, true);
        for (int i = 0; i < 4; i++)
            Assert.True(nes.RunFrame());
        nes.Controller1.SetButton(NesController.Button.Start, false);
        nes.Controller1.SetButton(NesController.Button.A, false);

        for (int i = 0; i < 120; i++)
            Assert.True(nes.RunFrame());

        int nonBackdropPixels = 0;
        for (int i = 0; i < nes.ppu.FrameBuffer.Length; i++)
        {
            if (nes.ppu.FrameBuffer[i] != nes.ppu.FrameBuffer[0])
                nonBackdropPixels++;
        }

        Assert.Greater(nonBackdropPixels, 2000);
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
        File.WriteAllBytes("/tmp/nesunity-screen.png", textureBytes);

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

        File.WriteAllText("/tmp/nesunity-nametable.txt", sb.ToString());
    }

    private static Texture2D CreateScreenTexture(Nes nes)
    {
        nes.ppu.GenBackground(0);
        Texture2D texture = new Texture2D(Ppu.X_PIXELS, Ppu.Y_PIXELS, TextureFormat.RGBA32, false, false);
        int[] ppuPixels = nes.ppu.pixels;
        uint[] pixels = new uint[ppuPixels.Length];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = NesScreenView.rgbaPalette[ppuPixels[i]];
        texture.SetPixelData(pixels, 0);
        return texture;
    }

    private static void TickCpu(Nes nes, int tickCount)
    {
        using StreamWriter fs = File.CreateText("/tmp/nesunity-result.txt");

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
        var colors = new uint[16 * 16 * 4 * 8];
        int index = 0;
        for (int y = 0; y < 4 * 8; y++)
        {
            for (int x = 0; x < 16 * 16; x++)
            {
                int i = (3 - y / 8) * 16 + x / 16;
                colors[index++] = NesScreenView.rgbaPalette[i];
            }
        }
        texture.SetPixelData(colors, 0);
        File.WriteAllBytes("/tmp/nesunity-palette.png", texture.EncodeToPNG());
    }

}
