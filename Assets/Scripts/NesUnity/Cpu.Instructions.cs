using System;
using static NesUnity.AddressingMode;
// ReSharper disable InconsistentNaming

namespace NesUnity
{
    public partial class Cpu
    {
        
        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
        public class OpcodeDef : Attribute
        {
            public byte Opcode;
            public int Cycles;
            public bool PageBoundary;
            public bool RMW;
            public AddressingMode Mode;
        }

        [OpcodeDef(Opcode = 0x20, Mode = Absolute, Cycles = 6)]
        private void JSR()
        {
            // JSR - Jump to Subroutine
            // Push address of next instruction - 1,
            PushWord(PC - 1);
            PC = _currentOpAddress;
        }

        [OpcodeDef(Opcode = 0x40, Mode = Implicit, Cycles = 6)]
        private void RTI()
        {
            // RTI - Return from Interrupt
            // Return from interrupt. Pull status and PC from stack.
            PopP();
            PC = PopWord();
        }

        [OpcodeDef(Opcode = 0x60, Mode = Implicit, Cycles = 6)]
        private void RTS()
        {
            // RTS - Return from Subroutine
            PC = PopWord() + 1;
        }

        [OpcodeDef(Opcode = 0xC8, Mode = Implicit, Cycles = 2)]
        private void INY()
        {
            // INY - Increment Y Register
            unchecked
            {
                P.SetZN(++Y);
            }
        }

        [OpcodeDef(Opcode = 0x88, Mode = Implicit, Cycles = 2)]
        private void DEY()
        {
            // DEY - Decrement Y Register
            unchecked
            {
                P.SetZN(--Y);    
            }
        }

        [OpcodeDef(Opcode = 0xE8, Mode = Implicit, Cycles = 2)]
        private void INX()
        {
            // INX - Increment X Register
            unchecked
            {
                P.SetZN(++X);    
            }
        }

        [OpcodeDef(Opcode = 0xCA, Mode = Implicit, Cycles = 2, RMW = true)]
        private void DEX()
        {
            // DEX - Decrement X Register
            unchecked
            {
                P.SetZN(--X);    
            }
        }

        [OpcodeDef(Opcode = 0xA8, Mode = Implicit, Cycles = 2)]
        private void TAY()
        {
            // TAY - Transfer Accumulator to Y
            P.SetZN(Y = A);
        }

        [OpcodeDef(Opcode = 0x98, Mode = Implicit, Cycles = 2)]
        private void TYA()
        {
            // TYA - Transfer Y to Accumulator
            P.SetZN(A = Y);
        }

        [OpcodeDef(Opcode = 0xAA, Mode = Implicit, Cycles = 2, RMW = true)]
        private void TAX()
        {
            // TAX - Transfer Accumulator to X
            P.SetZN(X = A);
        }

        [OpcodeDef(Opcode = 0x8A, Mode = Implicit, Cycles = 2, RMW = true)]
        private void TXA()
        {
            // TXA - Transfer X to Accumulator
            P.SetZN(A = X);
        }

        [OpcodeDef(Opcode = 0xBA, Mode = Implicit, Cycles = 2)]
        private void TSX()
        {
            // TSX - Transfer Stack Pointer to X
            P.SetZN(X = SP);
        }

        [OpcodeDef(Opcode = 0x9A, Mode = Implicit, Cycles = 2, RMW = true)]
        private void TXS()
        {
            // TXS - Transfer X to Stack Pointer
            SP = X;
        }

        [OpcodeDef(Opcode = 0x08, Mode = Implicit, Cycles = 3)]
        private void PHP()
        {
            // PHP - Push Processor Status
            PushByte((byte)(P.ToByte() | 0b00010000));
        }

        [OpcodeDef(Opcode = 0x28, Mode = Implicit, Cycles = 4)]
        private void PLP()
        {
            // PLP - Pull Processor Status
            P.FromByte((byte)(PopByte() & ~0b00010000));
        }

        [OpcodeDef(Opcode = 0x68, Mode = Implicit, Cycles = 4)]
        private void PLA()
        {
            // PLP - Pull Processor Status
            P.SetZN(A = PopByte());
        }

        [OpcodeDef(Opcode = 0x48, Mode = Implicit, Cycles = 3)]
        private void PHA()
        {
            // PHA - Push Accumulator
            PushByte(A);
        }

        [OpcodeDef(Opcode = 0x24, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0x2C, Mode = Absolute, Cycles = 4)]
        private void BIT()
        {
            // BIT - Bit Test
            //
            // A & M, N = M7, V = M6
            int val = AddressRead();
            P.Overflow = (val & 0x40) > 0;
            P.Zero = (val & A) == 0;
            P.Negative = (val & 0x80) > 0;
        }

