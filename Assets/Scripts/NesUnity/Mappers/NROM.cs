using UnityEngine;

namespace NesUnity.Mappers
{

 /*
  CPU $8000-$BFFF: First 16 KB of ROM.
  CPU $C000-$FFFF: Last 16 KB of ROM (NROM-256) or mirror of $8000-$BFFF (NROM-128).
 */
 
public class NROM : MapperBase
{
    private byte[] _prgRom;

    public NROM(NesRom rom)
    {
        _prgRom = rom.prgRom;
    }

    public override byte ReadByte(int address)
    {
        if (address >= 0x8000)
        {
            address -= 0x8000;
            if (address < 0x4000)
                return _prgRom[address];
            else
                return _prgRom[address - 0x4000];
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
                _prgRom[address] = val;
            else
                _prgRom[address - 0x4000] = val;
        } else
        {
            Debug.LogErrorFormat("NROM write invalid @ {0}", address);
        }
    }
}
}