using NesUnity.Mappers;

namespace NesUnity
{
    public class Nes
    {
        private Cpu _cpu;
        private Ppu _ppu;
        private NesRom _rom;

        public Cpu cpu => _cpu;
        public Ppu ppu => _ppu;
        public NesRom rom => _rom;
        
        public Nes()
        {
            _cpu = new Cpu(this);
            _ppu = new Ppu(this);
        }

        public bool PowerOn(byte[] romBytes, int pc = -1)
        {
            _rom = new NesRom();
            if (!_rom.ReadFromBytes(romBytes))
                return false;
            _cpu.Reset(pc);

            return true;
        }
    }
}