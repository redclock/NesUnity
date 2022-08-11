namespace NesUnity
{
    public class Instruction
    {
        public delegate void OpFunc();
        public string Name;
        public byte Code;
        public AddressingMode Mode;
        public int Cycles;
        public OpFunc Func;
        public bool PageBoundary;
        public bool Rmw;
    }
}