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
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40);
        var nes = new Nes();

        LogAssert.Expect(LogType.Error, "Unsupported Mapper 4");
        LogAssert.Expect(LogType.Error, "Nes error: unsupported mapper 4");
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

    private static void WriteMmc1Register(Nes nes, int address, int value)
    {
        for (int bit = 0; bit < 5; bit++)
            nes.cpu.Memory.WriteByte(address, (byte)((value >> bit) & 1));
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
    public void TestControllerCpuPortKeepsOpenBusBits()
    {
        byte[] bytes = File.ReadAllBytes(Application.streamingAssetsPath + "/nestest.nes");
        var nes = new Nes();
        Assert.True(nes.PowerOn(bytes));
        nes.Controller1.SetButton(NesController.Button.A, true);
        nes.cpu.Memory.WriteByte(0x4016, 1);
        nes.cpu.Memory.WriteByte(0x4016, 0);
        Assert.AreEqual(0x41, nes.cpu.Memory.ReadByte(0x4016));
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
