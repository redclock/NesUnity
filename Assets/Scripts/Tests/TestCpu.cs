using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NesUnity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NesUnity.Mappers;
using Debug = UnityEngine.Debug;

public class TestCpu
{
    [Test]
    public void TestNesInterruptVector()
    {
        byte[] bytes = File.ReadAllBytes( Application.streamingAssetsPath + "/smb.nes");
        var nes = new Nes();
        
        Assert.True(nes.PowerOn(bytes));
        int nmi = nes.cpu.Memory.GetInterruptVector(Interrupt.Nmi);
        int reset = nes.cpu.Memory.GetInterruptVector(Interrupt.Reset);
        int irq = nes.cpu.Memory.GetInterruptVector(Interrupt.Irq);
        
        Debug.LogFormat("NMI = ${0:X4} RST = ${1:X4} IRQ = ${2:X4}", nmi, reset, irq);
    }

    [Test]
    public void TestUnsupportedMapperFailsCleanly()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        var nes = new Nes();

        LogAssert.Expect(LogType.Error, "Unsupported Mapper 5");
        LogAssert.Expect(LogType.Error, "Nes error: unsupported mapper 5");
        Assert.False(nes.PowerOn(bytes));
    }

    [Test]
    public void TestCnromMapperSwitchesChrBank()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/dk3.nes");
        var nes = new Nes();

        Assert.True(nes.PowerOn(bytes));
        Assert.IsInstanceOf<CNROM>(nes.rom.mapper);
        Assert.AreEqual(0, ((CNROM)nes.rom.mapper).ChrBank);

        byte firstBank = nes.ppu.Memory.ReadByte(0x0001);
        nes.cpu.Memory.WriteByte(0x8000, 1);
        byte secondBank = nes.ppu.Memory.ReadByte(0x0001);

        Assert.AreEqual(1, ((CNROM)nes.rom.mapper).ChrBank);
        Assert.AreNotEqual(firstBank, secondBank);
    }

    [Test]
    public void TestUxromSwitchesPrgBankAndKeepsLastBankFixed()
    {
        const int prgBanks = 4;
        byte[] bytes = new byte[16 + prgBanks * 0x4000];
        bytes[0] = (byte)'N';
        bytes[1] = (byte)'E';
        bytes[2] = (byte)'S';
        bytes[3] = 0x1A;
        bytes[4] = prgBanks;
        bytes[5] = 0; // CHR RAM
        bytes[6] = 0x20; // Mapper 2

        for (int bank = 0; bank < prgBanks; bank++)
        {
            int offset = 16 + bank * 0x4000;
            for (int i = 0; i < 0x4000; i++)
                bytes[offset + i] = (byte)(0x10 + bank);
        }

        int vector = 16 + (prgBanks - 1) * 0x4000 + 0x3FFA;
        bytes[vector] = 0x00;
        bytes[vector + 1] = 0xC0;
        bytes[vector + 2] = 0x00;
        bytes[vector + 3] = 0xC0;
        bytes[vector + 4] = 0x00;
        bytes[vector + 5] = 0xC0;

        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        Assert.IsInstanceOf<UxROM>(nes.rom.mapper);
        Assert.AreEqual(0x10, nes.cpu.Memory.ReadByte(0x8000));
        Assert.AreEqual(0x13, nes.cpu.Memory.ReadByte(0xC000));

        nes.cpu.Memory.WriteByte(0x8000, 2);
        Assert.AreEqual(2, ((UxROM)nes.rom.mapper).PrgBank);
        Assert.AreEqual(0x12, nes.cpu.Memory.ReadByte(0x8000));
        Assert.AreEqual(0x13, nes.cpu.Memory.ReadByte(0xC000));

        nes.ppu.Memory.WriteByte(0x0010, 0xA5);
        Assert.AreEqual(0xA5, nes.ppu.Memory.ReadByte(0x0010));
    }

    [Test]
    public void TestMmc3BanksMirroringRamProtectionAndIrq()
    {
        byte[] bytes = CreateMmc3TestRom();
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        var mapper = (MMC3)nes.rom.mapper;

        // Reset mapping: switchable R6/R7, then the two fixed final banks.
        Assert.AreEqual(0x10, nes.cpu.Memory.ReadByte(0x8000));
        Assert.AreEqual(0x10, nes.cpu.Memory.ReadByte(0xA000));
        Assert.AreEqual(0x1E, nes.cpu.Memory.ReadByte(0xC000));
        Assert.AreEqual(0x1F, nes.cpu.Memory.ReadByte(0xE000));

        WriteMmc3(nes, 0x8000, 6);
        WriteMmc3(nes, 0x8001, 3);
        WriteMmc3(nes, 0x8000, 7);
        WriteMmc3(nes, 0x8001, 5);
        Assert.AreEqual(0x13, nes.cpu.Memory.ReadByte(0x8000));
        Assert.AreEqual(0x15, nes.cpu.Memory.ReadByte(0xA000));

        // PRG mode swaps the fixed second-last bank into $8000 and R6 into
        // $C000. The final bank remains fixed at $E000.
        WriteMmc3(nes, 0x8000, 0x46);
        Assert.AreEqual(0x1E, nes.cpu.Memory.ReadByte(0x8000));
        Assert.AreEqual(0x15, nes.cpu.Memory.ReadByte(0xA000));
        Assert.AreEqual(0x13, nes.cpu.Memory.ReadByte(0xC000));
        Assert.AreEqual(0x1F, nes.cpu.Memory.ReadByte(0xE000));

        // Configure all six CHR registers and verify both normal and inverted
        // 1 KiB layouts.
        WriteMmc3(nes, 0x8000, 0);
        WriteMmc3(nes, 0x8001, 4);
        WriteMmc3(nes, 0x8000, 1);
        WriteMmc3(nes, 0x8001, 8);
        WriteMmc3(nes, 0x8000, 2);
        WriteMmc3(nes, 0x8001, 10);
        WriteMmc3(nes, 0x8000, 3);
        WriteMmc3(nes, 0x8001, 11);
        WriteMmc3(nes, 0x8000, 4);
        WriteMmc3(nes, 0x8001, 12);
        WriteMmc3(nes, 0x8000, 5);
        WriteMmc3(nes, 0x8001, 13);
        Assert.AreEqual(0x44, nes.ppu.Memory.ReadByte(0x0000));
        Assert.AreEqual(0x45, nes.ppu.Memory.ReadByte(0x0400));
        Assert.AreEqual(0x48, nes.ppu.Memory.ReadByte(0x0800));
        Assert.AreEqual(0x49, nes.ppu.Memory.ReadByte(0x0C00));
        Assert.AreEqual(0x4A, nes.ppu.Memory.ReadByte(0x1000));
        Assert.AreEqual(0x4B, nes.ppu.Memory.ReadByte(0x1400));
        Assert.AreEqual(0x4C, nes.ppu.Memory.ReadByte(0x1800));
        Assert.AreEqual(0x4D, nes.ppu.Memory.ReadByte(0x1C00));

        WriteMmc3(nes, 0x8000, 0x80);
        Assert.AreEqual(0x4A, nes.ppu.Memory.ReadByte(0x0000));
        Assert.AreEqual(0x4B, nes.ppu.Memory.ReadByte(0x0400));
        Assert.AreEqual(0x4C, nes.ppu.Memory.ReadByte(0x0800));
        Assert.AreEqual(0x4D, nes.ppu.Memory.ReadByte(0x0C00));
        Assert.AreEqual(0x44, nes.ppu.Memory.ReadByte(0x1000));
        Assert.AreEqual(0x45, nes.ppu.Memory.ReadByte(0x1400));

        WriteMmc3(nes, 0xA000, 1);
        nes.ppu.Memory.WriteByte(0x2000, 0xA1);
        Assert.AreEqual(0xA1, nes.ppu.Memory.ReadByte(0x2400));
        WriteMmc3(nes, 0xA001, 0xC0);
        nes.cpu.Memory.WriteByte(0x6000, 0x5A);
        Assert.AreEqual(0, nes.cpu.Memory.ReadByte(0x6000));
        WriteMmc3(nes, 0xA001, 0x80);
        nes.cpu.Memory.WriteByte(0x6000, 0x5A);
        Assert.AreEqual(0x5A, nes.cpu.Memory.ReadByte(0x6000));

        // The scanline hook clocks the IRQ counter for this scanline renderer.
        nes.cpu.P.IrqDisable = false;
        WriteMmc3(nes, 0xC000, 2);
        WriteMmc3(nes, 0xC001, 0);
        WriteMmc3(nes, 0xE001, 0);
        nes.ppu.WriteRegister(1, 0x18);
        for (int i = 0; i < 341 * 3; i++)
            nes.ppu.Tick();
        Assert.AreEqual(0xC000, nes.cpu.PC);
        Assert.True(nes.cpu.P.IrqDisable);
        Assert.True(mapper.IrqEnabled);
    }

    private static void WriteMmc3(Nes nes, int address, int value)
    {
        nes.cpu.Memory.WriteByte(address, (byte)value);
    }

    private static byte[] CreateMmc3TestRom()
    {
        const int prgBanks = 8;
        const int chrBanks = 8;
        byte[] bytes = new byte[16 + prgBanks * 0x4000 + chrBanks * 0x2000];
        bytes[0] = (byte)'N';
        bytes[1] = (byte)'E';
        bytes[2] = (byte)'S';
        bytes[3] = 0x1A;
        bytes[4] = prgBanks;
        bytes[5] = chrBanks;
        bytes[6] = 0x40; // Mapper 4, horizontal mirroring from the header.

        int prgOffset = 16;
        for (int bank = 0; bank < prgBanks * 2; bank++)
        {
            int offset = prgOffset + bank * 0x2000;
            for (int i = 0; i < 0x2000; i++)
                bytes[offset + i] = (byte)(0x10 + bank);
        }

        int chrOffset = prgOffset + prgBanks * 0x4000;
        for (int bank = 0; bank < chrBanks * 8; bank++)
        {
            int offset = chrOffset + bank * 0x400;
            for (int i = 0; i < 0x400; i++)
                bytes[offset + i] = (byte)(0x40 + bank);
        }

        int vector = prgOffset + prgBanks * 0x4000 - 0x4000 + 0x3FFA;
        bytes[vector] = 0x00;
        bytes[vector + 1] = 0xC0;
        bytes[vector + 2] = 0x00;
        bytes[vector + 3] = 0xC0;
        bytes[vector + 4] = 0x00;
        bytes[vector + 5] = 0xC0;
        return bytes;
    }

    [Test]
    public void TestMmc1SerialWritesSwitchPrgAndChrBanks()
    {
        const int prgBanks = 4;
        const int chrBanks = 2;
        byte[] bytes = new byte[16 + prgBanks * 0x4000 + chrBanks * 0x2000];
        bytes[0] = (byte)'N';
        bytes[1] = (byte)'E';
        bytes[2] = (byte)'S';
        bytes[3] = 0x1A;
        bytes[4] = prgBanks;
        bytes[5] = chrBanks;
        bytes[6] = 0x10; // Mapper 1

        for (int bank = 0; bank < prgBanks; bank++)
        {
            int offset = 16 + bank * 0x4000;
            for (int i = 0; i < 0x4000; i++)
                bytes[offset + i] = (byte)(0x20 + bank);
        }

        int chrOffset = 16 + prgBanks * 0x4000;
        for (int bank = 0; bank < chrBanks; bank++)
        {
            int offset = chrOffset + bank * 0x2000;
            for (int i = 0; i < 0x2000; i++)
                bytes[offset + i] = (byte)(0x60 + bank);
        }

        int vector = 16 + (prgBanks - 1) * 0x4000 + 0x3FFA;
        bytes[vector] = 0x00;
        bytes[vector + 1] = 0xC0;
        bytes[vector + 2] = 0x00;
        bytes[vector + 3] = 0xC0;
        bytes[vector + 4] = 0x00;
        bytes[vector + 5] = 0xC0;

        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        Assert.IsInstanceOf<MMC1>(nes.rom.mapper);
        var mapper = (MMC1)nes.rom.mapper;
        Assert.AreEqual(0x20, nes.cpu.Memory.ReadByte(0x8000));
        Assert.AreEqual(0x23, nes.cpu.Memory.ReadByte(0xC000));

        WriteMmc1Register(nes, 0xE000, 2);
        Assert.AreEqual(2, mapper.PrgBank);
        Assert.AreEqual(0x22, nes.cpu.Memory.ReadByte(0x8000));
        Assert.AreEqual(0x23, nes.cpu.Memory.ReadByte(0xC000));

        WriteMmc1Register(nes, 0x8000, 0x1C);
        WriteMmc1Register(nes, 0xA000, 2);
        Assert.AreEqual(0x61, nes.ppu.Memory.ReadByte(0x0000));
    }

    [Test]
    public void TestMmc1DynamicMirroringAndPrgRamProtection()
    {
        byte[] bytes = CreateMmc1TestRom();
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        var mapper = (MMC1)nes.rom.mapper;

        // Control = 2: vertical mirroring (NT0=NT2, NT1=NT3).
        WriteMmc1Register(nes, 0x8000, 2);
        nes.ppu.Memory.WriteByte(0x2000, 0x11);
        nes.ppu.Memory.WriteByte(0x2400, 0x22);
        Assert.AreEqual(0x11, nes.ppu.Memory.ReadByte(0x2800));
        Assert.AreEqual(0x22, nes.ppu.Memory.ReadByte(0x2C00));

        // Control = 3: horizontal mirroring (NT0=NT1, NT2=NT3).
        WriteMmc1Register(nes, 0x8000, 3);
        nes.ppu.Memory.WriteByte(0x2000, 0x33);
        Assert.AreEqual(0x33, nes.ppu.Memory.ReadByte(0x2400));
        nes.ppu.Memory.WriteByte(0x2800, 0x44);
        Assert.AreEqual(0x44, nes.ppu.Memory.ReadByte(0x2C00));

        // Control = 0/1: one-screen lower/upper mirroring.
        WriteMmc1Register(nes, 0x8000, 0);
        nes.ppu.Memory.WriteByte(0x2000, 0x55);
        Assert.AreEqual(0x55, nes.ppu.Memory.ReadByte(0x2C00));
        WriteMmc1Register(nes, 0x8000, 1);
        nes.ppu.Memory.WriteByte(0x2000, 0x66);
        Assert.AreEqual(0x66, nes.ppu.Memory.ReadByte(0x2400));

        nes.cpu.Memory.WriteByte(0x6000, 0xA5);
        Assert.AreEqual(0xA5, nes.cpu.Memory.ReadByte(0x6000));
        WriteMmc1Register(nes, 0xE000, 0x10);
        nes.cpu.Memory.WriteByte(0x6000, 0x5A);
        Assert.AreEqual(0, nes.cpu.Memory.ReadByte(0x6000));
        Assert.False(mapper.PrgRamEnabled);
    }

    private static void WriteMmc1Register(Nes nes, int address, int value)
    {
        for (int bit = 0; bit < 5; bit++)
            nes.cpu.Memory.WriteByte(address, (byte)((value >> bit) & 1));
    }

    private static byte[] CreateMmc1TestRom()
    {
        const int prgBanks = 4;
        const int chrBanks = 2;
        byte[] bytes = new byte[16 + prgBanks * 0x4000 + chrBanks * 0x2000];
        bytes[0] = (byte)'N';
        bytes[1] = (byte)'E';
        bytes[2] = (byte)'S';
        bytes[3] = 0x1A;
        bytes[4] = prgBanks;
        bytes[5] = chrBanks;
        bytes[6] = 0x10;

        for (int bank = 0; bank < prgBanks; bank++)
        {
            int offset = 16 + bank * 0x4000;
            for (int i = 0; i < 0x4000; i++)
                bytes[offset + i] = (byte)(0x20 + bank);
        }

        int chrOffset = 16 + prgBanks * 0x4000;
        for (int bank = 0; bank < chrBanks; bank++)
        {
            int offset = chrOffset + bank * 0x2000;
            for (int i = 0; i < 0x2000; i++)
                bytes[offset + i] = (byte)(0x60 + bank);
        }

        int vector = 16 + (prgBanks - 1) * 0x4000 + 0x3FFA;
        bytes[vector] = 0x00;
        bytes[vector + 1] = 0xC0;
        bytes[vector + 2] = 0x00;
        bytes[vector + 3] = 0xC0;
        bytes[vector + 4] = 0x00;
        bytes[vector + 5] = 0xC0;
        return bytes;
    }

    [Test]
    public void TestControllerSerialRead()
    {
        var controller = new NesController();
        controller.SetButton(NesController.Button.A, true);
        controller.SetButton(NesController.Button.Up, true);
        controller.SetButton(NesController.Button.Right, true);

        controller.Write(1);
        controller.Write(0);
        Assert.AreEqual(1, controller.Read());

        Assert.AreEqual(0, controller.Read());
        Assert.AreEqual(0, controller.Read());
        Assert.AreEqual(0, controller.Read());
        Assert.AreEqual(1, controller.Read());
        Assert.AreEqual(0, controller.Read());
        Assert.AreEqual(0, controller.Read());
        Assert.AreEqual(1, controller.Read());
        Assert.AreEqual(1, controller.Read());
    }

    [Test]
    public void TestControllerStartBitOrder()
    {
        var controller = new NesController();
        controller.SetButton(NesController.Button.Start, true);
        controller.Write(1);
        controller.Write(0);
        Assert.AreEqual(0, controller.Read());
        Assert.AreEqual(0, controller.Read());
        Assert.AreEqual(0, controller.Read());
        Assert.AreEqual(1, controller.Read());
    }

    [Test]
    public void TestControllerStartRawSequence()
    {
        var controller = new NesController();
        controller.SetButton(NesController.Button.Start, true);
        controller.Write(1);
        controller.Write(0);
        int value = 0;
        for (int i = 0; i < 8; i++)
            value |= controller.Read() << i;
        Assert.AreEqual(0x08, value);
    }

    [Test]
    public void TestControllerCpuPortKeepsOpenBusBits()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        nes.Controller1.SetButton(NesController.Button.A, true);
        nes.cpu.Memory.WriteByte(0x4016, 1);
        nes.cpu.Memory.WriteByte(0x4016, 0);
        Assert.AreEqual(0x41, nes.cpu.Memory.ReadByte(0x4016));
        Assert.AreEqual(0x40, nes.cpu.Memory.ReadByte(0x4017));
    }

    [Test]
    public void TestApuPulseRegistersAndFrameCounter()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));

        nes.cpu.Memory.WriteByte(0x4000, 0x3F); // duty 0, constant volume 15
        nes.cpu.Memory.WriteByte(0x4002, 0x20);
        nes.cpu.Memory.WriteByte(0x4003, 0x08); // length index 1
        nes.cpu.Memory.WriteByte(0x4015, 0x01);

        Assert.AreEqual(1, nes.cpu.Memory.ReadByte(0x4015) & 1);
        Assert.AreEqual(0, nes.cpu.Memory.ReadByte(0x4015) & 2);

        nes.cpu.Memory.WriteByte(0x4017, 0xC0);
        Assert.True(nes.apu.FrameCounter5Step);
        Assert.True(nes.apu.IrqInhibit);

        for (int i = 0; i < 100; i++)
            nes.Tick();

        float[] samples = new float[32];
        int count = nes.apu.DrainSamples(samples);
        Assert.Greater(count, 0);
        Assert.GreaterOrEqual(nes.apu.PendingSampleCount, 0);
    }

    [Test]
    public void TestApuPulse2TriangleNoiseAndStatus()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));

        // Pulse 2 occupies $4004-$4007 (Pulse 1 must remain disabled).
        nes.cpu.Memory.WriteByte(0x4004, 0x3F); // constant volume 15
        nes.cpu.Memory.WriteByte(0x4006, 0x20);
        nes.cpu.Memory.WriteByte(0x4007, 0x08);
        nes.cpu.Memory.WriteByte(0x4015, 0x02);
        Assert.AreEqual(0x02, nes.cpu.Memory.ReadByte(0x4015) & 0x0F);

        // Triangle: control/linear reload, timer low, timer high + length.
        nes.cpu.Memory.WriteByte(0x4008, 0xFF);
        nes.cpu.Memory.WriteByte(0x400A, 0x08);
        nes.cpu.Memory.WriteByte(0x400B, 0x08);

        // Noise: constant volume, short period, length load.
        nes.cpu.Memory.WriteByte(0x400C, 0x1F);
        nes.cpu.Memory.WriteByte(0x400E, 0x00);
        nes.cpu.Memory.WriteByte(0x400F, 0x08);
        nes.cpu.Memory.WriteByte(0x4015, 0x0E);
        Assert.AreEqual(0x0E, nes.cpu.Memory.ReadByte(0x4015) & 0x0F);

        // DMC registers are accepted as a safe silent stub in this milestone;
        // no sample bytes are active, so status bit 4 remains clear.
        nes.cpu.Memory.WriteByte(0x4010, 0x0F);
        nes.cpu.Memory.WriteByte(0x4011, 0x20);
        nes.cpu.Memory.WriteByte(0x4012, 0x40);
        nes.cpu.Memory.WriteByte(0x4013, 0x10);
        nes.cpu.Memory.WriteByte(0x4015, 0x1E);
        Assert.AreEqual(0, nes.cpu.Memory.ReadByte(0x4015) & 0x10);

        // A five-step write clocks the linear counter immediately, so the
        // triangle produces samples without waiting for the first quarter frame.
        nes.cpu.Memory.WriteByte(0x4017, 0x80);
        for (int i = 0; i < 2000; i++)
            nes.Tick();

        float[] samples = new float[128];
        int count = nes.apu.DrainSamples(samples);
        Assert.Greater(count, 0);
        Assert.IsTrue(samples.Take(count).Any(sample => Math.Abs(sample) > 0.0001f));

        // Disabling a channel clears its length counter and the corresponding
        // $4015 status bit, matching the hardware enable register behaviour.
        nes.cpu.Memory.WriteByte(0x4015, 0);
        Assert.AreEqual(0, nes.cpu.Memory.ReadByte(0x4015) & 0x0F);
    }

    [Test]
    public void TestApuAudioBufferCopiesChannelsAndPadsUnderrun()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));

        nes.cpu.Memory.WriteByte(0x4000, 0xBF); // duty 2, constant volume 15
        nes.cpu.Memory.WriteByte(0x4002, 0x20);
        nes.cpu.Memory.WriteByte(0x4003, 0x08);
        nes.cpu.Memory.WriteByte(0x4015, 0x01);
        for (int i = 0; i < 2000; i++)
            nes.Tick();

        int pending = nes.apu.PendingSampleCount;
        Assert.Greater(pending, 0);
        float[] stereo = Enumerable.Repeat(float.NaN, (pending + 8) * 2).ToArray();
        nes.apu.FillAudioBuffer(stereo, 2);

        for (int frame = 0; frame < pending; frame++)
            Assert.AreEqual(stereo[frame * 2], stereo[frame * 2 + 1]);
        for (int frame = pending; frame < pending + 8; frame++)
        {
            Assert.AreEqual(0f, stereo[frame * 2]);
            Assert.AreEqual(0f, stereo[frame * 2 + 1]);
        }
        Assert.AreEqual(0, nes.apu.PendingSampleCount);
    }

    [Test]
    public void TestApuSampleRateMatchesCpuClock()
    {
        var apu = new Apu();
        apu.Reset();
        int cycles = Apu.CpuClockHz / 10;
        for (int i = 0; i < cycles; i++)
            apu.Tick();

        int expected = (int)Math.Round(cycles * (double)Apu.SampleRate / Apu.CpuClockHz);
        Assert.That(apu.PendingSampleCount, Is.InRange(expected - 1, expected + 1));
    }

    [Test]
    public void TestApuUnderrunIsRecordedAndPadded()
    {
        var apu = new Apu();
        apu.Reset();
        float[] buffer = Enumerable.Repeat(float.NaN, 512).ToArray();
        apu.FillAudioBuffer(buffer, 1);

        Assert.AreEqual(1, apu.AudioUnderrunCount);
        Assert.AreEqual(0, apu.AudioOverrunCount);
        Assert.IsTrue(buffer.All(sample => sample == 0f));
    }

    [Test]
    public void TestApuHighWatermarkReadsWithoutUnderrun()
    {
        var apu = new Apu();
        apu.Reset();
        apu.WriteRegister(0x4000, 0xBF);
        apu.WriteRegister(0x4002, 0x20);
        apu.WriteRegister(0x4003, 0x08);
        apu.WriteRegister(0x4015, 0x01);

        for (int i = 0; i < (int)Math.Round(29780.5 * 7); i++)
            apu.Tick();

        Assert.That(apu.PendingSampleCount, Is.InRange(5135, 5145));
        float[] block = new float[512];
        int underrunsBefore = apu.AudioUnderrunCount;
        for (int i = 0; i < 9; i++)
            apu.FillAudioBuffer(block, 1);
        Assert.AreEqual(underrunsBefore, apu.AudioUnderrunCount);
        Assert.AreEqual(0, apu.AudioOverrunCount);
    }

    [Test]
    public void TestApuLongProducerConsumerRunStaysBounded()
    {
        var apu = new Apu();
        apu.Reset();
        float[] block = new float[700];
        for (int frame = 0; frame < 100; frame++)
        {
            for (int cycle = 0; cycle < 29829; cycle++)
                apu.Tick();
            apu.FillAudioBuffer(block, 1);
        }

        Assert.That(apu.PendingSampleCount, Is.GreaterThanOrEqualTo(0));
        Assert.That(apu.PendingSampleCount, Is.LessThanOrEqualTo(8192));
        Assert.AreEqual(0, apu.AudioUnderrunCount);
        Assert.AreEqual(0, apu.AudioOverrunCount);
    }

    [Test]
    public void TestSmbRomApuMappingProducesSamples()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/smb.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        nes.cpu.Memory.WriteByte(0x4000, 0xBF);
        nes.cpu.Memory.WriteByte(0x4002, 0x20);
        nes.cpu.Memory.WriteByte(0x4003, 0x08);
        nes.cpu.Memory.WriteByte(0x4015, 0x01);
        float maxSample = 0f;
        float[] frameSamples = new float[1024];
        for (int cycle = 0; cycle < 100000; cycle++)
        {
            nes.apu.Tick();
            if ((cycle & 0x7FF) == 0)
            {
                int frameSampleCount = nes.apu.DrainSamples(frameSamples);
                if (frameSampleCount > 0)
                    maxSample = Math.Max(maxSample, frameSamples.Take(frameSampleCount).Select(Math.Abs).Max());
            }
        }
        int remainingSamples = nes.apu.DrainSamples(frameSamples);
        if (remainingSamples > 0)
            maxSample = Math.Max(maxSample, frameSamples.Take(remainingSamples).Select(Math.Abs).Max());
        Assert.IsTrue(maxSample > 0.0001f,
            "The SMB ROM environment produced no non-zero APU samples.");
    }

    [Test]
    public void TestCpuHasAllInstructions()
    {
        Cpu cpu = new Cpu(null);

        for (int i = 0; i < 256; i++)
        {
            if (cpu.Instructions[i] == null)
            {
                Debug.LogErrorFormat("Unsupported instruction {0:X2}", i);
            }
        }
    }

    [Test]
    public void TestCpuOpsWithStandardLog()
    {
        byte[] bytes = File.ReadAllBytes( Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes, 0xC000));
        var cpu = nes.cpu;
        int index = 0;
        var logs = ReadNesTestLog();
        
        using StreamWriter fs = File.CreateText("/tmp/nesunity-cpu-result.txt");
        bool isFinished = false;
        cpu.OnBeforeExecute = () =>
        {
            fs.Write($"{cpu.PC:X4} {cpu.GetCurOp().Name} P:{cpu.P.ToByte():X2}");
            if (index >= logs.Length)
            {
                index++;
                isFinished = true;
                return;
            }
            var log = logs[index++];
            string tag = $"Line {index + 1}";
            Assert.AreEqual(log.pc, cpu.PC, tag + " PC");
            Assert.AreEqual(log.a, cpu.A, tag + " A");
            Assert.AreEqual(log.x, cpu.X, tag + " X");
            Assert.AreEqual(log.y, cpu.Y, tag + " Y");
            Assert.AreEqual(log.p, cpu.P.ToByte(), tag + " P");
            Assert.AreEqual(log.sp, cpu.SP, tag + " SP");
            Assert.AreEqual(log.cycle, cpu.TotalCycle, tag + " CYCLE");
        };
        cpu.OnEndExecute = () =>
        {
            fs.WriteLine($" M:{cpu.CurrentOpAddress:X4}");
            fs.Flush();
        };
        Stopwatch sw = new Stopwatch();
        sw.Start();
        while (!cpu.Halted)
        {
            cpu.Tick();
        }
        Debug.Log(cpu.Memory.ReadWord(0x02));

        Debug.Log("sw = " + sw.ElapsedMilliseconds);
    }

    class LogLine
    {
        public int pc;
        public byte[] codes;
        public string opcode;
        public byte a;
        public byte x;
        public byte y;
        public byte p;
        public byte sp;
        public int ppuFrame;
        public int ppuCycle;
        public int cycle;
    }
    

    private LogLine[] ReadNesTestLog()
    {
        string[] lines = File.ReadAllLines( Application.streamingAssetsPath + "/nestest.log.txt");

        LogLine[] logs = lines.Select((str) =>
        {
            LogLine log = new LogLine();
            log.pc = Convert.ToInt32(str.Substring(0, 4), 16);
            log.codes = str.Substring(6, 8)
                .Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries)
                .Select((s) => Convert.ToByte(s, 16)).ToArray();
            log.opcode = str.Substring(16, 3);
            log.a = Convert.ToByte(str.Substring(50, 2), 16);
            log.x = Convert.ToByte(str.Substring(55, 2), 16);
            log.y = Convert.ToByte(str.Substring(60, 2), 16);
            log.p = Convert.ToByte(str.Substring(65, 2), 16);
            log.sp = Convert.ToByte(str.Substring(71, 2), 16);
            log.ppuFrame = Convert.ToInt32(str.Substring(78, 3).Trim());
            log.ppuCycle = Convert.ToInt32(str.Substring(82, 3).Trim());
            log.cycle = Convert.ToInt32(str.Substring(90, str.Length - 90).Trim());
            return log;
        }).ToArray();

        return logs;
    }

}
