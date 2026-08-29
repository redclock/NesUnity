namespace NesUnity
{
    public class Nes
    {
        public Cpu cpu;
        public Ppu ppu;
        public NesRom rom;

        public NesController Controller1 { get; }
        public Apu apu { get; }

        public bool isEndScreen;
        public bool FrameReady { get; private set; }
        public int FrameCount { get; private set; }

        private const int MaxTicksPerFrame = 30000;
        
        public Nes()
        {
            Controller1 = new NesController();
            apu = new Apu();
            cpu = new Cpu(this);
            ppu = new Ppu(this);
        }

        public bool PowerOn(byte[] romBytes, int pc = -1)
        {
            if (romBytes == null || romBytes.Length == 0)
                return false;

            rom = new NesRom();
            if (!rom.ReadFromBytes(romBytes))
                return false;

            if (rom.mapper == null)
                return false;

            rom.mapper.AttachNes(this);
            cpu.Reset(pc);
            ppu.Reset();
            Controller1.Reset();
            apu.Reset();
            isEndScreen = false;
            FrameReady = false;
            FrameCount = 0;
            return true;
        }

        public void Tick()
        {
            ppu.Tick();
            ppu.Tick();
            ppu.Tick();
            cpu.Tick();
            apu.Tick();
        }

        public bool RunFrame()
        {
            if (rom == null || rom.mapper == null)
                return false;

            Controller1.Latch();
            isEndScreen = false;
            FrameReady = false;

            int ticks = 0;
            while (!isEndScreen && ticks++ < MaxTicksPerFrame)
                Tick();

            if (!isEndScreen)
                return false;

            FrameReady = true;
            FrameCount++;
            return true;
        }
    }
}
