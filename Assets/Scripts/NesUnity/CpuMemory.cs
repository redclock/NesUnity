using System.Runtime.CompilerServices;
using NesUnity.Mappers;

namespace NesUnity
{
/*
  6502 CPU Memory Model
    Address range	Size	Device
    $0000-$07FF	    $0800	2KB internal RAM
    $0800-$0FFF	    $0800	Mirrors of $0000-$07FF
    $1000-$17FF	    $0800   Mirrors of $0000-$07FF
    $1800-$1FFF	    $0800   Mirrors of $0000-$07FF
    $2000-$2007	    $0008	NES PPU registers
    $2008-$3FFF	    $1FF8	Mirrors of $2000-2007 (repeats every 8 bytes)
    $4000-$4017	    $0018	NES APU and I/O registers
    $4018-$401F	    $0008	APU and I/O functionality that is 
                            normally disabled. See CPU Test Mode.
    $4020-$5FFF     $1FDF   Expansion ROM
    $6000-$7FFF	    $2000	Save RAM 
    $8000-$BFFF	    $4000	RPG ROM 
    $C000-$FFFF	    $400	RPG ROM 
 */

public class CpuMemory
{
    // internal RAM
    private byte[] _ram = new byte[0x800];

    // save RAM
    private byte[] _sram = new byte[0x2000];

    private MapperBase _mapper;

    public CpuMemory(MapperBase mapper)
    {
        _mapper = mapper;
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

    public byte ReadByte(int address)
    {
        if (address < 0x2000)
        {
            // $0000-$1FFF
            // Mirroring of RAM
            return _ram[address & 0x7ff];
        }

        if (address < 0x4000)
        {
            // $2000-$3FFF
            // Mirroring of PPU registers (repeat every 7 bytes)
            return ReadPPURegister(address & 0x07);
        }

        if (address < 0x4020)
        {
            // $4000-$4020
            // APU Registers
            return ReadAPURegister(address);
        }

        if (address < 0x6000)
        {
            // $4000-$5FFF
            // Expansion ROM
            return 0;
        }

        if (address < 0x8000)
        {
            // $6000-$7FFF
            // Save RAM
            return _sram[address - 0x6000];
        }

        return _mapper.ReadByte(address & 0xFFFF);
    }

    private byte ReadPPURegister(int address)
    {
        // TODO 
        return 0;
    }

    private byte ReadAPURegister(int address)
    {
        // TODO 
        return 0;
    }

    public void WriteByte(int address, byte val)
    {
        if (address < 0x2000)
        {
            // $0000-$1FFF
            // Mirroring of RAM
            _ram[address & 0x7ff] = val;
            
        } else if (address < 0x4000)
        {
            // $2000-$3FFF
            // Mirroring of PPU registers (repeat every 7 bytes)
            WritePPURegister(address & 0x07, val);
            
        } else if (address < 0x4020)
        {
            // $4000-$4020
            // APU Registers
            WriteAPURegister(address, val);
            
        } else if (address < 0x6000)
        {
            // $4000-$5FFF
            // Expansion ROM
            
        } else if (address < 0x8000)
        {
            // $6000-$7FFF
            // Save RAM
            _sram[address - 0x6000] = val;
            
        } else
        {
            _mapper.ReadByte(address & 0xFFFF);
        }
    }

    private void WritePPURegister(int address, byte val)
    {
        // TODO 
    }

    private void WriteAPURegister(int address, byte val)
    {
        // TODO 
    }


    public int GetInterruptVector(Interrupt interrupt)
    {
        return ReadWord((int) interrupt);
    }
}
}