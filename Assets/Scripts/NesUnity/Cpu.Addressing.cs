using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static NesUnity.AddressingMode;

namespace NesUnity
{
    public partial class Cpu
    {
        private Instruction _currentInstruction;
        private byte _rmwValue;
        private int _currentOpAddress;

        private int UpdateAddress()
        {
            if (_currentInstruction.Mode == Accumulator || _currentInstruction.Mode == Implicit)
                _currentOpAddress = -1;
            else
                _currentOpAddress = Address(_currentInstruction.Mode, _currentInstruction.PageBoundary);
            
            return _currentOpAddress;
        }
        
        private int Address(AddressingMode mode, bool checkPageBoundary)
        {
            int addr;
            switch (mode)
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
                    if (checkPageBoundary && (addr & 0xFF00) != ((addr + X) & 0xFF00)) Cycle += 1;
                    return addr + X;
                case AbsoluteY:
                    addr = NextWord();
                    if (checkPageBoundary && (addr & 0xFF00) != ((addr + Y) & 0xFF00)) Cycle += 1;
                    return addr + Y;
                case Indirect:
                    int off = NextWord();
                    // AN INDIRECT JUMP MUST NEVER USE A VECTOR BEGINNING ON THE LAST BYTE OF A PAGE
                    //
                    // If address $3000 contains $40, $30FF contains $80, and $3100 contains $50, 
                    // the result of JMP ($30FF) will be a transfer of control to $4080 rather than
                    // $5080 as you intended i.e. the 6502 took the low byte of the address from
                    // $30FF and the high byte from $3000.
                    //
                    // http://www.6502.org/tutorials/6502opcodes.html
                    int hi = (off & 0xFF00) | ((off + 1) & 0xFF);
                    if (checkPageBoundary && (off & 0xFF00) != (hi & 0xFF00)) Cycle += 1;
                    addr = _memory.ReadByte(off) | (_memory.ReadByte(hi) << 8);
                    return addr;
                case IndirectX:
                    off = (NextByte() + X) & 0xFF;
                    return _memory.ReadByte(off) | (_memory.ReadByte((off + 1) & 0xFF) << 8);
                case IndirectY:
                    off = NextByte() & 0xFF;
                    off = _memory.ReadByte(off) | (_memory.ReadByte((off + 1) & 0xFF) << 8);
                    if (checkPageBoundary && (off & 0xFF00) != ((off + Y) & 0xFF00)) Cycle += 1;
                    return (off + Y) & 0xFFFF;
                case Relative:
                    return NextSByte();
            }
            throw new NotImplementedException("Address Mode " + mode);
        }
        
        private byte AddressRead()
        {
            Debug.Assert(_currentInstruction.Mode != Implicit); 
            if (_currentInstruction.Mode == Accumulator) 
                return _rmwValue = A;
            
            Debug.Assert(_currentOpAddress >= 0);
            return _rmwValue = _memory.ReadByte(_currentOpAddress);
        }

        private void AddressWrite(byte val)
        {
            Debug.Assert(_currentInstruction.Mode != Implicit); 
            if (_currentInstruction.Mode == Accumulator)
            {
                A = val;
            } else
            {
                Debug.Assert(_currentOpAddress >= 0);
                if (_currentInstruction.RMW)
                    _memory.WriteByte(_currentOpAddress, _rmwValue);
                _memory.WriteByte(_currentOpAddress, val);
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