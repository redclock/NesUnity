namespace NesUnity
{
    public partial class Ppu
    {
        private byte _oamAddress;
        private bool _addressFlip;
        private int _ppuAddress;
        private int _tempAddress;
        private byte _lastReadData;
        private int _scrollFineX;

        private byte[] _oam = new byte[0x100];

        public byte ReadRegister(int reg)
        {
            switch (reg)
            {
                case 0: // PPUCTRL
                    return PpuStatus.OpenBus;
                
                case 1: // PPUMASK
                    return PpuStatus.OpenBus;
                
                case 2: // PPUSTATUS
                    return ReadPpuStatus();
                
                case 3: // OAMADDR
                    return _oamAddress;
                
                case 4: // OAMDATA
                    return _oam[_oamAddress];
                
                case 5: // PPUSCROLL
                    return PpuStatus.OpenBus;
                
                case 6: // PPUADDR
                    return PpuStatus.OpenBus;
                
                case 7: // PPUDATA
                    return ReadPpuData();
            }

            return 0;
        }

        private byte ReadPpuStatus()
        {
            byte b = PpuStatus.ToByte();
            PpuStatus.VBlank = false;
            _addressFlip = false;
            return b;
        }

        private byte ReadPpuData()
        {
            byte b = _memory.ReadByte(_ppuAddress);
            
            // Buffered read emulation
            // https://wiki.nesdev.org/w/index.php/PPU_registers#The_PPUDATA_read_buffer_.28post-fetch.29
            if (_ppuAddress < 0x3F00)
            {
                (b, _lastReadData) = (_lastReadData, b);
            } else
            {
                _lastReadData = _memory.ReadByte(_ppuAddress - 0x1000);
            }
            
            _ppuAddress = (_ppuAddress + PpuCtrl.VRamIncrement) & 0x3FFF;

            return b;

        }

        public void WriteRegister(int reg, byte val)
        {
            switch (reg)
            {
                case 0: // PPUCTRL
                    PpuCtrl.FromByte(val);
                    //Set the nametable in the temp address, this will be reflected in the data address during rendering
                    _tempAddress &= ~0xC00;                 //Unset
                    _tempAddress |= (val & 0x3) << 10;      //Set according to ctrl bits
                    return;
                
                case 1: // PPUMASK
                    PpuMask.FromByte(val);
                    return;
                
                case 2: // PPUSTATUS
                    return;
                
                case 3: // OAMADDR
                    _oamAddress = val;
                    return;
                
                case 4: // OAMDATA
                    _oam[_oamAddress] = val;
                    unchecked
                    {
                        _oamAddress++;
                    }

                    return;
                
                case 5: // PPUSCROLL
                    WritePpuScroll(val);
                    return;
                
                case 6: // PPUADDR
                    WritePpuAddress(val);
                    return;
                
                case 7: // PPUDATA
                    _memory.WriteByte(_ppuAddress, val);
                    _ppuAddress = (_ppuAddress + PpuCtrl.VRamIncrement) & 0x3FFF;
                    return;
            }
        }

        private void WritePpuScroll(byte val)
        {
            if (_addressFlip)
            {
                // Y
                _tempAddress &= ~0x73E0; 
                _tempAddress |= ((val & 0x7) << 12) |
                                ((val & 0xF8) << 2);
            }
            else
            {
                // X
                _tempAddress &= ~0x1F;
                _tempAddress |= val >> 3;
                _scrollFineX = val & 0x00000111;
            }
            
            _addressFlip = !_addressFlip;

        }

        private void WritePpuAddress(byte val)
        {
            if (_addressFlip)
            {
                // y address
                _tempAddress &= ~0xFF; //Unset the lower byte;
                _tempAddress |= val;
                _ppuAddress = _tempAddress;
            }
            else
            {
                // x address
                _tempAddress &= ~0xFF00; //Unset the upper byte
                _tempAddress |= (val & 0x3F) << 8;
            }
            
            _addressFlip = !_addressFlip;

        }
    }
}