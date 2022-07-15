using NesUnity;
using UnityEngine;

public class TestNes : MonoBehaviour
{
    private void Start()
    {
        var txt = Resources.Load<TextAsset>("nestest.nes");
        var nes = new NesRom();
        nes.ReadFromBytes(txt.bytes);
    }
}
