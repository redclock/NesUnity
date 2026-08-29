
using UnityEngine;

namespace NesUnity.Mappers {
    public abstract class MapperBase {
        public abstract byte ReadByte(int address);
        public abstract void WriteByte(int address, byte val);

        // Read byte and byte + 8 in Chr Rom
        // For fast access
        public abstract int ReadChrRom(int address);

        public virtual bool PrgRamEnabled => true;

        public virtual bool PrgRamWritable => PrgRamEnabled;

        public virtual void AttachNes(Nes nes)
        {
        }

        // Scanline-level PPU implementations can use this hook to model
        // mapper scanline counters without requiring a per-dot CHR bus.
        public virtual void ClockScanline()
        {
        }

        public virtual MirrorMode GetMirrorMode(MirrorMode fallback)
        {
            return fallback;
        }
        public static MapperBase Create(NesRom rom, int mapperNumber)
        {
            switch (mapperNumber)
            {
                case 0:
                    return new NROM(rom);
                case 3:
                    return new CNROM(rom);
                case 2:
                    return new UxROM(rom);
                case 1:
                    return new MMC1(rom);
                case 4:
                    return new MMC3(rom);
                default:
                    Debug.LogError("Unsupported Mapper " + mapperNumber);
                    return null;
            }
        }


    }
}