        private void Branch(bool cond)
        {
            if (cond)
            {
                int newPC = PC + _currentOpAddress;

                // clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
                if ((newPC & 0xFF00) != (PC & 0xFF00))
                    Cycle += 2;
                else
                    Cycle++;
                PC = newPC;
            }
        }

        [OpcodeDef(Opcode = 0x4C, Mode = Absolute, Cycles = 3)]
        [OpcodeDef(Opcode = 0x6C, Mode = Indirect, Cycles = 5)]
        private void JMP()
        {
            PC = _currentOpAddress;
        }

        [OpcodeDef(Opcode = 0xB0, Mode = Relative, Cycles = 2)]
        private void BCS()
        {
            // BCS - Branch if Carry Set
            Branch(P.Carry);
        }

        [OpcodeDef(Opcode = 0x90, Mode = Relative, Cycles = 2)]
        private void BCC()
        {
            // BCC - Branch if Carry Clear
            Branch(!P.Carry);
        }

        [OpcodeDef(Opcode = 0xF0, Mode = Relative, Cycles = 2)]
        private void BEQ()
        {
            // BEQ - Branch if Equal
            Branch(P.Zero);
        }

        [OpcodeDef(Opcode = 0xD0, Mode = Relative, Cycles = 2)]
        private void BNE()
        {
            // BNE - Branch if Not Equal
            Branch(!P.Zero);
        }

        [OpcodeDef(Opcode = 0x70, Mode = Relative, Cycles = 2)]
        private void BVS()
        {
            // BVS - Branch if Overflow Set
            Branch(P.Overflow);
        }

        [OpcodeDef(Opcode = 0x50, Mode = Relative, Cycles = 2)]
        private void BVC()
        {
            // BVC - Branch if Overflow Clear
            Branch(!P.Overflow);
        }

        [OpcodeDef(Opcode = 0x10, Mode = Relative, Cycles = 2)]
        private void BPL()
        {
            // BPL - Branch if Positive
            Branch(!P.Negative);
        }

        [OpcodeDef(Opcode = 0x30, Mode = Relative, Cycles = 2)]
        private void BMI()
        {
            // BMI - Branch if Minus
            Branch(P.Negative);
        }

        [OpcodeDef(Opcode = 0x81, Mode = IndirectX, Cycles = 6)]
        [OpcodeDef(Opcode = 0x91, Mode = IndirectY, Cycles = 6)]
        [OpcodeDef(Opcode = 0x95, Mode = ZeroPageX, Cycles = 4)]
        [OpcodeDef(Opcode = 0x99, Mode = AbsoluteY, Cycles = 5)]
        [OpcodeDef(Opcode = 0x9D, Mode = AbsoluteX, Cycles = 5)]
        [OpcodeDef(Opcode = 0x85, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0x8D, Mode = Absolute, Cycles = 4)]
        private void STA()
        {
            // STA - Store Accumulator
            AddressWrite(A);
        }

        [OpcodeDef(Opcode = 0x96, Mode = ZeroPageY, Cycles = 4)]
        [OpcodeDef(Opcode = 0x86, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0x8E, Mode = Absolute, Cycles = 4)]
        private void STX()
        {
            // Store index X in memory
            AddressWrite(X);
        }

        [OpcodeDef(Opcode = 0x94, Mode = ZeroPageX, Cycles = 4)]
        [OpcodeDef(Opcode = 0x84, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0x8C, Mode = Absolute, Cycles = 4)]
        private void STY()
        {
            // Store index Y in memory
            AddressWrite(Y);
        }

        [OpcodeDef(Opcode = 0x18, Mode = Implicit, Cycles = 2)]
        private void CLC()
        {
            // Clear carry flag
            P.Carry = false;
        }

        [OpcodeDef(Opcode = 0x38, Mode = Implicit, Cycles = 2)]
        private void SEC()
        {
            // Set carry flag
            P.Carry = true;
        }

        [OpcodeDef(Opcode = 0x58, Mode = Implicit, Cycles = 2)]
        private void CLI()
        {
            // Clear interrupt flag
            P.IrqDisable = false;
        }

        [OpcodeDef(Opcode = 0x78, Mode = Implicit, Cycles = 2)]
        private void SEI()
        {
            // Set interrupt disable status
            P.IrqDisable = true;
        }

