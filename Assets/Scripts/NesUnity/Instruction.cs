namespace NesUnity
{
    public struct Instruction
    {
        public byte OpCode;
        public int OpCount;
        public int Cycles;
        public AddressingMode AddressMode;
    }
}