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
        None,
        Direct,
        Immediate,
        ZeroPage,
        Absolute,
        ZeroPageX,
        ZeroPageY,
        AbsoluteX,
        AbsoluteY,
        IndirectX,
        IndirectY, 
        Relative,
    }
}