        [OpcodeDef(Opcode = 0xB8, Mode = Implicit, Cycles = 2)]
        private void CLV()
        {
            // Clear overflow flag
            P.Overflow = false;
        }

        [OpcodeDef(Opcode = 0xD8, Mode = Implicit, Cycles = 2)]
        private void CLD()
        {
            // Clear decimal flag
            P.Decimal = false;
        }

        [OpcodeDef(Opcode = 0xF8, Mode = Implicit, Cycles = 2)]
        private void SED()
        {
            // Set decimal flag
            P.Decimal = true;
        }

        [OpcodeDef(Opcode = 0xEA, Mode = Implicit, Cycles = 2)]
        [OpcodeDef(Opcode = 0x1A, Mode = Implicit, Cycles = 2)] // Unofficial
        [OpcodeDef(Opcode = 0x3A, Mode = Implicit, Cycles = 2)] // Unofficial
        [OpcodeDef(Opcode = 0x5A, Mode = Implicit, Cycles = 2)] // Unofficial
        [OpcodeDef(Opcode = 0x7A, Mode = Implicit, Cycles = 2)] // Unofficial
        [OpcodeDef(Opcode = 0xDA, Mode = Implicit, Cycles = 2)] // Unofficial
        [OpcodeDef(Opcode = 0xFA, Mode = Implicit, Cycles = 2)] // Unofficial
        [OpcodeDef(Opcode = 0x0C, Mode = Absolute, Cycles = 4)] // Unofficial, 3 bytes
        [OpcodeDef(Opcode = 0x1C, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)] // Unofficial, 3 bytes
        [OpcodeDef(Opcode = 0x3C, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)] // Unofficial, 3 bytes
        [OpcodeDef(Opcode = 0x5C, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)] // Unofficial, 3 bytes
        [OpcodeDef(Opcode = 0x7C, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)] // Unofficial, 3 bytes
        [OpcodeDef(Opcode = 0xDC, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)] // Unofficial, 3 bytes
        [OpcodeDef(Opcode = 0xFC, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)] // Unofficial, 3 bytes

        private void NOP() { }

        [OpcodeDef(Opcode = 0xA1, Mode = IndirectX, Cycles = 6)]
        [OpcodeDef(Opcode = 0xA5, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0xA9, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0xAD, Mode = Absolute, Cycles = 4)]
        [OpcodeDef(Opcode = 0xB1, Mode = IndirectY, Cycles = 5, PageBoundary = true)]
        [OpcodeDef(Opcode = 0xB5, Mode = ZeroPageX, Cycles = 4)]
        [OpcodeDef(Opcode = 0xB9, Mode = AbsoluteY, Cycles = 4, PageBoundary = true)]
        [OpcodeDef(Opcode = 0xBD, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)]
        private void LDA()
        {
            // Load accumulator with memory
            P.SetZN(A = AddressRead());
        }

        [OpcodeDef(Opcode = 0xA0, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0xA4, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0xAC, Mode = Absolute, Cycles = 4)]
        [OpcodeDef(Opcode = 0xB4, Mode = ZeroPageX, Cycles = 4)]
        [OpcodeDef(Opcode = 0xBC, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)]
        private void LDY()
        {
            // Load index Y with memory
            P.SetZN(Y = AddressRead());
        }

        [OpcodeDef(Opcode = 0xA2, Mode = Immediate, Cycles = 2, RMW = true)]
        [OpcodeDef(Opcode = 0xA6, Mode = ZeroPage, Cycles = 3, RMW = true)]
        [OpcodeDef(Opcode = 0xAE, Mode = Absolute, Cycles = 4, RMW = true)]
        [OpcodeDef(Opcode = 0xB6, Mode = ZeroPageY, Cycles = 4, RMW = true)]
        [OpcodeDef(Opcode = 0xBE, Mode = AbsoluteY, Cycles = 4, PageBoundary = true, RMW = true)]
        private void LDX()
        {
            // Load index X with memory
            P.SetZN(X = AddressRead());
        }

        [OpcodeDef(Opcode = 0x01, Mode = IndirectX, Cycles = 6)]
        [OpcodeDef(Opcode = 0x05, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0x09, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0x0D, Mode = Absolute, Cycles = 4)]
        [OpcodeDef(Opcode = 0x11, Mode = IndirectY, Cycles = 5, PageBoundary = true)]
        [OpcodeDef(Opcode = 0x15, Mode = ZeroPageX, Cycles = 4)]
        [OpcodeDef(Opcode = 0x19, Mode = AbsoluteY, Cycles = 4, PageBoundary = true)]
        [OpcodeDef(Opcode = 0x1D, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)]
        private void ORA()
        {
            // OR memory with accumulator, store in accumulator.
            P.SetZN(A |= AddressRead());
        }

