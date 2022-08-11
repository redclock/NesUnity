// ReSharper disable InconsistentNaming

namespace NesUnity
{
    public partial class Cpu
    {
        private void JSR()
        {
            // JSR - Jump to Subroutine
            // Push address of next instruction - 1,
            PushWord(PC - 1);
            PC = _currentOpAddress;
        }

        private void RTI()
        {
            // RTI - Return from Interrupt
            // Return from interrupt. Pull status and PC from stack.
            PopP();
            PC = PopWord();
        }

        private void RTS()
        {
            // RTS - Return from Subroutine
            PC = PopWord() + 1;
        }

        private void INY()
        {
            // INY - Increment Y Register
            unchecked
            {
                P.SetZN(++Y);
            }
        }

        private void DEY()
        {
            // DEY - Decrement Y Register
            unchecked
            {
                P.SetZN(--Y);    
            }
        }

        private void INX()
        {
            // INX - Increment X Register
            unchecked
            {
                P.SetZN(++X);    
            }
        }

        private void DEX()
        {
            // DEX - Decrement X Register
            unchecked
            {
                P.SetZN(--X);    
            }
        }

        private void TAY()
        {
            // TAY - Transfer Accumulator to Y
            P.SetZN(Y = A);
        }

        private void TYA()
        {
            // TYA - Transfer Y to Accumulator
            P.SetZN(A = Y);
        }

        private void TAX()
        {
            // TAX - Transfer Accumulator to X
            P.SetZN(X = A);
        }

        private void TXA()
        {
            // TXA - Transfer X to Accumulator
            P.SetZN(A = X);
        }

        private void TSX()
        {
            // TSX - Transfer Stack Pointer to X
            P.SetZN(X = SP);
        }

        private void TXS()
        {
            // TXS - Transfer X to Stack Pointer
            SP = X;
        }

        private void PHP()
        {
            // PHP - Push Processor Status
            PushByte((byte)(P.ToByte() | 0b00010000));
        }

        private void PLP()
        {
            // PLP - Pull Processor Status
            P.FromByte((byte)(PopByte() & ~0b00010000));
        }

        private void PLA()
        {
            // PLP - Pull Processor Status
            P.SetZN(A = PopByte());
        }

        private void PHA()
        {
            // PHA - Push Accumulator
            PushByte(A);
        }

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

        private void JMP()
        {
            PC = _currentOpAddress;
        }

        private void BCS()
        {
            // BCS - Branch if Carry Set
            BranchImpl(P.Carry);
        }

        private void BCC()
        {
            // BCC - Branch if Carry Clear
            BranchImpl(!P.Carry);
        }

        private void BEQ()
        {
            // BEQ - Branch if Equal
            BranchImpl(P.Zero);
        }

        private void BNE()
        {
            // BNE - Branch if Not Equal
            BranchImpl(!P.Zero);
        }

        private void BVS()
        {
            // BVS - Branch if Overflow Set
            BranchImpl(P.Overflow);
        }

        private void BVC()
        {
            // BVC - Branch if Overflow Clear
            BranchImpl(!P.Overflow);
        }

        private void BPL()
        {
            // BPL - Branch if Positive
            BranchImpl(!P.Negative);
        }

        private void BMI()
        {
            // BMI - Branch if Minus
            BranchImpl(P.Negative);
        }
        
        private void BranchImpl(bool cond)
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


        private void STA()
        {
            // STA - Store Accumulator
            AddressWrite(A);
        }

        private void STX()
        {
            // Store index X in memory
            AddressWrite(X);
        }

        private void STY()
        {
            // Store index Y in memory
            AddressWrite(Y);
        }

        private void CLC()
        {
            // Clear carry flag
            P.Carry = false;
        }

        private void SEC()
        {
            // Set carry flag
            P.Carry = true;
        }

        private void CLI()
        {
            // Clear interrupt flag
            P.IrqDisable = false;
        }

        private void SEI()
        {
            // Set interrupt disable status
            P.IrqDisable = true;
        }

        private void CLV()
        {
            // Clear overflow flag
            P.Overflow = false;
        }

        private void CLD()
        {
            // Clear decimal flag
            P.Decimal = false;
        }

        private void SED()
        {
            // Set decimal flag
            P.Decimal = true;
        }

        private void NOP() { }

        private void LDA()
        {
            // Load accumulator with memory
            P.SetZN(A = AddressRead());
        }

        private void LDY()
        {
            // Load index Y with memory
            P.SetZN(Y = AddressRead());
        }

        private void LDX()
        {
            // Load index X with memory
            P.SetZN(X = AddressRead());
        }

        private void ORA()
        {
            // OR memory with accumulator, store in accumulator.
            P.SetZN(A |= AddressRead());
        }

        private void AND()
        {
            // AND memory with accumulator, store in accumulator.
            P.SetZN(A &= AddressRead());
        }

        private void EOR()
        {
            // XOR memory with accumulator, store in accumulator.
            P.SetZN(A ^= AddressRead());
        }

