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
        private bool _halted;
        private Nes _nesSys;
        
        public int Cycle;
        public int TotalCycle;
        public Action OnBeforeExecute;
        public Action OnEndExecute;
        
        public bool Halted => _halted;
        public Nes NesSys => _nesSys;
        public CpuMemory Memory => _memory;
        public Cpu(Nes nes)
        {
            _nesSys = nes;
            InitInstructions();
        }

        public bool Tick()
        {
            if (Halted)
                return false;
            
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
            // Fetch op code
            byte opcode = _memory.ReadByte(PC++);
            
            // Find Instruction
            _currentInstruction = _instructions[opcode];
            
            //Debug.LogFormat("${0:X4} {1} CYCLE {2}", PC - 1, _currentInstruction.Name, TotalCycle);
            
            // Resolve address mode
            if (_currentInstruction.Mode == AddressingMode.Accumulator || _currentInstruction.Mode == AddressingMode.Implicit)
                _currentOpAddress = -1;
            else
                _currentOpAddress = Address(_currentInstruction.Mode, _currentInstruction.PageBoundary);
            
            // Add cycles
            Cycle += _currentInstruction.Cycles;
            
            // Execute function
            _currentInstruction.Func();
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

        public void Reset(int pc = -1)
        {
            _memory = new CpuMemory(this);
            A = 0;
            X = 0;
            Y = 0;
            P.FromByte(0b00100100); // IrqDisable = true
            SP = 0xFD;
            Cycle = 7;
            TotalCycle = 0;
            if (pc < 0)
                PC = _memory.GetInterruptVector(Interrupt.Reset);
            _halted = false;
        }

        public void TriggerInterrupt(Interrupt interrupt)
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