        [OpcodeDef(Opcode = 0x21, Mode = IndirectX, Cycles = 6)]
        [OpcodeDef(Opcode = 0x25, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0x29, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0x2D, Mode = Absolute, Cycles = 4)]
        [OpcodeDef(Opcode = 0x31, Mode = IndirectY, Cycles = 5, PageBoundary = true)]
        [OpcodeDef(Opcode = 0x35, Mode = ZeroPageX, Cycles = 4)]
        [OpcodeDef(Opcode = 0x39, Mode = AbsoluteY, Cycles = 4, PageBoundary = true)]
        [OpcodeDef(Opcode = 0x3D, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)]
        private void AND()
        {
            // AND memory with accumulator, store in accumulator.
            P.SetZN(A &= AddressRead());
        }

        [OpcodeDef(Opcode = 0x41, Mode = IndirectX, Cycles = 6)]
        [OpcodeDef(Opcode = 0x45, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0x49, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0x4D, Mode = Absolute, Cycles = 4)]
        [OpcodeDef(Opcode = 0x51, Mode = IndirectY, Cycles = 5, PageBoundary = true)]
        [OpcodeDef(Opcode = 0x55, Mode = ZeroPageX, Cycles = 4)]
        [OpcodeDef(Opcode = 0x59, Mode = AbsoluteY, Cycles = 4, PageBoundary = true)]
        [OpcodeDef(Opcode = 0x5D, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)]
        private void EOR()
        {
            // XOR memory with accumulator, store in accumulator.
            P.SetZN(A ^= AddressRead());
        }

        [OpcodeDef(Opcode = 0xE1, Mode = IndirectX, Cycles = 6)]
        [OpcodeDef(Opcode = 0xE5, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0xE9, Mode = Immediate, Cycles = 2)] // Official duplicate of $E9
        [OpcodeDef(Opcode = 0xEB, Mode = Immediate, Cycles = 2)] // Unofficial duplicate of $E9
        [OpcodeDef(Opcode = 0xED, Mode = Absolute, Cycles = 4)]
        [OpcodeDef(Opcode = 0xF1, Mode = IndirectY, Cycles = 5, PageBoundary = true)]
        [OpcodeDef(Opcode = 0xF5, Mode = ZeroPageX, Cycles = 4)]
        [OpcodeDef(Opcode = 0xF9, Mode = AbsoluteY, Cycles = 4, PageBoundary = true)]
        [OpcodeDef(Opcode = 0xFD, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)]
        private void SBC()
        {
            // SBC - Subtract with Carry
            //
            // A,Z,C,N = A-M-(1-C)
            ADCImpl((byte) ~AddressRead());
        }

        [OpcodeDef(Opcode = 0x61, Mode = IndirectX, Cycles = 6)]
        [OpcodeDef(Opcode = 0x65, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0x69, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0x6D, Mode = Absolute, Cycles = 4)]
        [OpcodeDef(Opcode = 0x71, Mode = IndirectY, Cycles = 5, PageBoundary = true)]
        [OpcodeDef(Opcode = 0x75, Mode = ZeroPageX, Cycles = 4)]
        [OpcodeDef(Opcode = 0x79, Mode = AbsoluteY, Cycles = 4, PageBoundary = true)]
        [OpcodeDef(Opcode = 0x7D, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)]
        private void ADC()
        {
            // ADC - Add with Carry
            // A,Z,C,N = A+M+C
            ADCImpl(AddressRead());
        }

        private void ADCImpl(byte val)
        {
            unchecked
            {
                int nA = (sbyte)A + (sbyte)val + (sbyte)(P.Carry ? 1 : 0);
                P.Overflow = nA < -128 || nA > 127;
                P.Carry = (A + val + (P.Carry ? 1 : 0)) > 0xFF;
                P.SetZN(A = (byte)(nA & 0xFF));
            }
        }

        [OpcodeDef(Opcode = 0x00, Mode = Implicit, Cycles = 7)]
        private void BRK()
        {
            PC++;
            PushWord(PC);
            PushPWithBreak();
            P.IrqDisable = true;
            PC = _memory.ReadWord((int)Interrupt.Irq);
        }

