namespace NesUnity
{
    public class PatternTable
    {
        // original bytes
        private byte[] _bytes;
        // converted to 8x8 pattern, each byte a pixel
        private byte[] _patterns;

        public int PatternCount => _patterns.Length / 64;

        public byte[] GetPattern(int index, out int startPos)
        {
            startPos = index * 64;
            return _patterns;
        }

        public byte[] GetPatternBuffer()
        {
            return _patterns;
        }

        public PatternTable(byte[] bytes)
        {
            _bytes = bytes;
            // each pattern 16 bytes
            int patternCount = _bytes.Length / 16;
            _patterns = new byte[patternCount * 64];
            for (int i = 0; i < patternCount; i++)
            {
                ConvertPattern(i);
            }
        }

        private void ConvertPattern(int index)
        {
            int srcOffset = index * 16 + 7;
            int dstOffset = index * 64;
            for (int row = 7; row >= 0; row--)
            {
                for (int col = 7; col >= 0; col--)
                {
                    int mask = 1 << col;
                    byte bit1 = (byte)((_bytes[srcOffset] & mask) >> col);
                    byte bit2 = (byte)((_bytes[srcOffset + 8] & mask) >> col);
                    _patterns[dstOffset++] = (byte)(bit1 + (bit2 << 1));
                }

                srcOffset--;
            }
        }
    }
}