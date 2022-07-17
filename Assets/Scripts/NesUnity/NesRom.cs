using System;
using NesUnity.Mappers;
using UnityEngine;

namespace NesUnity
{
    public class NesRom
    {
        private const int HEADER_LEN = 16;
        private const int MAGIC = 0x4E45531A;
        private const int PRG_UNIT = 0x4000;
        private const int CHR_UNIT = 0x2000;
        private const int TRAINER_LEN = 512;

        private byte[] _rawBytes;
        private MirrorMode _mirrorMode;
        private int _mapperNumber;
        private bool _hasTrainer;
        private bool _hasSRam;
        public byte[] prgRom;
        public byte[] chrRom;

        public PatternTable chrPatternTable;
        public MapperBase mapper;
        
        public bool ReadFromBytes(byte[] bytes)
        {
            _rawBytes = bytes;
            if (!ReadHeader())
                return false;
            
            chrPatternTable = new PatternTable(chrRom);
            
            mapper = MapperBase.Create(this, _mapperNumber);

            return true;
        }

        private bool Error(string message)
        {
            Debug.LogErrorFormat("Nes error: {0}", message);
            return false;
        }
        
        private bool ReadHeader()
        {
            /*
             The format of the header is as follows:
                0-3: Constant $4E $45 $53 $1A ("NES" followed by MS-DOS end-of-file)
                4: Size of PRG ROM in 16 KB units
                5: Size of CHR ROM in 8 KB units (Value 0 means the board uses CHR RAM)
                6: Flags 6 - Mapper, mirroring, battery, trainer
                7: Flags 7 - Mapper, VS/Playchoice, NES 2.0
                8: Flags 8 - PRG-RAM size (rarely used extension)
                9: Flags 9 - TV system (rarely used extension)
                10: Flags 10 - TV system, PRG-RAM presence (unofficial, rarely used extension)
                11-15: Unused padding (should be filled with zero, but some rippers put their name across bytes 7-15)
             */
            if (_rawBytes.Length < HEADER_LEN)
                return Error("header length");

            int magicWord = _rawBytes[0] << 24 | _rawBytes[1] << 16 | _rawBytes[2] << 8 | _rawBytes[3];
            if (magicWord != MAGIC)
                return Error("magic word not NES<EOF>");
            
            ExtractFlag6(_rawBytes[6]);
            ExtractFlag7(_rawBytes[7]);

            Debug.LogFormat("Mapper = {0}, Mirror = {1}, Trainer = {2}, SRAM = {3}", _mapperNumber, _mirrorMode, _hasTrainer, _hasSRam);
            
            // PRG / CHR ROM
            Debug.LogFormat("PRG Size = {0}*16K, CHR Size = {1}*8K", _rawBytes[4], _rawBytes[5]);
            int sizePrg = _rawBytes[4] * PRG_UNIT;
            int sizeChr = _rawBytes[5] * CHR_UNIT;

            prgRom = new byte[sizePrg];
            int  prgRomOffset = HEADER_LEN;
            
            if (_hasTrainer)
                prgRomOffset += TRAINER_LEN;

            if (_rawBytes.Length < prgRomOffset + sizePrg)
                return Error("PRG ROM size exceed");

            Array.Copy(_rawBytes, prgRomOffset, prgRom, 0, sizePrg);

            if (sizeChr == 0)
            {
                 chrRom = new byte[CHR_UNIT];
            }
            else
            {
                chrRom = new byte[sizeChr];
                if (_rawBytes.Length < prgRomOffset + sizePrg + sizeChr)
                    return Error("CHR ROM size exceed");
                Array.Copy(_rawBytes, prgRomOffset + sizePrg, chrRom, 0, sizeChr);
            }

            return true;
        }

        private void ExtractFlag6(byte flag)
        {
            /*
                76543210
                ||||||||
                |||||||+- Mirroring: 0: horizontal (vertical arrangement) (CIRAM A10 = PPU A11)
                |||||||              1: vertical (horizontal arrangement) (CIRAM A10 = PPU A10)
                ||||||+-- 1: Cartridge contains battery-backed PRG RAM ($6000-7FFF) or other persistent memory
                |||||+--- 1: 512-byte trainer at $7000-$71FF (stored before PRG data)
                ||||+---- 1: Ignore mirroring control or above mirroring bit; instead provide four-screen VRAM
                ++++----- Lower nybble of mapper number
             */
            
            bool mirrorV    = (flag & 0b00000001) > 0;
            _hasSRam        = (flag & 0b00000010) > 0;
            _hasTrainer     = (flag & 0b00000100) > 0;
            bool fourScreen = (flag & 0b00001000) > 0;
            _mapperNumber   = flag >> 4;
            _mirrorMode = fourScreen ? MirrorMode.FourScreen :
                mirrorV ? MirrorMode.Vertical : MirrorMode.Horizontal;
        }
        
        private void ExtractFlag7(byte flag)
        {
            /*
                76543210
                ||||||||
                |||||||+- VS Unisystem
                ||||||+-- PlayChoice-10 (8KB of Hint Screen data stored after CHR data)
                ||||++--- If equal to 2, flags 8-15 are in NES 2.0 format
                ++++----- Upper nybble of mapper number             
            */
            _mapperNumber |= flag & 0b11110000;
        }

    }   
}