        [OpcodeDef(Opcode = 0xC1, Mode = IndirectX, Cycles = 6)]
        [OpcodeDef(Opcode = 0xC5, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0xC9, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0xCD, Mode = Absolute, Cycles = 4)]
        [OpcodeDef(Opcode = 0xD1, Mode = IndirectY, Cycles = 5, PageBoundary = true)]
        [OpcodeDef(Opcode = 0xD5, Mode = ZeroPageX, Cycles = 4)]
        [OpcodeDef(Opcode = 0xD9, Mode = AbsoluteY, Cycles = 4, PageBoundary = true)]
        [OpcodeDef(Opcode = 0xDD, Mode = AbsoluteX, Cycles = 4, PageBoundary = true)]
        private void CMP()
        {
            // CMP - Compare
            //
            // Z,C,N = A-M
            CMPImpl(A);
        }

        [OpcodeDef(Opcode = 0xE0, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0xE4, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0xEC, Mode = Absolute, Cycles = 4)]
        private void CPX()
        {
            // CPX - Compare
            //
            // Z,C,N = X-M
            CMPImpl(X);
        }

        [OpcodeDef(Opcode = 0xC0, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0xC4, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0xCC, Mode = Absolute, Cycles = 4)]
        private void CPY()
        {
            // CPY - Compare
            //
            // Z,C,N = Y-M
            CMPImpl(Y);
        }

        private void CMPImpl(byte reg)
        {
            int d = reg - AddressRead();

            P.Negative = (d & 0x80) > 0;
            P.Carry = d >= 0;
            P.Zero = d == 0;
        }

        [OpcodeDef(Opcode = 0x46, Mode = ZeroPage, Cycles = 5, RMW = true)]
        [OpcodeDef(Opcode = 0x4E, Mode = Absolute, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x56, Mode = ZeroPageX, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x5E, Mode = AbsoluteX, Cycles = 7, RMW = true)]
        [OpcodeDef(Opcode = 0x4A, Mode = Accumulator, Cycles = 2, RMW = true)]
        private void LSR()
        {
            // LSR - Shift right one bit
            byte d = AddressRead();
            P.Carry = (d & 0x1) > 0;
            d >>= 1;
            AddressWrite(P.SetZN(d));
        }

        [OpcodeDef(Opcode = 0x06, Mode = ZeroPage, Cycles = 5, RMW = true)]
        [OpcodeDef(Opcode = 0x0E, Mode = Absolute, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x16, Mode = ZeroPageX, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x1E, Mode = AbsoluteX, Cycles = 7, RMW = true)]
        [OpcodeDef(Opcode = 0x0A, Mode = Accumulator, Cycles = 2, RMW = true)]
        private void ASL()
        {
            // ASL - Arithmetic Shift Left
            byte d = AddressRead();
            P.Carry = (d & 0x80) > 0;
            d <<= 1;
            AddressWrite(P.SetZN(d));
        }

        [OpcodeDef(Opcode = 0x66, Mode = ZeroPage, Cycles = 5, RMW = true)]
        [OpcodeDef(Opcode = 0x6E, Mode = Absolute, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x76, Mode = ZeroPageX, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x7E, Mode = AbsoluteX, Cycles = 7, RMW = true)]
        [OpcodeDef(Opcode = 0x6A, Mode = Accumulator, Cycles = 2, RMW = true)]
        private void ROR()
        {
            // ROR - Rotate Right
            byte d = AddressRead();
            bool c = P.Carry;
            P.Carry = (d & 0x1) > 0;
            d >>= 1;
            if (c) d |= 0x80;
            AddressWrite(P.SetZN(d));
        }

        [OpcodeDef(Opcode = 0x26, Mode = ZeroPage, Cycles = 5, RMW = true)]
        [OpcodeDef(Opcode = 0x2E, Mode = Absolute, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x36, Mode = ZeroPageX, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x3E, Mode = AbsoluteX, Cycles = 7, RMW = true)]
        [OpcodeDef(Opcode = 0x2A, Mode = Accumulator, Cycles = 2, RMW = true)]
        private void ROL()
        {
            // ROL - Rotate Left
            byte d = AddressRead();
            bool c = P.Carry;
            P.Carry = (d & 0x80) > 0;
            d <<= 1;
            if (c) d |= 0x1;
            AddressWrite(P.SetZN(d));
        }

        [OpcodeDef(Opcode = 0xE6, Mode = ZeroPage, Cycles = 5, RMW = true)]
        [OpcodeDef(Opcode = 0xEE, Mode = Absolute, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0xF6, Mode = ZeroPageX, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0xFE, Mode = AbsoluteX, Cycles = 7, RMW = true)]
        private void INC()
        {
            // INC - Increment Memory

            byte d = AddressRead();
            unchecked
            {
                d++;
            }
            AddressWrite(P.SetZN(d));
        }

