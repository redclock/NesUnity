
using System.Runtime.CompilerServices;

namespace NesUnity
{
    public partial class Ppu
    {
        // yyy NN YYYYY XXXXX
        // ||| || ||||| +++++-- coarse X scroll
        // ||| || +++++-------- coarse Y scroll
        // ||| ++-------------- nametable select
        // +++----------------- fine Y scroll
        // current ppu r/w address
        private int _ppuAddress;
        // left top screen address
        private int _tempAddress;
        // current draw address
        private int _drawAddress;
        
        private int _scrollFineX;

        private int _currentX;
        private int _currentY;
        public int TempAddress => _tempAddress;

        public int[] pixels = new int[Y_PIXELS * X_PIXELS];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCoarseX(int address)
        {
            return address & 0b0000000011111;
        } 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCoarseY(int address)
        {
            return address & 0b0001111100000;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFineY(int address)
        {
            return address & 0b1110000000000;
        }

        private void Step()
        {
            _currentX++;
            if (_currentX == X_CYCLES)
            {
                _currentX = 0;
                _currentY++;
                if (_currentY == Y_PIXELS)
                {
                    TriggerNmi();
                    _nesSys.isEndScreen = true;
                } else if (_currentY == Y_SCANLINES)
                {
                    PpuStatus.VBlank = false;
                    _currentY = 0;
                } 
            }
        }

        private void StepPreScanline(int x)
        {
            if (x == 0)
            {
                PpuStatus.VBlank = false;
                PpuStatus.Sprite0Hit = false;
            } else if (x >= 280 && x <= 304)
            {
                // 280 - 304 reload Y
                // vert(v) = vert(t)
                _drawAddress = (_drawAddress & 0b1000010000011111) | (_tempAddress & 0b111101111100000);
            }
            else if (x == 257)
            {
                // 257 - 320
                _drawAddress = (_drawAddress & 0b1111101111100000) | (_tempAddress & 0b000010000011111);
            }
        }
        
        private void TriggerNmi()
        {
            PpuStatus.VBlank = true;
            if (PpuCtrl.NmiEnabled)
                _nesSys.cpu.TriggerInterrupt(Interrupt.Nmi);
        }

        public void GenBackground(int nameIndex)
        {
            int addressBase = _memory.GetNameTableAddress(nameIndex);
            int addressAttrBase = addressBase + 0x3C0;
            int pixelIndex = (Y_PIXELS - 1) * X_PIXELS;
            byte[] vram = _memory.Vram;
            byte[] palette = _memory.Palette;
            
            for (int y = 0; y < Y_PIXELS; y++)
            {
                int coarseY = y >> 3;
                int fineY = y & 0b111;
                for (int coarseX = 0; coarseX < 32; coarseX++)
                {
                    int addressName = coarseY * 32 + coarseX;
                    int tileIndex = vram[addressBase + addressName];
                    int tileAddress = PpuCtrl.BackgroundChrAddress + tileIndex * 16 + fineY;
                    int bgPattern = _memory.ReadChrRom(tileAddress);
                    byte byte1 = (byte)(bgPattern & 0xFF);
                    byte byte2 = (byte)(bgPattern >> 8);
                    // interleave every bit to 2 bits
                    int interleaved = Utils.Interleave8To16(byte1) | (Utils.Interleave8To16(byte2) << 1);

                    int addressAttr = coarseY / 4 * 8 + coarseX / 4;
                    byte attr = vram[addressAttrBase + addressAttr];

                    // left-top: 0, right-top: 2, left-bottom: 4, right-bottom: 6
                    int attrBit = ((coarseY & 0b10) << 1) | (coarseX & 0b10);
                    int attrByte = ((attr >> attrBit) & 0b11) << 2;
                    int shift = 14;
                    for (int offset = 0; offset < 8; offset++)
                    {
                        int chr = (interleaved >> shift) & 0b11;
                        shift -= 2;
                        int paletteIndex = attrByte | chr;
                        if (paletteIndex % 4 == 0) 
                            paletteIndex = 0;
                        int colorIndex = palette[paletteIndex];
                        pixels[pixelIndex + coarseX * 8 + offset] = colorIndex & 0x3F;
                    }
                }

                pixelIndex -= X_PIXELS;
            }

        }
        
    }
}