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

        public Cpu(MapperBase mapper)
        {
            _memory = new CpuMemory(mapper);
            InitInstructions();
        }
  
        private void Op(string name, byte code, Instruction.OpFunc func, AddressingMode mode, int cycles, bool pageBoundary = false, bool rmw = false)
        {
            var instruction = _instructions[code];
            instruction.Name = name;
            instruction.Code = code;
            instruction.Mode = mode;
            instruction.Cycles = cycles;
            instruction.Func = func;
            instruction.PageBoundary = pageBoundary;
            instruction.RMW = rmw;
        }

    }

}