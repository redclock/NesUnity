using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NesUnity;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TestCpu
{
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

    [Test]
    public void TestCpuOpsWithStandardLog()
    {
        byte[] bytes = File.ReadAllBytes( Application.streamingAssetsPath + "/nestest.nes");
        var nes = new NesRom();
        Assert.True(nes.ReadFromBytes(bytes));
        Cpu cpu = new Cpu(nes.mapper);
        cpu.Reset(0xC000);
        int index = 0;
        var logs = ReadNesTestLog();
        
        //using StreamWriter fs = File.CreateText("result.txt");
        
        cpu.OnBeforeExecute = () =>
        {
        //    fs.Write($"{cpu.PC:X4} {cpu.GetCurOp().Name} P:{cpu.P.ToByte():X2}");
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
        // cpu.OnEndExecute = () =>
        // {
        //     fs.WriteLine($" M:{cpu.CurrentOpAddress:X4}");
        //     fs.Flush();
        // };
        Stopwatch sw = new Stopwatch();
        sw.Start();
        for (int i = 0; i < 10000; i++)
        {
            cpu.Tick();
        }

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
