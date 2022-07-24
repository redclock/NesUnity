using UnityEngine;

namespace NesUnity
{
    public partial class Ppu
    {
        private PpuMemory _memory;
        private Nes _nesSys;

        public const int X_PIXELS = 256;
        public const int Y_PIXELS = 240;
        public const int X_CYCLES = 341;
        public const int Y_SCANLINES = 262;

        // 256 bytes OAM for 64 sprites x 4 bytes

        public Nes NesSys => _nesSys;
        public PpuMemory Memory => _memory;
        
        public Ppu(Nes nes)
        {
            _nesSys = nes;
        }

        public void Reset()
        {
            _memory = new PpuMemory(this);
            PpuCtrl.FromByte(0);
            PpuMask.FromByte(0);
            PpuStatus.Sprite0Hit = false;
            PpuStatus.SpriteOverflow = false;
            PpuStatus.VBlank = false;
            PpuStatus.OpenBus = 0;
            _ppuAddress = 0;
            _addressFlip = false;
            _lastReadData = 0;
        }

        public void Tick()
        {
            Step();
        }
    }
}