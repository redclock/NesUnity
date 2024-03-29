using System;
using System.Text;
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

        public Instruction[] Instructions => _instructions;

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
            if (_instructions[code] != null)
            {
                Debug.LogErrorFormat("Duplicated instruction {0:X2}", code);
            }
            var instruction = new Instruction();
            instruction.Name = name;
            instruction.Code = code;
            instruction.Mode = mode;
            instruction.Cycles = cycles;
            instruction.Func = func;
            instruction.PageBoundary = pageBoundary;
            instruction.Rmw = rmw;
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
            PC = pc;
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

        public string GetDisassembly(int pc)
        {
            byte opcode = _memory.ReadByte(pc);
            Instruction inst = _instructions[opcode];
            byte op1 = _memory.ReadByte(pc + 1);
            byte op2 = _memory.ReadByte(pc + 2);
            switch (inst.Mode)
            {
                case AddressingMode.Implicit:
                    return inst.Name;
                case AddressingMode.Accumulator:
                    return $"{inst.Name} A";
                case AddressingMode.Immediate:
                    return $"{inst.Name} #${op1:X2}";
                case AddressingMode.ZeroPage:
                    return $"{inst.Name} ${op1:X2}";
                case AddressingMode.Absolute:
                    return $"{inst.Name} ${op2:X2}{op1:X2}";
                case AddressingMode.ZeroPageX:
                    return $"{inst.Name} ${op1:X2},X";
                case AddressingMode.ZeroPageY:
                    return $"{inst.Name} ${op1:X2},Y";
                case AddressingMode.AbsoluteX:
                    return $"{inst.Name} ${op2:X2}{op1:X2},Y";
                case AddressingMode.AbsoluteY:
                    return $"{inst.Name} ${op2:X2}{op1:X2},X";
                case AddressingMode.Indirect:
                    return $"{inst.Name} ${op2:X2}{op1:X2}";
                case AddressingMode.IndirectX:
                    return $"{inst.Name} (${op2:X2}{op1:X2}),X";
                case AddressingMode.IndirectY:
                    return $"{inst.Name} (${op2:X2}{op1:X2}),Y";
                case AddressingMode.Relative:
                    return $"{inst.Name} ${pc + 2 + (sbyte)op1:X4}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

}