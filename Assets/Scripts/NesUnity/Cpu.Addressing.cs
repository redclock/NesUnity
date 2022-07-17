using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static NesUnity.AddressingMode;

namespace NesUnity
{
    public partial class Cpu
    {
        private OpcodeDef _currentInstruction;
        private byte _rmwValue;
        
        private int Address()
        {
            var def = _currentInstruction;
            int addr;
            switch (def.Mode)
            {
                case Immediate:
                    return PC++;
                case ZeroPage:
                    return NextByte();
                case Absolute:
                    return NextWord();
                case ZeroPageX:
                    return (NextByte() + X) & 0xFF;
                case ZeroPageY:
                    return (NextByte() + Y) & 0xFF;
                case AbsoluteX:
                    addr = NextWord();
                    if (def.PageBoundary && (addr & 0xFF00) != ((addr + X) & 0xFF00)) Cycle += 1;
                    return addr + X;
                case AbsoluteY:
                    addr = NextWord();
                    if (def.PageBoundary && (addr & 0xFF00) != ((addr + Y) & 0xFF00)) Cycle += 1;
                    return addr + Y;
                case IndirectX:
                    int off = (NextByte() + X) & 0xFF;
                    return _memory.ReadByte(off) | (_memory.ReadByte((off + 1) & 0xFF) << 8);
                case IndirectY:
                    off = NextByte() & 0xFF;
                    addr = _memory.ReadByte(off) | (_memory.ReadByte((off + 1) & 0xFF) << 8);
                    if (def.PageBoundary && (addr & 0xFF00) != ((addr + Y) & 0xFF00)) Cycle += 1;
                    return (addr + Y) & 0xFFFF;
            }
            throw new NotImplementedException("Address Mode " + def.Mode);
        }
        
        private byte AddressRead()
        {
            if (_currentInstruction.Mode == Direct) 
                return _rmwValue = A;
            
            int address = Address();
            return _rmwValue = _memory.ReadByte(address);
        }

        private void AddressWrite(byte val)
        {
            if (_currentInstruction.Mode == Direct)
            {
                A = val;
            } else
            {
                int address = Address();
                if (_currentInstruction.RMW)
                    _memory.WriteByte(address, _rmwValue);
                _memory.WriteByte(address, val);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte NextByte()
        {
            return _memory.ReadByte(PC++);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte NextWord()
        {
            byte low = _memory.ReadByte(PC++);
            byte high = _memory.ReadByte(PC++);
            return (byte) (low | (high << 8));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private sbyte NextSByte()
        {
            return (sbyte) NextByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushByte(byte val)
        {
            _memory.WriteByte(0x100 + SP, val);
            SP--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte PopByte()
        {
            SP++;
            return _memory.ReadByte(0x100 + SP);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushWord(int val)
        {
            _memory.WriteWord(0x100 - 1 + SP, val);
            SP -= 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PopWord()
        {
            SP += 2;
            int word = _memory.ReadWord(0x100 - 1 + SP);
            return word;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte PopP()
        {
            return P.FromByte(PopByte());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushPWithBreak()
        {
            PushByte((byte)(P.ToByte() | 0b00010000));
        }

    }
}