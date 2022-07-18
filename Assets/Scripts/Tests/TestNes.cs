using System.IO;
using NesUnity;
using NUnit.Framework;
using UnityEngine;

public class TestNes
{
    [Test]
    public void TestNesChrTable()
    {
        byte[] bytes = File.ReadAllBytes( Application.streamingAssetsPath + "/smb.nes");
        var nes = new NesRom();
        Assert.True(nes.ReadFromBytes(bytes));
        var texture = CreatePatternTexture(nes.chrPatternTable);
        byte[] textureBytes = texture.EncodeToPNG();
        File.WriteAllBytes("Assets/chr.png", textureBytes);
    }

    private Texture2D CreatePatternTexture(PatternTable patternTable)
    {
        
        byte[] pattern = patternTable.GetPatternBuffer();
        Color32[] buffer = new Color32[128 * 128];
        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false, false);

        for (int iPat = 0; iPat < 256; iPat++)
        {
            int patternOffset = iPat * 64;
            
            for (int iPixel = 0; iPixel < 64; iPixel++)
            {
                byte p = pattern[patternOffset + iPixel];
                byte c = (byte) (p * 64);
                Color32 color = new Color32(c, c, c, 255);
                int row = (15 - iPat / 16) * 8 + iPixel / 8;
                int col = iPat % 16 * 8 + iPixel % 8;
                buffer[row * 128 + col] = color;
            }

        }
        
        texture.SetPixels32(buffer);

        return texture;
    }
}
