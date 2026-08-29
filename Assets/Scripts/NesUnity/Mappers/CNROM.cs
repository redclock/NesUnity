namespace NesUnity.Mappers
{
    // CNROM keeps the CPU PRG mapping fixed and switches the 8 KiB CHR bank
    // when the CPU writes to the cartridge address range.
    public sealed class CNROM : MapperBase
    {
        private readonly NesRom _rom;
        private int _chrBank;

        public int ChrBank => _chrBank;

        public CNROM(NesRom rom)
        {
            _rom = rom;
        }

        public override byte ReadByte(int address)
        {
            address &= 0xFFFF;
            if (address >= 0x8000)
            {
                int offset = address - 0x8000;
                if (offset < _rom.prgRom.Length)
                    return _rom.prgRom[offset];
                return _rom.prgRom[offset - 0x4000];
            }

            if (address < 0x2000)
            {
                int offset = _chrBank * 0x2000 + address;
                if (offset < _rom.chrRom.Length)
                    return _rom.chrRom[offset];
            }

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
                int bankCount = _rom.chrRom.Length / 0x2000;
                if (bankCount > 0)
                    _chrBank = (val & 0x03) % bankCount;
                return;
            }

            if (address < 0x2000 && _rom.HasChrRam)
            {
                int offset = _chrBank * 0x2000 + address;
                if (offset < _rom.chrRom.Length)
                    _rom.chrRom[offset] = val;
            }
        }
    }
}
