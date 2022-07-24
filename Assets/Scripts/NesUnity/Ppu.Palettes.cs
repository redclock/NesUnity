using UnityEngine;

namespace NesUnity
{
    public partial class Ppu
    {
        private static Color32 ToColor(uint color)
        {
            return new Color32((byte) ((color >> 24) & 0xFF),
                (byte) ((color >> 16) & 0xFF),
                (byte) ((color >> 8) & 0xFF), 255);
        }

        public static readonly Color32[] rgbaPalette =
        {
            ToColor(0x7C7C7CFF), ToColor(0x0000FCFF), ToColor(0x0000BCFF), ToColor(0x4428BCFF),
            ToColor(0x940084FF), ToColor(0xA80020FF), ToColor(0xA81000FF), ToColor(0x881400FF),
            ToColor(0x503000FF), ToColor(0x007800FF), ToColor(0x006800FF), ToColor(0x005800FF),
            ToColor(0x004058FF), ToColor(0x000000FF), ToColor(0x000000FF), ToColor(0x000000FF),
            ToColor(0xBCBCBCFF), ToColor(0x0078F8FF), ToColor(0x0058F8FF), ToColor(0x6844FCFF),
            ToColor(0xD800CCFF), ToColor(0xE40058FF), ToColor(0xF83800FF), ToColor(0xE45C10FF),
            ToColor(0xAC7C00FF), ToColor(0x00B800FF), ToColor(0x00A800FF), ToColor(0x00A844FF),
            ToColor(0x008888FF), ToColor(0x000000FF), ToColor(0x000000FF), ToColor(0x000000FF),
            ToColor(0xF8F8F8FF), ToColor(0x3CBCFCFF), ToColor(0x6888FCFF), ToColor(0x9878F8FF),
            ToColor(0xF878F8FF), ToColor(0xF85898FF), ToColor(0xF87858FF), ToColor(0xFCA044FF),
            ToColor(0xF8B800FF), ToColor(0xB8F818FF), ToColor(0x58D854FF), ToColor(0x58F898FF),
            ToColor(0x00E8D8FF), ToColor(0x787878FF), ToColor(0x000000FF), ToColor(0x000000FF),
            ToColor(0xFCFCFCFF), ToColor(0xA4E4FCFF), ToColor(0xB8B8F8FF), ToColor(0xD8B8F8FF),
            ToColor(0xF8B8F8FF), ToColor(0xF8A4C0FF), ToColor(0xF0D0B0FF), ToColor(0xFCE0A8FF),
            ToColor(0xF8D878FF), ToColor(0xD8F878FF), ToColor(0xB8F8B8FF), ToColor(0xB8F8D8FF),
            ToColor(0x00FCFCFF), ToColor(0xF8D8F8FF), ToColor(0x000000FF), ToColor(0x000000FF)
        };
    }
}