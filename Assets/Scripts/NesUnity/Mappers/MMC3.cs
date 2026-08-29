namespace NesUnity.Mappers
{
    // MMC3 (Mapper 4) bank controller. The scanline IRQ counter is clocked by
    // Ppu's scanline hook because this project intentionally uses scanline-level
    // rendering instead of a complete per-dot CHR fetch pipeline.
    public sealed class MMC3 : MapperBase
    {
        private readonly NesRom _rom;
        private Nes _nes;
        private readonly byte[] _chrBanks = new byte[6];
        private byte _prgBank6;
        private byte _prgBank7;
        private byte _bankSelect;
        private byte _mirroring;
        private bool _mirroringWritten;
        private byte _prgRamControl = 0x80;
        private byte _irqLatch;
        private byte _irqCounter;
        private bool _irqReload;
        private bool _irqEnabled;

        public int SelectedBank => _bankSelect & 7;
        public bool PrgMode => (_bankSelect & 0x40) != 0;
        public bool ChrInversion => (_bankSelect & 0x80) != 0;
        public byte IrqLatch => _irqLatch;
        public byte IrqCounter => _irqCounter;
        public bool IrqEnabled => _irqEnabled;
        public override bool PrgRamEnabled => (_prgRamControl & 0x80) != 0;
        public override bool PrgRamWritable => PrgRamEnabled && (_prgRamControl & 0x40) == 0;

        public MMC3(NesRom rom)
        {
            _rom = rom;
        }

        public override void AttachNes(Nes nes)
        {
            _nes = nes;
        }

        public override MirrorMode GetMirrorMode(MirrorMode fallback)
        {
            // A000=0 selects vertical CIRAM arrangement, A000=1 horizontal.
            if (_rom.mirrorMode == MirrorMode.FourScreen)
                return MirrorMode.FourScreen;
            if (!_mirroringWritten)
                return fallback;
            return (_mirroring & 1) == 0 ? MirrorMode.Vertical : MirrorMode.Horizontal;
        }

        public override byte ReadByte(int address)
        {
            address &= 0xFFFF;
            if (address >= 0x8000)
            {
                int bank = GetPrgBank(address);
                return ReadPrg8K(bank, address & 0x1FFF);
            }

            if (address < 0x2000)
            {
                int bank = GetChrBank(address);
                return ReadChr1K(bank, address & 0x03FF);
            }

            return 0;
        }

        public override int ReadChrRom(int address)
        {
            address &= 0x1FFF;
            return ReadByte(address) | (ReadByte((address + 8) & 0x1FFF) << 8);
        }

        public override void WriteByte(int address, byte value)
        {
            address &= 0xFFFF;
            if (address < 0x2000)
            {
                if (_rom.HasChrRam)
                {
                    int bank = GetChrBank(address);
                    int index = (bank * 0x0400 + (address & 0x03FF)) % _rom.chrRom.Length;
                    _rom.chrRom[index] = value;
                }
                return;
            }

            if (address < 0x8000)
                return;

            bool even = (address & 1) == 0;
            switch (address & 0xE001)
            {
                case 0x8000:
                case 0x8001:
                    if (even)
                        _bankSelect = value;
                    else
                        WriteBankData(value);
                    break;
                case 0xA000:
                case 0xA001:
                    if (even)
                        _mirroring = value;
                    else
                        _prgRamControl = value;
                    if (even)
                        _mirroringWritten = true;
                    break;
                case 0xC000:
                case 0xC001:
                    if (even)
                        _irqLatch = value;
                    else
                        _irqReload = true;
                    break;
                case 0xE000:
                case 0xE001:
                    if (even)
                    {
                        _irqEnabled = false;
                    }
                    else
                    {
                        _irqEnabled = true;
                    }
                    break;
            }
        }

        public override void ClockScanline()
        {
            if (_irqReload || _irqCounter == 0)
            {
                _irqCounter = _irqLatch;
                _irqReload = false;
            }
            else
            {
                _irqCounter--;
            }

            if (_irqCounter == 0 && _irqEnabled && _nes != null)
                _nes.cpu.TriggerInterrupt(Interrupt.Irq);
        }

        private void WriteBankData(byte value)
        {
            switch (_bankSelect & 7)
            {
                case 0:
                case 1:
                    _chrBanks[_bankSelect & 7] = (byte)(value & 0xFE);
                    break;
                case 2:
                case 3:
                case 4:
                case 5:
                    _chrBanks[_bankSelect & 7] = value;
                    break;
                case 6:
                    _prgBank6 = (byte)(value & 0x3F);
                    break;
                case 7:
                    _prgBank7 = (byte)(value & 0x3F);
                    break;
            }
        }

        private int GetPrgBank(int address)
        {
            int bankCount = _rom.prgRom.Length / 0x2000;
            if (bankCount == 0)
                return 0;

            int last = bankCount - 1;
            int secondLast = bankCount > 1 ? bankCount - 2 : 0;
            int bank;
            int slot = (address - 0x8000) >> 13;
            if (!PrgMode)
            {
                bank = slot == 0 ? _prgBank6 :
                    slot == 1 ? _prgBank7 :
                    slot == 2 ? secondLast : last;
            }
            else
            {
                bank = slot == 0 ? secondLast :
                    slot == 1 ? _prgBank7 :
                    slot == 2 ? _prgBank6 : last;
            }
            return bank % bankCount;
        }

        private int GetChrBank(int address)
        {
            int bankCount = _rom.chrRom.Length / 0x0400;
            if (bankCount == 0)
                return 0;

            int slot = address >> 10;
            int bank;
            if (!ChrInversion)
            {
                if (slot == 0) bank = _chrBanks[0];
                else if (slot == 1) bank = _chrBanks[0] + 1;
                else if (slot == 2) bank = _chrBanks[1];
                else if (slot == 3) bank = _chrBanks[1] + 1;
                else bank = _chrBanks[slot - 2];
            }
            else
            {
                if (slot == 0) bank = _chrBanks[2];
                else if (slot == 1) bank = _chrBanks[3];
                else if (slot == 2) bank = _chrBanks[4];
                else if (slot == 3) bank = _chrBanks[5];
                else if (slot == 4) bank = _chrBanks[0];
                else if (slot == 5) bank = _chrBanks[0] + 1;
                else if (slot == 6) bank = _chrBanks[1];
                else bank = _chrBanks[1] + 1;
            }
            return bank % bankCount;
        }

        private byte ReadPrg8K(int bank, int offset)
        {
            int index = (bank * 0x2000 + offset) % _rom.prgRom.Length;
            return _rom.prgRom[index];
        }

        private byte ReadChr1K(int bank, int offset)
        {
            int index = (bank * 0x0400 + offset) % _rom.chrRom.Length;
            return _rom.chrRom[index];
        }
    }
}
