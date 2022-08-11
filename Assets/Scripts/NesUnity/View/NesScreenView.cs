using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace NesUnity
{
    [RequireComponent(typeof(RawImage))]
    public class NesScreenView: MonoBehaviour
    {
        [SerializeField] private string _fileName;
        private Texture2D[] _textures = new Texture2D[2];
        private int _currentIndex;

        private Texture2D currentTexture => _textures[_currentIndex];
        private Texture2D currentBackTexture => _textures[1 - _currentIndex];

        private RawImage _rawImage;
        
        private Nes _nes;
        public static readonly uint[] rgbaPalette =
        {
            0xFF7C7C7C, 0xFFFC0000, 0xFFBC0000, 0xFFBC2844,
            0xFF840094, 0xFF2000A8, 0xFF0010A8, 0xFF001488,
            0xFF003050, 0xFF007800, 0xFF006800, 0xFF005800,
            0xFF584000, 0xFF000000, 0xFF000000, 0xFF000000,
            0xFFBCBCBC, 0xFFF87800, 0xFFF85800, 0xFFFC4468,
            0xFFCC00D8, 0xFF5800E4, 0xFF0038F8, 0xFF105CE4,
            0xFF007CAC, 0xFF00B800, 0xFF00A800, 0xFF44A800,
            0xFF888800, 0xFF000000, 0xFF000000, 0xFF000000,
            0xFFF8F8F8, 0xFFFCBC3C, 0xFFFC8868, 0xFFF87898,
            0xFFF878F8, 0xFF9858F8, 0xFF5878F8, 0xFF44A0FC,
            0xFF00B8F8, 0xFF18F8B8, 0xFF54D858, 0xFF98F858,
            0xFFD8E800, 0xFF787878, 0xFF000000, 0xFF000000,
            0xFFFCFCFC, 0xFFFCE4A4, 0xFFF8B8B8, 0xFFF8B8D8,
            0xFFF8B8F8, 0xFFC0A4F8, 0xFFB0D0F0, 0xFFA8E0FC,
            0xFF78D8F8, 0xFF78F8D8, 0xFFB8F8B8, 0xFFD8F8B8,
            0xFFFCFC00, 0xFFF8D8F8, 0xFF000000, 0xFF000000
        };

        private uint[] _pixels = new uint[Ppu.X_PIXELS * Ppu.Y_PIXELS];

        private void Awake()
        {
            _textures[0] = new Texture2D(Ppu.X_PIXELS, Ppu.Y_PIXELS, TextureFormat.RGBA32, false);
            _textures[1] = new Texture2D(Ppu.X_PIXELS, Ppu.Y_PIXELS, TextureFormat.RGBA32, false);
            _currentIndex = 0;
            _rawImage = GetComponent<RawImage>();
            _nes = new Nes();
        }

        private void OnDestroy()
        {
            Destroy(_textures[0]);
            Destroy(_textures[1]);
        }

        private void Start()
        {
            byte[] bytes = File.ReadAllBytes( Application.streamingAssetsPath + "/" + _fileName);
            _nes.PowerOn(bytes);
        }

        private void FixedUpdate()
        {
            do
            {
                _nes.Tick();
            } while (!_nes.isEndScreen);
            _nes.ppu.GenBackground(0);
            //Debug.Log(_nes.cpu.TotalCycle);
            UploadTexture();
        }

        private void UploadTexture()
        {
            int[] ppuPixels = _nes.ppu.pixels;
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = rgbaPalette[ppuPixels[i]];
            currentBackTexture.SetPixelData(_pixels, 0);
            currentBackTexture.Apply(false);
            _currentIndex = 1 - _currentIndex;
            _rawImage.texture = currentTexture;
        }
    }
}