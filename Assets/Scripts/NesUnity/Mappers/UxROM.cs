namespace NesUnity.Mappers
{
    // UxROM (Mapper 2): a switchable 16 KiB PRG bank at $8000 and a fixed
    // final PRG bank at $C000. CHR is normally 8 KiB of cartridge RAM.
    public sealed class UxROM : MapperBase
    {
        private readonly NesRom _rom;
        private int _prgBank;

        public int PrgBank => _prgBank;

        public UxROM(NesRom rom)
        {
            _rom = rom;
            _prgBank = 0;
        }

        public override byte ReadByte(int address)
        {
            address &= 0xFFFF;
            if (address >= 0xC000)
            {
                int fixedOffset = (_rom.prgRom.Length - 0x4000) + (address - 0xC000);
                return _rom.prgRom[fixedOffset % _rom.prgRom.Length];
            }

            if (address >= 0x8000)
            {
                int bankCount = _rom.prgRom.Length / 0x4000;
                int bank = bankCount == 0 ? 0 : _prgBank % bankCount;
                int offset = bank * 0x4000 + (address - 0x8000);
                return _rom.prgRom[offset % _rom.prgRom.Length];
            }

            if (address < 0x2000)
                return _rom.chrRom[address % _rom.chrRom.Length];

            return 0;
        }

        public override int ReadChrRom(int address)
        {
            address &= 0x1FFF;
            return ReadByte(address) | (ReadByte((address + 8) & 0x1FFF) << 8);
        }

        public override void WriteByte(int address, byte val)
        {
            address &= 0xFFFF;
            if (address >= 0x8000)
            {
                int bankCount = _rom.prgRom.Length / 0x4000;
                _prgBank = bankCount == 0 ? 0 : (val & 0x0F) % bankCount;
                return;
            }

            if (address < 0x2000 && _rom.HasChrRam)
                _rom.chrRom[address % _rom.chrRom.Length] = val;
        }
    }
}
