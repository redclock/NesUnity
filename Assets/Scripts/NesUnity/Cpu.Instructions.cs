using System;
using static NesUnity.AddressingMode;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

namespace NesUnity
{
    public partial class Cpu
    {
        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
        public class OpcodeDef : Attribute
        {
            public int Opcode;
            public int Cycles = 1;
            public bool PageBoundary;
            public bool RMW;
            public AddressingMode Mode = None;
        }

        [OpcodeDef(Opcode = 0x20, Cycles = 6)]
        private void JSR()
        {
            // JSR - Jump to Subroutine
            // Push address of next instruction - 1,
            // thus PC + 1 instead of PC + 2
            // since PC and PC + 1 are address of subroutine
            PushWord(PC + 1);
            PC = NextWord();
        }

        [OpcodeDef(Opcode = 0x40, Cycles = 6)]
        private void RTI()
        {
            // RTI - Return from Interrupt
            // Return from interrupt. Pull status and PC from stack.
            PopP();
            PC = PopWord();
        }

        [OpcodeDef(Opcode = 0x60, Cycles = 6)]
        private void RTS()
        {
            // RTS - Return from Subroutine
            PC = PopWord() + 1;
        }

        [OpcodeDef(Opcode = 0xC8, Cycles = 2)]
        private void INY()
        {
            // INY - Increment Y Register
            unchecked
            {
                P.SetZN(++Y);
            }
        }

        [OpcodeDef(Opcode = 0x88, Cycles = 2)]
        private void DEY()
        {
            // DEY - Decrement Y Register
            unchecked
            {
                P.SetZN(--Y);    
            }
        }

        [OpcodeDef(Opcode = 0xE8, Cycles = 2)]
        private void INX()
        {
            // INX - Increment X Register
            unchecked
            {
                P.SetZN(++X);    
            }
        }

        [OpcodeDef(Opcode = 0xCA, Cycles = 2, RMW = true)]
        private void DEX()
        {
            // DEX - Decrement X Register
            unchecked
            {
                P.SetZN(--X);    
            }
        }

        [OpcodeDef(Opcode = 0xA8, Cycles = 2)]
        private void TAY()
        {
            // TAY - Transfer Accumulator to Y
            P.SetZN(Y = A);
        }

        [OpcodeDef(Opcode = 0x98, Cycles = 2)]
        private void TYA()
        {
            // TYA - Transfer Y to Accumulator
            P.SetZN(A = Y);
        }

        [OpcodeDef(Opcode = 0xAA, Cycles = 2, RMW = true)]
        private void TAX()
        {
            // TAX - Transfer Accumulator to X
            P.SetZN(X = A);
        }

        [OpcodeDef(Opcode = 0x8A, Cycles = 2, RMW = true)]
        private void TXA()
        {
            // TXA - Transfer X to Accumulator
            P.SetZN(A = X);
        }

        [OpcodeDef(Opcode = 0xBA, Cycles = 2)]
        private void TSX()
        {
            // TSX - Transfer Stack Pointer to X
            P.SetZN(X = SP);
        }

        [OpcodeDef(Opcode = 0x9A, Cycles = 2, RMW = true)]
        private void TXS()
        {
            // TXS - Transfer X to Stack Pointer
            P.SetZN(SP = X);
        }

        [OpcodeDef(Opcode = 0x08, Cycles = 3)]
        private void PHP()
        {
            // PHP - Push Processor Status
            PushByte((byte)(P.ToByte() | 0b00010000));
        }

        [OpcodeDef(Opcode = 0x28, Cycles = 4)]
        private void PLP()
        {
            // PLP - Pull Processor Status
            P.FromByte((byte)(PopByte() & ~0b00010000));
        }

        [OpcodeDef(Opcode = 0x68, Cycles = 4)]
        private void PLA()
        {
            // PLP - Pull Processor Status
            P.SetZN(A = PopByte());
        }

        [OpcodeDef(Opcode = 0x48, Cycles = 3)]
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
            sbyte operand = NextSByte();
            if (cond)
            {
                int newPC = PC + operand;

                if ((newPC & 0xFF00) != (PC & 0xFF00))
                    Cycle += 2;
                else
                    Cycle++;
                PC = newPC;
            }
        }

        [OpcodeDef(Opcode = 0x4C, Cycles = 3)]
        [OpcodeDef(Opcode = 0x6C, Cycles = 5)]
        private void JMP()
        {
            if (_currentInstruction.Opcode == 0x4C)
                PC = NextWord();
            else if (_currentInstruction.Opcode == 0x6C)
            {
                int off = NextWord();
                // AN INDIRECT JUMP MUST NEVER USE A VECTOR BEGINNING ON THE LAST BYTE OF A PAGE
                //
                // If address $3000 contains $40, $30FF contains $80, and $3100 contains $50, 
                // the result of JMP ($30FF) will be a transfer of control to $4080 rather than
                // $5080 as you intended i.e. the 6502 took the low byte of the address from
                // $30FF and the high byte from $3000.
                //
                // http://www.6502.org/tutorials/6502opcodes.html
                int hi = (off & 0xFF) | ((off + 1) & 0xFF);
                //int oldPC = PC;
                PC = _memory.ReadByte(off) | (_memory.ReadByte(hi) << 8);

                //if ((oldPC & 0xFF00) != (PC & 0xFF00)) Cycle += 2;
            }
        }

