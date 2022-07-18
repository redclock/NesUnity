namespace NesUnity
{
    public enum MirrorMode
    {
        Horizontal, Vertical, FourScreen
    }

    public enum Interrupt
    {
        Nmi = 0xFFFA,
        Reset = 0xFFFC,
        Irq = 0xFFFE
    }
    
    public enum AddressingMode
    {
        Implicit,
        Accumulator,
        Immediate,
        ZeroPage,
        Absolute,
        ZeroPageX,
        ZeroPageY,
        AbsoluteX,
        AbsoluteY,
        Indirect,
        IndirectX,
        IndirectY, 
        Relative,
    }
}