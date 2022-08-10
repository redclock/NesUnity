
namespace NesUnity
{
    public partial class Ppu
    {
        // yyy NN YYYYY XXXXX
        // ||| || ||||| +++++-- coarse X scroll
        // ||| || +++++-------- coarse Y scroll
        // ||| ++-------------- nametable select
        // +++----------------- fine Y scroll
        private int _ppuAddress;
        private int _tempAddress;
        private int _scrollFineX;

        private int _currentX;
        private int _currentY;
        public int TempAddress => _tempAddress;

        public int[] pixels = new int[Y_PIXELS * X_PIXELS];

        public static int GetCoarseX(int address)
        {
            return address & 0b0000000011111;
        } 

        public static int GetCoarseY(int address)
        {
            return address & 0b0001111100000;
        }

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
                    byte byte1 = _memory.ReadByte(tileAddress);
                    byte byte2 = _memory.ReadByte(tileAddress + 8);

                    int addressAttr = coarseY / 4 * 8 + coarseX / 4;
                    byte attr = vram[addressAttrBase + addressAttr];

                    // left-top: 0, right-top: 2, left-bottom: 4, right-bottom: 6
                    int attrBit = ((coarseY & 0b10) << 1) | (coarseX & 0b10);
                    int attrByte = ((attr >> attrBit) & 0b11) << 2;
                    // interleave every bit to 2 bits
                    int interleaved = Utils.Interleave8To16(byte1) | (Utils.Interleave8To16(byte2) << 1);
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