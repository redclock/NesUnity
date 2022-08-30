using System.Runtime.CompilerServices;
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
        public const int PRE_SCANLINE = 261;

        // 256 bytes OAM for 64 sprites x 4 bytes

        public Nes NesSys
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _nesSys; }
        }

        public PpuMemory Memory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _memory; }
        }

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
            _currentX = 0;
            _currentY = 0;
            _tempAddress = 0;
            _drawAddress = 0;
        }

        public void Tick()
        {
            Step();
        }
    }
}