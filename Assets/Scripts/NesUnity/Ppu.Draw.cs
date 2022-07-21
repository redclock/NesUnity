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

        public int TempAddress => _tempAddress;
    }
}