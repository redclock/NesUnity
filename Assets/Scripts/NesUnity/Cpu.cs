using System;
using System.Reflection;
using NesUnity.Mappers;
using UnityEngine;

namespace NesUnity
{

    public partial class Cpu
    {
        private CpuMemory _memory;
        private Instruction[] _instructions = new Instruction[256];
        public int Cycle;
        public int TotalCycle;
        public Action OnBeforeExecute;
        public Action OnEndExecute;

        public Cpu(MapperBase mapper)
        {
            _memory = new CpuMemory(mapper);
            InitInstructions();
        }

        public bool Tick()
        {
            TotalCycle++;
            if (Cycle > 1)
            {
                Cycle--;
                return false;
            }

            Cycle = 0;
            OnBeforeExecute?.Invoke();
            ExecuteOpcode();
            OnEndExecute?.Invoke();
            return true;
        }

        private void ExecuteOpcode()
        {
            byte opcode = _memory.ReadByte(PC++);
            _currentInstruction = _instructions[opcode];
            //Debug.LogFormat("${0:X4} {1} CYCLE {2}", PC - 1, _currentInstruction.Name, TotalCycle);
            UpdateAddress();
            _currentInstruction.Func();
            Cycle += _currentInstruction.Cycles;
        }
        
        private void Op(string name, byte code, Instruction.OpFunc func, AddressingMode mode, int cycles, bool pageBoundary = false, bool rmw = false)
        {
            var instruction = new Instruction();
            instruction.Name = name;
            instruction.Code = code;
            instruction.Mode = mode;
            instruction.Cycles = cycles;
            instruction.Func = func;
            instruction.PageBoundary = pageBoundary;
            instruction.RMW = rmw;
            _instructions[code] = instruction;
        }

        public void Reset(int pc)
        {
            A = 0;
            X = 0;
            Y = 0;
            P.FromByte(0b00100100); // IrqDisable = true
            SP = 0xFD;
            PC = pc;
            Cycle = 7;
            TotalCycle = 0;
        }

        private void TriggerInterrupt(Interrupt interrupt)
        {
            if (interrupt == Interrupt.Irq && P.IrqDisable)
                return;

            PushWord(PC);
            PushByte(P.ToByte());
            PC = _memory.GetInterruptVector(interrupt);
            P.IrqDisable = true;
            Cycle += 7;
        }

        public Instruction GetCurOp()
        {
            return _instructions[_memory.ReadByte(PC)];
        }
    }

}