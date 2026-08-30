using System;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace NesUnity
{
    [RequireComponent(typeof(RawImage), typeof(AudioSource))]
    public class NesScreenView: MonoBehaviour
    {
        [SerializeField] private string _fileName;
        private Texture2D[] _textures = new Texture2D[2];
        private int _currentIndex;

        private Texture2D currentTexture => _textures[_currentIndex];
        private Texture2D currentBackTexture => _textures[1 - _currentIndex];

        private RawImage _rawImage;
        private AudioSource _audioSource;
        private AudioClip _audioClip;
        private int _audioReadCount;
        private int _audioNonZeroReadCount;
        private int _previousVSyncCount;
        private int _previousTargetFrameRate;
        private bool _framePacingConfigured;
        
        private Nes _nes;
        private bool _running;
        private bool _audioStartPending;
        private float _frameAccumulator;
        private const float FrameTime = 29780.5f / Apu.CpuClockHz;
        private const int AudioPrebufferFrames = 7;
        private const int NormalFrameBudget = 1;
        private const int MaxFrameDebt = 1;
        public bool IsRunning => _running;
        public bool IsAudioPlaying => _audioSource != null && _audioSource.isPlaying;
        public int AudioReadCount => Volatile.Read(ref _audioReadCount);
        public int AudioNonZeroReadCount => Volatile.Read(ref _audioNonZeroReadCount);
        public int AudioPendingSampleCount => _nes == null ? 0 : _nes.apu.PendingSampleCount;
        public int AudioOutputSampleRate => _nes == null ? 0 : _nes.apu.OutputSampleRate;
        public int AudioUnderrunCount => _nes == null ? 0 : _nes.apu.AudioUnderrunCount;
        public int AudioOverrunCount => _nes == null ? 0 : _nes.apu.AudioOverrunCount;

#if UNITY_EDITOR
        // Editor-only test hook used to verify Unity's audio callback path.
        public void DebugInjectPulseTone()
        {
            if (_nes == null)
                return;
            _nes.cpu.Memory.WriteByte(0x4000, 0xBF);
            _nes.cpu.Memory.WriteByte(0x4002, 0x20);
            _nes.cpu.Memory.WriteByte(0x4003, 0x08);
            _nes.cpu.Memory.WriteByte(0x4015, 0x01);
        }
#endif
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
            ConfigureFramePacing();
            _textures[0] = new Texture2D(Ppu.X_PIXELS, Ppu.Y_PIXELS, TextureFormat.RGBA32, false);
            _textures[1] = new Texture2D(Ppu.X_PIXELS, Ppu.Y_PIXELS, TextureFormat.RGBA32, false);
            _textures[0].filterMode = FilterMode.Point;
            _textures[1].filterMode = FilterMode.Point;
            _textures[0].wrapMode = TextureWrapMode.Clamp;
            _textures[1].wrapMode = TextureWrapMode.Clamp;
            _currentIndex = 0;
            _rawImage = GetComponent<RawImage>();
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;
            _audioSource.spatialBlend = 0f;
            _nes = new Nes();
            ConfigureAudioSettings();
            _nes.apu.SetOutputSampleRate(AudioSettings.outputSampleRate);
            int outputSampleRate = _nes.apu.OutputSampleRate;
            _audioClip = AudioClip.Create(
                "NES APU",
                outputSampleRate,
                1,
                outputSampleRate,
                true,
                OnAudioRead,
                OnAudioSetPosition);
            _audioSource.clip = _audioClip;
        }

        private static void ConfigureAudioSettings()
        {
            AudioConfiguration configuration = AudioSettings.GetConfiguration();
            if (configuration.sampleRate == Apu.SampleRate && configuration.dspBufferSize == 512)
                return;

            configuration.sampleRate = Apu.SampleRate;
            configuration.dspBufferSize = 512;
            AudioSettings.Reset(configuration);
        }

        private void ConfigureFramePacing()
        {
            _previousVSyncCount = QualitySettings.vSyncCount;
            _previousTargetFrameRate = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            _framePacingConfigured = true;
        }

        private void OnDestroy()
        {
            _running = false;
            _audioStartPending = false;
            if (_audioSource != null)
                _audioSource.Stop();
            _nes = null;
            Destroy(_textures[0]);
            Destroy(_textures[1]);
            if (_audioClip != null)
                Destroy(_audioClip);
            if (_framePacingConfigured)
            {
                QualitySettings.vSyncCount = _previousVSyncCount;
                Application.targetFrameRate = _previousTargetFrameRate;
                _framePacingConfigured = false;
            }
        }

        private void Start()
        {
            if (string.IsNullOrWhiteSpace(_fileName))
            {
                Debug.LogError("NES ROM filename is empty.");
                return;
            }

            string path = Path.Combine(Application.streamingAssetsPath, _fileName);
            if (!File.Exists(path))
            {
                Debug.LogError("NES ROM not found: " + path);
                return;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                _running = _nes.PowerOn(bytes);
                if (!_running)
                    Debug.LogError("Unable to load NES ROM: " + _fileName);
                else
                {
                    for (int frame = 0; frame < AudioPrebufferFrames; frame++)
                    {
                        if (_nes.RunFrame())
                            continue;
                        _running = false;
                        Debug.LogError("NES stopped while preparing audio output.");
                        break;
                    }
                    if (_running)
                    {
                        _frameAccumulator = 0f;
                        UploadTexture();
                        _audioStartPending = true;
                    }
                }
            }
            catch (IOException exception)
            {
                Debug.LogError("Unable to read NES ROM: " + exception.Message);
            }
        }

        private void Update()
        {
            if (!_running)
                return;

            UpdateController();
            _frameAccumulator = Mathf.Min(
                _frameAccumulator + Time.unscaledDeltaTime,
                FrameTime * MaxFrameDebt);

            int framesRun = 0;
            if (_frameAccumulator >= FrameTime)
            {
                if (!TryRunFrame())
                    return;
                _frameAccumulator -= FrameTime;
                framesRun = NormalFrameBudget;
                // Never replay accumulated time by running several expensive
                // NES frames in one Unity update. This keeps video pacing
                // continuous when the editor briefly misses its deadline.
                if (_frameAccumulator >= FrameTime)
                    _frameAccumulator = 0f;
            }

            if (framesRun > 0)
                UploadTexture();

            if (_audioStartPending)
            {
                _audioStartPending = false;
                _audioSource.Play();
            }
        }

        private bool TryRunFrame()
        {
            if (_nes.RunFrame())
                return true;

            _running = false;
            if (_audioSource != null)
                _audioSource.Stop();
            Debug.LogError("NES stopped before completing a frame.");
            return false;
        }

        private void UpdateController()
        {
            _nes.Controller1.SetButton(NesController.Button.A, Input.GetKey(KeyCode.Z));
            _nes.Controller1.SetButton(NesController.Button.B, Input.GetKey(KeyCode.X));
            _nes.Controller1.SetButton(NesController.Button.Select, Input.GetKey(KeyCode.RightShift));
            _nes.Controller1.SetButton(NesController.Button.Start,
                Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter));
            _nes.Controller1.SetButton(NesController.Button.Up, Input.GetKey(KeyCode.UpArrow));
            _nes.Controller1.SetButton(NesController.Button.Down, Input.GetKey(KeyCode.DownArrow));
            _nes.Controller1.SetButton(NesController.Button.Left, Input.GetKey(KeyCode.LeftArrow));
            _nes.Controller1.SetButton(NesController.Button.Right, Input.GetKey(KeyCode.RightArrow));
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

        private void OnAudioRead(float[] data)
        {
            Interlocked.Increment(ref _audioReadCount);
            Nes nes = _nes;
            if (nes == null)
            {
                Array.Clear(data, 0, data.Length);
                return;
            }
            nes.apu.FillAudioBuffer(data, 1);
            for (int i = 0; i < data.Length; i++)
            {
                if (Mathf.Abs(data[i]) > 0.0001f)
                {
                    Interlocked.Increment(ref _audioNonZeroReadCount);
                    break;
                }
            }
        }

        private void OnAudioSetPosition(int position)
        {
        }
    }
}
