using System.Runtime.CompilerServices;

namespace NesUnity
{
    public partial class Cpu
    {
        // ReSharper disable once InconsistentNaming
        public int PC;
        public byte A;
        // ReSharper disable once InconsistentNaming
        public byte SP;
        public byte X;
        public byte Y;
        
        public struct CpuFlags
        {
            public bool Carry;
            public bool Zero;
            public bool IrqDisable;
            public bool Decimal;
            public bool Break;
            public bool Overflow;
            public bool Negative;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            // ReSharper disable once InconsistentNaming
            public byte SetZN(byte val)
            {
                Zero = val == 0;
                Negative = (val & 0x80) > 0;
                return val;
            }

            public byte ToByte()
            {
                int p = Carry    ? 1 : 0;
                p |= (Zero       ? 1 : 0) << 1;
                p |= (IrqDisable ? 1 : 0) << 2;
                p |= (Decimal    ? 1 : 0) << 3;
                p |= (Break      ? 1 : 0) << 4;
                p |= 1 << 5;
                p |= (Overflow   ? 1 : 0) << 6;
                p |= (Negative   ? 1 : 0) << 7;
                return (byte) p;
            }

            public byte FromByte(byte p)
            {
                Carry      = (p & 1) > 0;
                Zero       = (p & (1 << 1)) > 0;
                IrqDisable = (p & (1 << 2)) > 0;
                Decimal    = (p & (1 << 3)) > 0; 
                Break      = (p & (1 << 4)) > 0;
                Overflow   = (p & (1 << 6)) > 0;
                Negative   = (p & (1 << 7)) > 0;
                return p;
            }
        }

        public CpuFlags P;
    }
}