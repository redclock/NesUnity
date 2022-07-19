using NesUnity.Mappers;

namespace NesUnity
{
    public class Nes
    {
        private Cpu _cpu;
        private NesRom _rom;

        public Cpu cpu => _cpu;
        public MapperBase Mapper => _rom.mapper;
        
        public Nes()
        {
            _cpu = new Cpu(this);
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