        [OpcodeDef(Opcode = 0xC6, Mode = ZeroPage, Cycles = 5, RMW = true)]
        [OpcodeDef(Opcode = 0xCE, Mode = Absolute, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0xD6, Mode = ZeroPageX, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0xDE, Mode = AbsoluteX, Cycles = 7, RMW = true)]
        private void DEC()
        {
            // DEC - Decrement Memory
            byte d = AddressRead();
            unchecked
            {
                d--;
            }
            AddressWrite(P.SetZN(d));
        }

        #region Unofficial Opcodes
        [OpcodeDef(Opcode = 0x04, Mode = Immediate, Cycles = 3)]
        [OpcodeDef(Opcode = 0x44, Mode = Immediate, Cycles = 3)]
        [OpcodeDef(Opcode = 0x64, Mode = Immediate, Cycles = 3)]
        [OpcodeDef(Opcode = 0x14, Mode = Immediate, Cycles = 4)]
        [OpcodeDef(Opcode = 0x34, Mode = Immediate, Cycles = 4)]
        [OpcodeDef(Opcode = 0x54, Mode = Immediate, Cycles = 4)]
        [OpcodeDef(Opcode = 0x74, Mode = Immediate, Cycles = 4)]
        [OpcodeDef(Opcode = 0xD4, Mode = Immediate, Cycles = 4)]
        [OpcodeDef(Opcode = 0xF4, Mode = Immediate, Cycles = 4)]
        [OpcodeDef(Opcode = 0x80, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0x82, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0x89, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0xC2, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0xE2, Mode = Immediate, Cycles = 2)]
        private void SKB()
        {
            // Essentially a 2-byte NOP
        }
        
        [OpcodeDef(Opcode = 0x0B, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0x2B, Mode = Immediate, Cycles = 2)]
        private void ANC()
        {
            A &= AddressRead();
            P.Carry = P.Negative;
        }

        [OpcodeDef(Opcode = 0x4B, Mode = Immediate, Cycles = 2)]
        private void ALR()
        {
            A &= AddressRead();
            P.Carry = (A & 0x1) > 0;
            P.SetZN(A >>= 1);
        }

        [OpcodeDef(Opcode = 0x6B, Mode = Immediate, Cycles = 2)]
        private void ARR()
        {
            A &= AddressRead();
            bool c = P.Carry;
            P.Carry = (A & 0x1) > 0;
            A >>= 1;
            if (c) A |= 0x80;
            P.SetZN(A);
        }

        [OpcodeDef(Opcode = 0xAB, Mode = Immediate, Cycles = 2)]
        private void ATX()
        {
            // This opcode ORs the A register with #$EE, ANDs the result with an immediate 
            // value, and then stores the result in both A and X.
            A |= _memory.ReadByte(0xEE);
            A &= AddressRead();
            P.SetZN(X = A);
        }

        [OpcodeDef(Opcode = 0x03, Mode = IndirectX, Cycles = 8, RMW = true)]
        [OpcodeDef(Opcode = 0x07, Mode = ZeroPage, Cycles = 5, RMW = true)]
        [OpcodeDef(Opcode = 0x0F, Mode = Absolute, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x13, Mode = IndirectY, Cycles = 8, RMW = true)]
        [OpcodeDef(Opcode = 0x17, Mode = ZeroPageX, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x1B, Mode = AbsoluteY, Cycles = 7, RMW = true)]
        [OpcodeDef(Opcode = 0x1F, Mode = AbsoluteX, Cycles = 7, RMW = true)]
        private void SLO()
        {
            //  SLO - Undocumented Opcode
            // 
            //  Equivalent to ASL value then ORA value, except supporting more addressing modes.
            //  LDA #0 followed by SLO is an efficient way to shift a variable while also loading it in A.
            
            ASL();
            ORA();
        }

        [OpcodeDef(Opcode = 0x23, Mode = IndirectX, Cycles = 8, RMW = true)]
        [OpcodeDef(Opcode = 0x27, Mode = ZeroPage, Cycles = 5, RMW = true)]
        [OpcodeDef(Opcode = 0x2F, Mode = Absolute, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x33, Mode = IndirectY, Cycles = 8, RMW = true)]
        [OpcodeDef(Opcode = 0x37, Mode = ZeroPageX, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x3B, Mode = AbsoluteY, Cycles = 7, RMW = true)]
        [OpcodeDef(Opcode = 0x3F, Mode = AbsoluteX, Cycles = 7, RMW = true)]
        private void RLA()
        {
            //     RLA - Undocumented Opcode
            //
            //     Equivalent to ROL value then AND value, except supporting more addressing modes.
            //     LDA #$FF followed by RLA is an efficient way to rotate a variable while also loading it in A.
            ROL();
            AND();
        }

