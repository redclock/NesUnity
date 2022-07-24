
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
        
    }
}