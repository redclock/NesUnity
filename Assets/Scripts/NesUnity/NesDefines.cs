namespace NesUnity
{
    public enum MirrorMode
    {
        Horizontal = 0, 
        Vertical = 1, 
        FourScreen = 2, 
        Upper = 3, 
        Lower = 4
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