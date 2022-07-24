namespace NesUnity
{
    public static class Utils
    {
        public static int Interleave8To16(byte b) {
            int x = b;  // x = 0000 0000 abcd efgh
            x = (x | (x << 4)) & 0b0000111100001111; // x = 0000 abcd 0000 efgh
            x = (x | (x << 2)) & 0b0011001100110011; // x = 00ab 00cd 00ef 00gh
            x = (x | (x << 1)) & 0b0101010101010101; // x = 0a0b 0c0d 0e0f 0g0h
            return x;
        }
    }
}