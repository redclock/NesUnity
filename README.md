# NesUnity - In Progress

An NES emulator based on Unity3D for personally study, uncompleted.

The current runtime targets desktop NTSC NROM games. CPU instruction execution and
the implemented PPU/controller paths have automated regression coverage, but this
is not yet a cycle-accurate or complete NES implementation.

## Current Status:

Scanline-level background and sprite rendering, keyboard controller input, and NROM games:

![screen.png](screen.png)

## Progress & planning:

1. Rom file: 
  * iNes (.nes file)  ✅  
2. CPU
  * Addressing modes ✅
  * Memory mapping ✅
  * Mappers 🔲
    * NROM ✅
    * Other 🔲
  * 6502 instructions ✅
    * official ✅
    * unofficial ✅  [Ref](http://www.oxyron.de/html/opcodes02.html)
    * Tested with nestest.nes log ✅
    * Disassmebly 🔲
3. PPU
  * Memeory mappings
    * Register ✅
    * IO ✅
    * Palette ✅ 
  * Backgrounds ✅ (scanline-level)
    * NMI interruption ✅
    * PatternTable ✅
    * NameTable + AttributeTable ✅
    * Scrolling ✅
  * Sprites
    * OAM ✅
    * DMA ✅
    * Priority ✅
    * Sprite0 hit ✅
    * Overscan 🔲
  4. Input ✅ (keyboard controller 1)
 5. APU
  * Pulse
  * Triangle
  * Noise
  * DMC

## Accuracy boundaries

Implemented: NTSC 2C02 base frame timing, VBlank/NMI, PPU `v/t/x/w` scroll address
updates, NameTable mirroring, NROM, OAM DMA, keyboard controller 1, background and
sprite composition.

Not yet implemented: APU channels, additional mappers, PAL/Dendy timing, exact
per-dot PPU bus behavior, sprite evaluation hardware bugs, and full open-bus behavior.
