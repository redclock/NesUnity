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
        private bool _nmiRaisedThisVblank;
        private bool _frameReady;
        private bool _oddFrame;
        private readonly bool[] _backgroundOpaque = new bool[X_PIXELS];
        private readonly bool[] _spriteWritten = new bool[X_PIXELS];
        private readonly int[] _lineAddresses = new int[Y_PIXELS];
        private readonly int[] _lineFineX = new int[Y_PIXELS];

        public int TempAddress => _tempAddress;
        public int CurrentAddress => _ppuAddress;
        public int RenderAddress => _ppuAddress;
        public int Scanline => _currentY;
        public int Dot => _currentX;
        public bool FrameReady => _frameReady;

        // Palette indexes, stored bottom row first for Unity Texture2D.
        public int[] pixels = new int[Y_PIXELS * X_PIXELS];
        public int[] FrameBuffer => pixels;

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
            if (_currentY == 0 && _currentX == 0)
                _frameReady = false;

            if (_currentY >= 0 && _currentY < Y_PIXELS && _currentX == 0)
            {
                _lineAddresses[_currentY] = _ppuAddress & 0x7FFF;
                _lineFineX[_currentY] = _scrollFineX;
                RenderScanline(_currentY);
            }

            if (_currentY == 241 && _currentX == 1)
            {
                PpuStatus.VBlank = true;
                _frameReady = true;
                _nesSys.isEndScreen = true;
                if (!_nmiRaisedThisVblank && PpuCtrl.NmiEnabled)
                {
                    _nmiRaisedThisVblank = true;
                    _nesSys.cpu.TriggerInterrupt(Interrupt.Nmi);
                }
            }

            if (_currentY == 261 && _currentX == 1)
            {
                PpuStatus.VBlank = false;
                PpuStatus.Sprite0Hit = false;
                PpuStatus.SpriteOverflow = false;
                _nmiRaisedThisVblank = false;
            }

            if (RenderingEnabled)
            {
                if ((_currentY < Y_PIXELS || _currentY == 261) &&
                    _currentX > 0 && _currentX <= 256 && (_currentX & 7) == 0)
                    IncrementHorizontal();

                if (_currentX == 256 && (_currentY < Y_PIXELS || _currentY == 261))
                    IncrementVertical();

                if (_currentX == 257 && (_currentY < Y_PIXELS || _currentY == 261))
                    CopyHorizontalBits();

                if (_currentY == 261 && _currentX >= 280 && _currentX <= 304)
                    CopyVerticalBits();
            }

            // NTSC PPU skips the last pre-render dot on odd frames when
            // rendering is enabled, yielding a 89342-dot frame.
            if (_currentY == 261 && _currentX == X_CYCLES - 2 && _oddFrame && RenderingEnabled)
            {
                _currentX = 0;
                _currentY = 0;
                _oddFrame = false;
                return;
            }

            _currentX++;
            if (_currentX == X_CYCLES)
            {
                _currentX = 0;
                _currentY++;
                if (_currentY == Y_SCANLINES)
                {
                    _currentY = 0;
                    _oddFrame = !_oddFrame;
                }
            }
        }

        private bool RenderingEnabled => PpuMask.ShowBackground || PpuMask.ShowSprites;

        private void IncrementHorizontal()
        {
            if ((_ppuAddress & 0x001F) == 31)
            {
                _ppuAddress &= ~0x001F;
                _ppuAddress ^= 0x0400;
            }
            else
            {
                _ppuAddress++;
            }
        }

        private void IncrementVertical()
        {
            if ((_ppuAddress & 0x7000) != 0x7000)
            {
                _ppuAddress += 0x1000;
                return;
            }

            _ppuAddress &= ~0x7000;
            int coarseY = (_ppuAddress >> 5) & 0x1F;
            if (coarseY == 29)
            {
                coarseY = 0;
                _ppuAddress ^= 0x0800;
            }
            else if (coarseY == 31)
            {
                coarseY = 0;
            }
            else
            {
                coarseY++;
            }

            _ppuAddress = (_ppuAddress & ~0x03E0) | (coarseY << 5);
        }

        private void CopyHorizontalBits()
        {
            _ppuAddress = (_ppuAddress & ~0x041F) | (_tempAddress & 0x041F);
        }

        private void CopyVerticalBits()
        {
            _ppuAddress = (_ppuAddress & ~0x7BE0) | (_tempAddress & 0x7BE0);
        }

        private void RenderFrame()
        {
            for (int scanline = 0; scanline < Y_PIXELS; scanline++)
                RenderScanline(scanline);
        }

        public void RenderFrameForTest()
        {
            RenderFrame();
        }

        private void RenderScanline(int scanline)
        {
            int backdrop = ReadPaletteColor(0);
            for (int x = 0; x < X_PIXELS; x++)
            {
                _backgroundOpaque[x] = false;
                _spriteWritten[x] = false;
                SetPixel(x, scanline, backdrop);
            }

            if (PpuMask.ShowBackground)
            {
                RenderBackgroundLine(scanline, _lineAddresses[scanline], _lineFineX[scanline], true);
            }

            if (PpuMask.ShowSprites)
                RenderSprites(scanline);
        }

        private void RenderBackgroundLine(int scanline, int address, int fineXScroll, bool applyMask)
        {
            int scrollX = (GetCoarseX(address) * 8) + fineXScroll;
            int scrollY = (((address >> 5) & 0x1F) * 8) + ((address >> 12) & 0x07);
            int nametableX = (address >> 10) & 1;
            int nametableY = (address >> 11) & 1;

            for (int x = 0; x < X_PIXELS; x++)
            {
                if (applyMask && x < 8 && !PpuMask.ShowLeft8Background)
                    continue;

                int worldX = scrollX + x;
                int worldY = scrollY;
                int tileColumn = worldX / 8;
                int tileRow = worldY / 8;
                int tileX = tileColumn & 0x1F;
                int tileY = tileRow % 30;
                int ntX = (nametableX + tileColumn / 32) & 1;
                int ntY = (nametableY + tileRow / 30) & 1;
                int nametable = ntY * 2 + ntX;
                int nameAddress = 0x2000 + nametable * 0x400 + tileY * 32 + tileX;
                int tile = _memory.ReadByte(nameAddress);
                int fineX = worldX & 7;
                int fineY = worldY & 7;
                int patternAddress = PpuCtrl.BackgroundChrAddress + tile * 16 + fineY;
                int bit = 7 - fineX;
                int chr = ((_memory.ReadByte(patternAddress) >> bit) & 1) |
                          (((_memory.ReadByte(patternAddress + 8) >> bit) & 1) << 1);
                if (chr == 0)
                    continue;

                int attributeAddress = 0x2000 + nametable * 0x400 + 0x3C0 +
                                       (tileY / 4) * 8 + tileX / 4;
                int attribute = _memory.ReadByte(attributeAddress);
                int quadrantShift = ((tileY & 2) != 0 ? 4 : 0) + ((tileX & 2) != 0 ? 2 : 0);
                int palette = ((attribute >> quadrantShift) & 0x03) * 4 + chr;
                _backgroundOpaque[x] = true;
                SetPixel(x, scanline, ReadPaletteColor(palette));
            }
        }

        private void RenderSprites(int scanline)
        {
            int height = PpuCtrl.SpritesSize;
            int spritesOnLine = 0;

            for (int sprite = 0; sprite < 64; sprite++)
            {
                int offset = sprite * 4;
                int spriteY = _oam[offset];
                int row = scanline - spriteY - 1;
                if (row < 0 || row >= height)
                    continue;

                if (spritesOnLine >= 8)
                {
                    PpuStatus.SpriteOverflow = true;
                    continue;
                }

                spritesOnLine++;
                int tile = _oam[offset + 1];
                int attributes = _oam[offset + 2];
                int spriteX = _oam[offset + 3];
                bool flipHorizontal = (attributes & 0x40) != 0;
                bool flipVertical = (attributes & 0x80) != 0;
                if (flipVertical)
                    row = height - 1 - row;

                int patternAddress;
                if (height == 16)
                {
                    int bank = (tile & 1) * 0x1000;
                    int baseTile = tile & 0xFE;
                    if (row >= 8)
                    {
                        baseTile++;
                        row -= 8;
                    }
                    patternAddress = bank + baseTile * 16 + row;
                }
                else
                {
                    patternAddress = PpuCtrl.SpriteChrAddress + tile * 16 + row;
                }

                byte low = _memory.ReadByte(patternAddress);
                byte high = _memory.ReadByte(patternAddress + 8);
                for (int column = 0; column < 8; column++)
                {
                    int x = spriteX + column;
                    if (x < 0 || x >= X_PIXELS || _spriteWritten[x])
                        continue;
                    if (x < 8 && !PpuMask.ShowLeft8Sprite)
                        continue;

                    int sourceColumn = flipHorizontal ? column : 7 - column;
                    int chr = ((low >> sourceColumn) & 1) | (((high >> sourceColumn) & 1) << 1);
                    if (chr == 0)
                        continue;

                    if (sprite == 0 && x > 0 && x < 255 && _backgroundOpaque[x] && PpuMask.ShowBackground)
                        PpuStatus.Sprite0Hit = true;

                    bool behindBackground = (attributes & 0x20) != 0;
                    if (!behindBackground || !_backgroundOpaque[x])
                    {
                        int palette = 0x10 + (attributes & 0x03) * 4 + chr;
                        SetPixel(x, scanline, ReadPaletteColor(palette));
                    }
                    _spriteWritten[x] = true;
                }
            }
        }

        private int ReadPaletteColor(int paletteIndex)
        {
            return _memory.ReadByte(0x3F00 + paletteIndex) & 0x3F;
        }

        private void SetPixel(int x, int y, int color)
        {
            pixels[(Y_PIXELS - 1 - y) * X_PIXELS + x] = color & 0x3F;
        }

        // Compatibility helper used by the existing static NameTable tests.
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
                    int attrBit = ((coarseY & 0b10) << 1) | (coarseX & 0b10);
                    int attrByte = ((attr >> attrBit) & 0b11) << 2;
                    int interleaved = Utils.Interleave8To16(byte1) | (Utils.Interleave8To16(byte2) << 1);
                    int shift = 14;
                    for (int offset = 0; offset < 8; offset++)
                    {
                        int chr = (interleaved >> shift) & 0b11;
                        shift -= 2;
                        int paletteIndex = attrByte | chr;
                        if (paletteIndex % 4 == 0)
                            paletteIndex = 0;
                        pixels[pixelIndex + coarseX * 8 + offset] = palette[paletteIndex] & 0x3F;
                    }
                }

                pixelIndex -= X_PIXELS;
            }
        }
    }
}
