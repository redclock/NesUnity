using UnityEngine;

namespace NesUnity.Mappers
{

 /*
  CPU $8000-$BFFF: First 16 KB of ROM.
  CPU $C000-$FFFF: Last 16 KB of ROM (NROM-256) or mirror of $8000-$BFFF (NROM-128).
  PPU $0000-$1FFF: CHR ROM
 */
 
 // ReSharper disable once InconsistentNaming
 public class NROM : MapperBase
{
    private NesRom _rom;

    public NROM(NesRom rom)
    {
        _rom = rom;
    }
    
    public override byte ReadByte(int address)
    {
        if (address >= 0x8000)
        {
            address -= 0x8000;
            if (address < _rom.prgRom.Length)
                return _rom.prgRom[address];
            else
                return _rom.prgRom[address - 0x4000];
        } else if (address < 0x2000)
        {
            return _rom.chrRom[address];
        }

        Debug.LogErrorFormat("NROM read invalid @ {0}", address);
        return 0;
    }

    public override void WriteByte(int address, byte val)
    {
        if (address >= 0x8000)
        {
            address -= 0x8000;
            if (address < 0x4000)
                _rom.prgRom[address] = val;
            else
                _rom.prgRom[address - 0x4000] = val;
        } else if (address < 0x2000)
        {
            _rom.chrRom[address] = val;
        } else
        {
            Debug.LogErrorFormat("NROM write invalid @ {0}", address);
        }
    }
}
}