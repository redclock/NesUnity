namespace NesUnity.Mappers
{
    // MMC1 (Mapper 1) serial register implementation. This covers the common
    // SNROM/SxROM configurations used by desktop test ROMs.
    public sealed class MMC1 : MapperBase
    {
        private readonly NesRom _rom;
        private byte _shift = 0x10;
        private byte _control = 0x0C;
        private byte _chrBank0;
        private byte _chrBank1;
        private byte _prgBank;

        public byte Control => _control;
        public byte PrgBank => _prgBank;
        public byte ChrBank0 => _chrBank0;
        public byte ChrBank1 => _chrBank1;

        public MMC1(NesRom rom)
        {
            _rom = rom;
        }

        public override byte ReadByte(int address)
        {
            address &= 0xFFFF;
            if (address >= 0x8000)
            {
                int mode = (_control >> 2) & 3;
                int bankCount = _rom.prgRom.Length / 0x4000;
                if (bankCount == 0)
                    return 0;

                int bank;
                int offset;
                if (mode <= 1)
                {
                    int pair = (_prgBank & 0x0E) % bankCount;
                    bank = address < 0xC000 ? pair : (pair + 1) % bankCount;
                    offset = address & 0x3FFF;
                }
                else if (mode == 2)
                {
                    bank = address < 0xC000 ? 0 : _prgBank % bankCount;
                    offset = address & 0x3FFF;
                }
                else
                {
                    bank = address < 0xC000 ? _prgBank % bankCount : bankCount - 1;
                    offset = address & 0x3FFF;
                }

                return _rom.prgRom[bank * 0x4000 + offset];
            }

            if (address < 0x2000)
            {
                int chrBank;
                if ((_control & 0x10) == 0)
                    chrBank = (_chrBank0 & 0x1E) * 0x1000 + address;
                else if (address < 0x1000)
                    chrBank = (_chrBank0 * 0x1000) + address;
                else
                    chrBank = (_chrBank1 * 0x1000) + (address - 0x1000);

                return _rom.chrRom[chrBank % _rom.chrRom.Length];
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
            if (address < 0x8000)
            {
                if (address < 0x2000 && _rom.HasChrRam)
                    _rom.chrRom[address % _rom.chrRom.Length] = val;
                return;
            }

            if ((val & 0x80) != 0)
            {
                _shift = 0x10;
                _control |= 0x0C;
                return;
            }

            bool complete = (_shift & 1) != 0;
            _shift = (byte)((_shift >> 1) | ((val & 1) << 4));
            if (!complete)
                return;

            byte value = (byte)(_shift & 0x1F);
            if (address < 0xA000)
                _control = value;
            else if (address < 0xC000)
                _chrBank0 = value;
            else if (address < 0xE000)
                _chrBank1 = value;
            else
                _prgBank = value;
            _shift = 0x10;
        }
    }
}
