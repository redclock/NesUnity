using System;
using System.Runtime.CompilerServices;
using NesUnity.Mappers;

namespace NesUnity
{
/*
  PPU Memory Model
    Address range	Size	Description
    $0000-$0FFF	    $1000	Pattern table 0
    $1000-$1FFF	    $1000	Pattern table 1
    $2000-$23FF	    $0400	Nametable 0
    $2400-$27FF	    $0400	Nametable 1
    $2800-$2BFF	    $0400	Nametable 2
    $2C00-$2FFF	    $0400	Nametable 3
    $3000-$3EFF	    $0F00	Mirrors of $2000-$2EFF
    $3F00-$3F1F	    $0020	Palette RAM indexes
    $3F20-$3FFF	    $00E0	Mirrors of $3F00-$3F1F
 */

public class PpuMemory
{
    private Ppu _ppu;
    
    // Internal 4K VRAM for 4 Name table
    private byte[] _vram = new byte[0x1000];
    
    // 256 bytes OAM for 64 sprites x 4 bytes
    private byte[] _oam = new byte[0x100]; 

    // 64 bytes for palette
    private byte[] _palette = new byte[0x20];

    private int[] _mirrorNameTable;
    private MapperBase _mapper;
    
    public PpuMemory(Ppu ppu)
    {
        _ppu = ppu;
        _mapper = ppu.NesSys.rom.mapper;
        InitNameTableMap();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadWord(int address)
    {
        return ReadByte(address) + (ReadByte(address + 1) << 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteWord(int address, int word)
    {
        WriteByte(address, (byte) (word & 0xFF));
        WriteByte(address + 1, (byte) (word >> 8));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetNameTableAddress(int address)
    {
        // Each bank size $400 = 1K
        int bank = (address  & 0x0FFF) >> 10;
            
        // Map to address - $2000
        return (_mirrorNameTable[bank] << 10) | (address & 0x3FF);  
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetPaletteAddress(int address)
    {
        // Mirror 64 bytes
        address &= 0x1F;
        // Mirror BG colors
        if (address == 0x10 || address == 0x14 || address == 0x18 || address == 0x1C)
            address -= 0x10;
        
        return address;
    }

    public byte ReadByte(int address)
    {
        address &= 0x3FFF;

        if (address < 0x2000)
        {
            // $0000-$1FFF
            // Pattern tables
            return _mapper.ReadByte(address);
        }

        if (address < 0x2FFF)
        {
            // $2000-$2FFF
            // Name tables 
            // $3000-$3EFF Mirrors of $2000-$2EFF
            return _vram[GetNameTableAddress(address)];
        }

        if (address < 0x4000)
        {
            // $3F00-$3F1F Palette RAM indexes
            // $3F20-$3FFF Mirrors of $3F00-$3F1F
            return _palette[GetPaletteAddress(address)];
        }

        return 0;
    }

    public void WriteByte(int address, byte val)
    {
        address &= 0x3FFF;
        if (address < 0x2000)
        {
            // $0000-$1FFF
            // Pattern tables
            _mapper.WriteByte(address, val);
        }

        if (address < 0x2FFF)
        {
            // $2000-$2FFF
            // Name tables 
            // $3000-$3EFF Mirrors of $2000-$2EFF
            _vram[GetNameTableAddress(address)] = val;
        }

        if (address < 0x4000)
        {
            // $3F00-$3F1F Palette RAM indexes
            // $3F20-$3FFF Mirrors of $3F00-$3F1F
            _palette[GetPaletteAddress(address)] = val;
        }

    }

    private void InitNameTableMap()
    {
        switch (_ppu.NesSys.rom.mirrorMode)
        {
            case MirrorMode.Horizontal:
                _mirrorNameTable = new[] {0, 0, 1, 1};
                break;
            case MirrorMode.Vertical:
                _mirrorNameTable = new[] {0, 1, 0, 1};
                break;
            case MirrorMode.FourScreen:
                _mirrorNameTable = new[] {0, 1, 2, 3};
                break;
            case MirrorMode.Upper:
                _mirrorNameTable = new[] {0, 0, 0, 0};
                break;
            case MirrorMode.Lower:
                _mirrorNameTable = new[] {1, 1, 1, 1};
                break;
        }
    }

}
}