        [OpcodeDef(Opcode = 0xB0, Cycles = 2, Mode = Relative)]
        private void BCS()
        {
            // BCS - Branch if Carry Set
            Branch(P.Carry);
        }

        [OpcodeDef(Opcode = 0x90, Cycles = 2, Mode = Relative)]
        private void BCC()
        {
            // BCC - Branch if Carry Clear
            Branch(!P.Carry);
        }

        [OpcodeDef(Opcode = 0xF0, Cycles = 2, Mode = Relative)]
        private void BEQ()
        {
            // BEQ - Branch if Equal
            Branch(P.Zero);
        }

        [OpcodeDef(Opcode = 0xD0, Cycles = 2, Mode = Relative)]
        private void BNE()
        {
            // BNE - Branch if Not Equal
            Branch(!P.Zero);
        }

        [OpcodeDef(Opcode = 0x70, Cycles = 2, Mode = Relative)]
        private void BVS()
        {
            // BVS - Branch if Overflow Set
            Branch(P.Overflow);
        }

        [OpcodeDef(Opcode = 0x50, Cycles = 2, Mode = Relative)]
        private void BVC()
        {
            // BVC - Branch if Overflow Clear
            Branch(!P.Overflow);
        }

        [OpcodeDef(Opcode = 0x10, Cycles = 2, Mode = Relative)]
        private void BPL()
        {
            // BPL - Branch if Positive
            Branch(!P.Negative);
        }

        [OpcodeDef(Opcode = 0x30, Cycles = 2, Mode = Relative)]
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

        [OpcodeDef(Opcode = 0x18, Cycles = 2)]
        private void CLC()
        {
            // Clear carry flag
            P.Carry = false;
        }

        [OpcodeDef(Opcode = 0x38, Cycles = 2)]
        private void SEC()
        {
            // Set carry flag
            P.Carry = true;
        }

        [OpcodeDef(Opcode = 0x58, Cycles = 2)]
        private void CLI()
        {
            // Clear interrupt flag
            P.IrqDisable = false;
        }

        [OpcodeDef(Opcode = 0x78, Cycles = 2)]
        private void SEI()
        {
            // Set interrupt disable status
            P.IrqDisable = true;
        }

        [OpcodeDef(Opcode = 0xB8, Cycles = 2)]
        private void CLV()
        {
            // Clear overflow flag
            P.Overflow = false;
        }

        [OpcodeDef(Opcode = 0xD8, Cycles = 2)]
        private void CLD()
        {
            // Clear decimal flag
            P.Decimal = false;
        }

        [OpcodeDef(Opcode = 0xF8, Cycles = 2)]
        private void SED()
        {
            // Set decimal flag
            P.Decimal = true;
        }

        [OpcodeDef(Opcode = 0xEA, Cycles = 2)]
        [OpcodeDef(Opcode = 0x1A, Cycles = 2)] // Unofficial
        [OpcodeDef(Opcode = 0x3A, Cycles = 2)] // Unofficial
        [OpcodeDef(Opcode = 0x5A, Cycles = 2)] // Unofficial
        [OpcodeDef(Opcode = 0x7A, Cycles = 2)] // Unofficial
        [OpcodeDef(Opcode = 0xDA, Cycles = 2)] // Unofficial
        [OpcodeDef(Opcode = 0xFA, Cycles = 2)] // Unofficial
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
            A ^= AddressRead();
        }

        [OpcodeDef(Opcode = 0xE1, Mode = IndirectX, Cycles = 6)]
        [OpcodeDef(Opcode = 0xE5, Mode = ZeroPage, Cycles = 3)]
        [OpcodeDef(Opcode = 0x69, Mode = Immediate, Cycles = 2)]
        [OpcodeDef(Opcode = 0xE9, Mode = Immediate, Cycles = 2)] // Official duplicate of $69
        [OpcodeDef(Opcode = 0xEB, Mode = Immediate, Cycles = 2)] // Unofficial duplicate of $69
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

        [OpcodeDef(Opcode = 0x00, Cycles = 7)]
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
        [OpcodeDef(Opcode = 0x4A, Mode = Direct, Cycles = 2, RMW = true)]
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
        [OpcodeDef(Opcode = 0x0A, Mode = Direct, Cycles = 2, RMW = true)]
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
        [OpcodeDef(Opcode = 0x6A, Mode = Direct, Cycles = 2, RMW = true)]
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
        [OpcodeDef(Opcode = 0x2A, Mode = Direct, Cycles = 2, RMW = true)]
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
        [OpcodeDef(Opcode = 0xCE, Mode = Absolute, Cycles = 3, RMW = true)]
        [OpcodeDef(Opcode = 0xD6, Mode = ZeroPageX, Cycles = 6, RMW = true)]
        [OpcodeDef(Opcode = 0xDE, Mode = AbsoluteX, Cycles = 7, RMW = true)]
        private void DEC()
        {
            // INC - Decrement Memory

            byte d = AddressRead();
            unchecked
            {
                d--;
            }
            AddressWrite(P.SetZN(d));
        }

        #region Unofficial Opcodes

        [OpcodeDef(Opcode = 0x80, Cycles = 2)]
        [OpcodeDef(Opcode = 0x82, Cycles = 2)]
        [OpcodeDef(Opcode = 0x89, Cycles = 2)]
        [OpcodeDef(Opcode = 0xC2, Cycles = 2)]
        [OpcodeDef(Opcode = 0xE2, Cycles = 2)]
        private void SKB() => NextByte(); // Essentially a 2-byte NOP

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

        #endregion
    }
}