        private void SBC()
        {
            // SBC - Subtract with Carry
            //
            // A,Z,C,N = A-M-(1-C)
            ADCImpl((byte) ~AddressRead());
        }

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

        private void BRK()
        {
            PC++;
            PushWord(PC);
            PushPWithBreak();
            P.IrqDisable = true;
            PC = _memory.ReadWord((int)Interrupt.Irq);
        }

        private void CMP()
        {
            // CMP - Compare
            //
            // Z,C,N = A-M
            CMPImpl(A);
        }

        private void CPX()
        {
            // CPX - Compare
            //
            // Z,C,N = X-M
            CMPImpl(X);
        }

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

        private void LSR()
        {
            // LSR - Shift right one bit
            byte d = AddressRead();
            P.Carry = (d & 0x1) > 0;
            d >>= 1;
            AddressWrite(P.SetZN(d));
        }

        private void ASL()
        {
            // ASL - Arithmetic Shift Left
            byte d = AddressRead();
            P.Carry = (d & 0x80) > 0;
            d <<= 1;
            AddressWrite(P.SetZN(d));
        }

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

        private void SKB()
        {
            // Essentially a 2-byte NOP
        }
        
        private void ANC()
        {
            A &= AddressRead();
            P.Carry = P.Negative;
        }

        private void ALR()
        {
            A &= AddressRead();
            P.Carry = (A & 0x1) > 0;
            P.SetZN(A >>= 1);
        }

        private void ARR()
        {
            A &= AddressRead();
            bool c = P.Carry;
            P.Carry = (A & 0x1) > 0;
            A >>= 1;
            if (c) A |= 0x80;
            P.SetZN(A);
        }

        private void ATX()
        {
            // This opcode ORs the A register with #$EE, ANDs the result with an immediate 
            // value, and then stores the result in both A and X.
            A |= _memory.ReadByte(0xEE);
            A &= AddressRead();
            P.SetZN(X = A);
        }

        private void SLO()
        {
            //  SLO - Undocumented Opcode
            // 
            //  Equivalent to ASL value then ORA value, except supporting more addressing modes.
            //  LDA #0 followed by SLO is an efficient way to shift a variable while also loading it in A.
            
            ASL();
            ORA();
        }

        private void RLA()
        {
            //     RLA - Undocumented Opcode
            //
            //     Equivalent to ROL value then AND value, except supporting more addressing modes.
            //     LDA #$FF followed by RLA is an efficient way to rotate a variable while also loading it in A.
            ROL();
            AND();
        }

        private void RRA()
        {
            //     RRA - Undocumented Opcode
            //
            //     Equivalent to ROR value then ADC value, except supporting more addressing modes. Essentially
            //     this computes A + value / 2, where value is 9-bit and the division is rounded up.

            ROR();
            ADC();
        }

        private void SRE()
        {
            //     SRE - Undocumented Opcode
            //
            //     Equivalent to LSR value then EOR value, except supporting more addressing modes. LDA #0 followed
            //     by SRE is an efficient way to shift a variable while also loading it in A.
            LSR();
            EOR();
        }

        private void ISC()
        {
            //     ISC - Undocumented Opcode
            //
            //     Equivalent to INC value then SBC value, except supporting more addressing modes.
            INC();
            SBC();
        }

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

        
        private void SAX()
        {
            //     SAX - Undocumented Opcode
            //
            //     Stores the bitwise AND of A and X. As with STA and STX, no flags are affected.

            AddressWrite((byte)(A & X));
        }

        private void XAA()
        {
            // A = X & M
            byte d = AddressRead();
            P.SetZN(A = (byte) (X & d));
        }

        private void AXS()
        {
            // X = A & X - d
            byte d = AddressRead();
            int r = (A & X) - d;
            P.SetZN(X = (byte) (r & 0xFF));
            P.Carry = P.Negative;
        }
        
        private void AHX()
        {
            // M = A & X & H
            byte H = (byte) (_currentOpAddress >> 8);
            byte d = (byte) (A & X & H);
            AddressWrite(d);
        }

        private void SHX()
        {
            // M = X & H
            byte H = (byte) (_currentOpAddress >> 8);
            byte d = (byte) (X & H);
            AddressWrite(d);
        }

        private void SHY()
        {
            // M = Y & H
            byte H = (byte) (_currentOpAddress >> 8);
            byte d = (byte) (Y & H);
            AddressWrite(d);
        }

        private void TAS()
        {
            // S = A & X, M = S & H
            SP = (byte) (A & X);
            byte H = (byte) (_currentOpAddress >> 8);
            byte d = (byte) (SP & H);
            AddressWrite(d);
        }

        private void LAS()
        {
            // A, X, S = M & S
            byte d = AddressRead();
            SP = (byte) (SP & d);
            A = SP;
            X = SP;
        }

        private void KIL()
        {
            // Halt
            _halted = true;
        }
        #endregion
    }
}