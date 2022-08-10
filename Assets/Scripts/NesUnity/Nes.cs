namespace NesUnity
{
    public class Nes
    {
        public Cpu cpu;
        public Ppu ppu;
        public NesRom rom;

        public bool isEndScreen;
        
        public Nes()
        {
            cpu = new Cpu(this);
            ppu = new Ppu(this);
        }

        public bool PowerOn(byte[] romBytes, int pc = -1)
        {
            rom = new NesRom();
            if (!rom.ReadFromBytes(romBytes))
                return false;
            cpu.Reset(pc);
            ppu.Reset();
            return true;
        }

        public void Tick()
        {
            isEndScreen = false;
            ppu.Tick();
            ppu.Tick();
            ppu.Tick();
            cpu.Tick();
        }
    }
}