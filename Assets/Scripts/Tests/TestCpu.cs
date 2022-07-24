using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NesUnity;
using NUnit.Framework;
using UnityEngine;
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
        
        using StreamWriter fs = File.CreateText("result.txt");
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