        [OpcodeDef(Opcode = 0x63, Mode = IndirectX, Cycles = 8, RMW = true)]
        [OpcodeDef(Opcode = 0x67, Mode = ZeroPage, Cycles = 5, RMW = true)]
        [OpcodeDef(Opcode = 0x6F, Mode = Absolute, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x73, Mode = IndirectY, Cycles = 8, RMW = true)]
        [OpcodeDef(Opcode = 0x77, Mode = ZeroPageX, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x7B, Mode = AbsoluteY, Cycles = 7, RMW = true)]
        [OpcodeDef(Opcode = 0x7F, Mode = AbsoluteX, Cycles = 7, RMW = true)]
        private void RRA()
        {
            //     RRA - Undocumented Opcode
            //
            //     Equivalent to ROR value then ADC value, except supporting more addressing modes. Essentially
            //     this computes A + value / 2, where value is 9-bit and the division is rounded up.

            ROR();
            ADC();
        }

        [OpcodeDef(Opcode = 0x43, Mode = IndirectX, Cycles = 8, RMW = true)]
        [OpcodeDef(Opcode = 0x47, Mode = ZeroPage, Cycles = 5, RMW = true)]
        [OpcodeDef(Opcode = 0x4F, Mode = Absolute, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x53, Mode = IndirectY, Cycles = 8, RMW = true)]
        [OpcodeDef(Opcode = 0x57, Mode = ZeroPageX, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0x5B, Mode = AbsoluteY, Cycles = 7, RMW = true)]
        [OpcodeDef(Opcode = 0x5F, Mode = AbsoluteX, Cycles = 7, RMW = true)]
        private void SRE()
        {
            //     SRE - Undocumented Opcode
            //
            //     Equivalent to LSR value then EOR value, except supporting more addressing modes. LDA #0 followed
            //     by SRE is an efficient way to shift a variable while also loading it in A.
            LSR();
            EOR();
        }

        [OpcodeDef(Opcode = 0xE3, Mode = IndirectX, Cycles = 8)]
        [OpcodeDef(Opcode = 0xE7, Mode = ZeroPage, Cycles = 5)]
        [OpcodeDef(Opcode = 0xEF, Mode = Absolute, Cycles = 6)]
        [OpcodeDef(Opcode = 0xF3, Mode = IndirectY, Cycles = 8)]
        [OpcodeDef(Opcode = 0xF7, Mode = ZeroPageX, Cycles = 6)]
        [OpcodeDef(Opcode = 0xFB, Mode = AbsoluteY, Cycles = 7)]
        [OpcodeDef(Opcode = 0xFF, Mode = AbsoluteX, Cycles = 7)]
        private void ISC()
        {
            //     ISC - Undocumented Opcode
            //
            //     Equivalent to INC value then SBC value, except supporting more addressing modes.
            INC();
            SBC();
        }

        [OpcodeDef(Opcode = 0xC3, Mode = IndirectX, Cycles = 8)]
        [OpcodeDef(Opcode = 0xC7, Mode = ZeroPage, Cycles = 5)]
        [OpcodeDef(Opcode = 0xCF, Mode = Absolute, Cycles = 6)]
        [OpcodeDef(Opcode = 0xD3, Mode = IndirectY, Cycles = 8)]
        [OpcodeDef(Opcode = 0xD7, Mode = ZeroPageX, Cycles = 6)]
        [OpcodeDef(Opcode = 0xDB, Mode = AbsoluteY, Cycles = 7)]
        [OpcodeDef(Opcode = 0xDF, Mode = AbsoluteX, Cycles = 7)]
        private void DCP()
        {
            //    DCP - Undocumented OpCode
            //
            //     The read-modify-write instructions (INC, DEC, ASL, LSR, ROL, ROR) have few valid addressing modes,
            //     but these instructions have three more: (d,X),
            //     (d),Y, and a,Y. In some cases, it could be worth it to use these and ignore the side effect on the accumulator.
            DEC();
            CMP();
        }

