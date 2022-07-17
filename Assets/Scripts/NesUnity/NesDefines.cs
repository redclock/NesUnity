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
}