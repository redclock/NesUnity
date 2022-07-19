namespace NesUnity
{
    public partial class Ppu
    {
        // PPUCTRL($2000) write
        // 7  bit  0
        // ---- ----
        // VPHB SINN
        // |||| ||||
        // |||| ||++- Base nametable address
        // |||| ||    (0 = $2000; 1 = $2400; 2 = $2800; 3 = $2C00)
        // |||| |+--- VRAM address increment per CPU read/write of PPUDATA
        // |||| |     (0: add 1, going across; 1: add 32, going down)
        // |||| +---- Sprite pattern table address for 8x8 sprites
        // ||||       (0: $0000; 1: $1000; ignored in 8x16 mode)
        // |||+------ Background pattern table address (0: $0000; 1: $1000)
        // ||+------- Sprite size (0: 8x8 pixels; 1: 8x16 pixels – see PPU OAM#Byte 1)
        // |+-------- PPU master/slave select
        // |          (0: read backdrop from EXT pins; 1: output color on EXT pins)
        // +--------- Generate an NMI at the start of the
        // vertical blanking interval (0: off; 1: on)
        public struct PpuCtrlReg
        {
            public int BaseNameTableAddress;
            public int VRamIncrement;
            public int SpriteChrAddress;
            public int BackgroundChrAddress;
            public int SpritesSize;
            public bool IsMaster;
            public bool NmiEnabled;

            public void FromByte(byte value)
            {
                BaseNameTableAddress    = (value & 0b00000011) * 0x400 + 0x2000;
                VRamIncrement           = (value & 0b00000100) > 0 ? 32 : 1;
                SpriteChrAddress        = (value & 0b00001000) > 0 ? 0x1000 : 0x0000;
                BackgroundChrAddress    = (value & 0b00010000) > 0 ? 0x1000 : 0x0000;
                SpritesSize             = (value & 0b00100000) > 0 ? 16 : 8;
                IsMaster                = (value & 0b01000000) > 0;
                NmiEnabled              = (value & 0x10000000) > 0;
            }
        }

        public PpuCtrlReg PpuCtrl;
        
        // PPUMASK($2001) write
        // 7  bit  0
        // ---- ----
        // BGRs bMmG
        // |||| ||||
        // |||| |||+- Greyscale (0: normal color, 1: produce a greyscale display)
        // |||| ||+-- 1: Show background in leftmost 8 pixels of screen, 0: Hide
        // |||| |+--- 1: Show sprites in leftmost 8 pixels of screen, 0: Hide
        // |||| +---- 1: Show background
        // |||+------ 1: Show sprites
        // ||+------- Emphasize red (green on PAL/Dendy)
        // |+-------- Emphasize green (red on PAL/Dendy)
        // +--------- Emphasize blue
        // vertical blanking interval (0: off; 1: on)
        public struct PpuMaskReg
        {
            public bool Greyscale;
            public bool ShowLeft8Background;
            public bool ShowLeft8Sprite;
            public bool ShowBackground;
            public bool ShowSprites;
            public int EmphasizeColor;

            public void FromByte(byte value)
            {
                Greyscale               = (value & 0b00000001) > 0;
                ShowLeft8Background     = (value & 0b00000010) > 0;
                ShowLeft8Sprite         = (value & 0b00000100) > 0;
                ShowBackground          = (value & 0b00001000) > 0;
                ShowSprites             = (value & 0b00010000) > 0;
                EmphasizeColor          = (value & 0b11000000) >> 6;
            }
        }

        public PpuMaskReg PpuMask;

        // PPUSTATUS($2002) read
        // 7  bit  0
        // ---- ----
        // VSO. ....
        // |||| ||||
        // |||+-++++- PPU open bus. Returns stale PPU bus contents.
        // ||+------- Sprite overflow. The intent was for this flag to be set
        // ||         whenever more than eight sprites appear on a scanline, but a
        // ||         hardware bug causes the actual behavior to be more complicated
        // ||         and generate false positives as well as false negatives; see
        // ||         PPU sprite evaluation. This flag is set during sprite
        // ||         evaluation and cleared at dot 1 (the second dot) of the
        // ||         pre-render line.
        // |+-------- Sprite 0 Hit.  Set when a nonzero pixel of sprite 0 overlaps
        // |          a nonzero background pixel; cleared at dot 1 of the pre-render
        // |          line.  Used for raster timing.
        // +--------- Vertical blank has started (0: not in vblank; 1: in vblank).
        //            Set at dot 1 of line 241 (the line *after* the post-render
        //            line); cleared after reading $2002 and at dot 1 of the
        //            pre-render line.        // vertical blanking interval (0: off; 1: on)
        public struct PpuStatusReg
        {
            public byte OpenBus;
            public bool SpriteOverflow;
            public bool Sprite0Hit;
            public bool VBlank;

            public byte ToByte()
            {
                int s = OpenBus & 0x1F;
                s |= (SpriteOverflow ? 1 : 0) << 5;
                s |= (Sprite0Hit ? 1 : 0) << 6;
                s |= (VBlank ? 1 : 0) << 6;
                return (byte)s;
            }
        }

        public PpuStatusReg PpuStatus;
        
        // OAM address ($2003) > write
        public byte OamAddress;
    }
}