        //[Instruction(Opcode = 0xAB, Mode = Immediate, Cycles = 2)] // Unstable
        [OpcodeDef(Opcode = 0xA3, Mode = IndirectX, Cycles = 6)]
        [OpcodeDef(Opcode = 0xA7, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0xAF, Mode = Absolute, Cycles = 4)]
        [OpcodeDef(Opcode = 0xB3, Mode = IndirectY, Cycles = 5, PageBoundary = true)]
        [OpcodeDef(Opcode = 0xB7, Mode = ZeroPageY, Cycles = 4)]
        [OpcodeDef(Opcode = 0xBF, Mode = AbsoluteY, Cycles = 4, PageBoundary = true)]
        private void LAX()
        {
            //     LAX - Undocumented Opcode
            //
            //     Shortcut for LDA value then TAX. Saves a byte and two cycles and allows use of the X register
            //     with the (d),Y addressing mode. Notice that the immediate is missing; the opcode that would
            //     have been LAX is affected by line noise on the data bus. MOS 6502: even the bugs have bugs.
            LDA();
            TAX();
        }

        
        [OpcodeDef(Opcode = 0x83, Mode = IndirectX, Cycles = 6)]
        [OpcodeDef(Opcode = 0x87, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0x8F, Mode = Absolute, Cycles = 4)]
        [OpcodeDef(Opcode = 0x97, Mode = ZeroPageY, Cycles = 4)]
        private void SAX()
        {
            //     SAX - Undocumented Opcode
            //
            //     Stores the bitwise AND of A and X. As with STA and STX, no flags are affected.

            AddressWrite((byte)(A & X));
        }

        [OpcodeDef(Opcode = 0x8B, Mode = Immediate, Cycles = 2)] // Unstable
        private void XAA()
        {
            // A = X & M
            byte d = AddressRead();
            P.SetZN(A = (byte) (X & d));
        }

        [OpcodeDef(Opcode = 0xCB, Mode = Immediate, Cycles = 2)]
        private void AXS()
        {
            // X = A & X - d
            byte d = AddressRead();
            int r = (A & X) - d;
            P.SetZN(X = (byte) (r & 0xFF));
            P.Carry = P.Negative;
        }
        
        [OpcodeDef(Opcode = 0x93, Mode = IndirectX, Cycles = 6)]
        [OpcodeDef(Opcode = 0x9F, Mode = AbsoluteY, Cycles = 5)]
        private void AHX()
        {
            // M = A & X & H
            byte H = (byte) (_currentOpAddress >> 8);
            byte d = (byte) (A & X & H);
            AddressWrite(d);
        }

        [OpcodeDef(Opcode = 0x9E, Mode = AbsoluteY, Cycles = 5)]
        private void SHX()
        {
            // M = X & H
            byte H = (byte) (_currentOpAddress >> 8);
            byte d = (byte) (X & H);
            AddressWrite(d);
        }

        [OpcodeDef(Opcode = 0x9C, Mode = Absolute, Cycles = 5)]
        private void SHY()
        {
            // M = X & H
            byte H = (byte) (_currentOpAddress >> 8);
            byte d = (byte) (Y & H);
            AddressWrite(d);
        }

        [OpcodeDef(Opcode = 0x9B, Mode = AbsoluteY, Cycles = 5)]
        private void TAS()
        {
            // S = A & X, M = S & H
            SP = (byte) (A & X);
            byte H = (byte) (_currentOpAddress >> 8);
            byte d = (byte) (SP & H);
            AddressWrite(d);
        }

        [OpcodeDef(Opcode = 0xBB, Mode = AbsoluteY, Cycles = 4)]
        private void LAS()
        {
            // A, X, S = M & S
            byte d = AddressRead();
            SP = (byte) (SP & d);
            A = SP;
            X = SP;
        }

        [OpcodeDef(Opcode = 0x02, Mode = Implicit)]
        [OpcodeDef(Opcode = 0x22, Mode = Implicit)]
        [OpcodeDef(Opcode = 0x42, Mode = Implicit)]
        [OpcodeDef(Opcode = 0x62, Mode = Implicit)]
        [OpcodeDef(Opcode = 0x12, Mode = Implicit)]
        [OpcodeDef(Opcode = 0x32, Mode = Implicit)]
        [OpcodeDef(Opcode = 0x52, Mode = Implicit)]
        [OpcodeDef(Opcode = 0x72, Mode = Implicit)]
        [OpcodeDef(Opcode = 0x92, Mode = Implicit)]
        [OpcodeDef(Opcode = 0xB2, Mode = Implicit)]
        [OpcodeDef(Opcode = 0xD2, Mode = Implicit)]
        [OpcodeDef(Opcode = 0xF2, Mode = Implicit)]
        private void KIL()
        {
            // Halt
            _halted = true;
        }
        #endregion
    }
}