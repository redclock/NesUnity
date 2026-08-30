using System;
using System.Threading;

namespace NesUnity
{
    public sealed class Apu
    {
        public const int CpuClockHz = 1789773;
        public const int SampleRate = 44100;

        private readonly PulseChannel[] _pulse = { new PulseChannel(true), new PulseChannel(false) };
        private readonly TriangleChannel _triangle = new TriangleChannel();
        private readonly NoiseChannel _noise = new NoiseChannel();
        private readonly DmcChannel _dmc = new DmcChannel();
        private byte _frameCounter;
        private int _frameCounterCycles;
        private bool _halfRateTimerClock;
        private int _outputSampleRate = SampleRate;
        private double _sampleAccumulator;
        private const int SampleBufferCapacity = 8192;
        private readonly float[] _sampleBuffer = new float[SampleBufferCapacity];
        private int _sampleReadPosition;
        private int _sampleWritePosition;
        private int _audioUnderrunCount;
        private int _audioOverrunCount;

        public bool FrameCounter5Step => (_frameCounter & 0x80) != 0;
        public bool IrqInhibit => (_frameCounter & 0x40) != 0;
        public int OutputSampleRate => _outputSampleRate;
        public int AudioUnderrunCount => Volatile.Read(ref _audioUnderrunCount);
        public int AudioOverrunCount => Volatile.Read(ref _audioOverrunCount);
        public int PendingSampleCount
        {
            get
            {
                int count = Volatile.Read(ref _sampleWritePosition) - Volatile.Read(ref _sampleReadPosition);
                return Math.Max(0, Math.Min(SampleBufferCapacity, count));
            }
        }

        public void Reset()
        {
            _frameCounter = 0;
            _frameCounterCycles = 0;
            _halfRateTimerClock = false;
            _sampleAccumulator = 0;
            Volatile.Write(ref _sampleReadPosition, 0);
            Volatile.Write(ref _sampleWritePosition, 0);
            Volatile.Write(ref _audioUnderrunCount, 0);
            Volatile.Write(ref _audioOverrunCount, 0);
            _pulse[0].Reset();
            _pulse[1].Reset();
            _triangle.Reset();
            _noise.Reset();
            _dmc.Reset();
        }

        public void SetOutputSampleRate(int sampleRate)
        {
            _outputSampleRate = sampleRate > 0 ? sampleRate : SampleRate;
            _sampleAccumulator = 0;
        }

        public byte ReadRegister(int address)
        {
            if (address == 0x4015)
            {
                byte status = 0;
                if (_pulse[0].LengthCounter > 0) status |= 1;
                if (_pulse[1].LengthCounter > 0) status |= 2;
                if (_triangle.LengthCounter > 0) status |= 4;
                if (_noise.LengthCounter > 0) status |= 8;
                // Bit 4 is the DMC active flag. The first APU milestone keeps
                // DMC playback disabled, but the register block is retained so
                // games can safely configure it without affecting execution.
                return status;
            }

            return 0;
        }

        public void WriteRegister(int address, byte value)
        {
            if (address == 0x4015)
            {
                _pulse[0].Enabled = (value & 1) != 0;
                _pulse[1].Enabled = (value & 2) != 0;
                _triangle.Enabled = (value & 4) != 0;
                _noise.Enabled = (value & 8) != 0;
                _dmc.Enabled = (value & 0x10) != 0;
                if (!_pulse[0].Enabled) _pulse[0].LengthCounter = 0;
                if (!_pulse[1].Enabled) _pulse[1].LengthCounter = 0;
                if (!_triangle.Enabled) _triangle.LengthCounter = 0;
                if (!_noise.Enabled) _noise.LengthCounter = 0;
                return;
            }

            if (address == 0x4017)
            {
                _frameCounter = value;
                _frameCounterCycles = 0;
                if (FrameCounter5Step)
                {
                    ClockQuarterFrame();
                    ClockHalfFrame();
                }
                return;
            }

            if (address >= 0x4000 && address <= 0x4003)
                _pulse[0].WriteRegister(address & 3, value);
            else if (address >= 0x4004 && address <= 0x4007)
                _pulse[1].WriteRegister(address & 3, value);
            else if (address >= 0x4008 && address <= 0x400B)
                _triangle.WriteRegister(address & 3, value);
            else if (address >= 0x400C && address <= 0x400F)
                _noise.WriteRegister(address & 3, value);
            else if (address >= 0x4010 && address <= 0x4013)
                _dmc.WriteRegister(address & 3, value);
        }

        public void Tick()
        {
            _triangle.TickTimer();
            _halfRateTimerClock = !_halfRateTimerClock;
            if (_halfRateTimerClock)
            {
                _pulse[0].TickTimer();
                _pulse[1].TickTimer();
                _noise.TickTimer();
            }

            _frameCounterCycles++;
            if (FrameCounter5Step)
            {
                // 5-step sequence: quarter at 3729/7457/11186/18641,
                // half-frame at 7457 and 18641; 14915 has no clock.
                if (_frameCounterCycles == 3729 || _frameCounterCycles == 11186)
                    ClockQuarterFrame();
                else if (_frameCounterCycles == 7457)
                {
                    ClockQuarterFrame();
                    ClockHalfFrame();
                }
                else if (_frameCounterCycles == 18641)
                {
                    ClockQuarterFrame();
                    ClockHalfFrame();
                    _frameCounterCycles = 0;
                }
            }
            else
            {
                // 4-step sequence: quarter at 3729/7457/11186/14915,
                // half-frame at 7457 and 14915, then restart.
                if (_frameCounterCycles == 3729 || _frameCounterCycles == 11186)
                    ClockQuarterFrame();
                else if (_frameCounterCycles == 7457 || _frameCounterCycles == 14915)
                {
                    ClockQuarterFrame();
                    ClockHalfFrame();
                    if (_frameCounterCycles == 14915)
                        _frameCounterCycles = 0;
                }
            }

            _sampleAccumulator += _outputSampleRate;
            if (_sampleAccumulator >= CpuClockHz)
            {
                _sampleAccumulator -= CpuClockHz;
                PushSample(GetMixedSample());
            }
        }

        public int DrainSamples(float[] destination)
        {
            if (destination == null || destination.Length == 0)
                return 0;
            int read = Volatile.Read(ref _sampleReadPosition);
            int write = Volatile.Read(ref _sampleWritePosition);
            int count = Math.Min(destination.Length, Math.Max(0, write - read));
            for (int i = 0; i < count; i++)
                destination[i] = _sampleBuffer[(read + i) & (SampleBufferCapacity - 1)];
            Volatile.Write(ref _sampleReadPosition, read + count);
            return count;
        }

        /// <summary>
        /// Fills Unity's interleaved audio callback buffer. The emulator thread
        /// is the sole producer and the audio thread is the sole consumer, so
        /// this path uses volatile positions instead of a blocking lock.
        /// </summary>
        public void FillAudioBuffer(float[] destination, int channels)
        {
            if (destination == null || destination.Length == 0)
                return;
            if (channels < 1)
                channels = 1;

            int frames = destination.Length / channels;
            int read = Volatile.Read(ref _sampleReadPosition);
            int write = Volatile.Read(ref _sampleWritePosition);
            int available = Math.Min(frames, Math.Max(0, write - read));
            if (available < frames)
                Interlocked.Increment(ref _audioUnderrunCount);
            for (int frame = 0; frame < frames; frame++)
            {
                float sample = frame < available
                    ? _sampleBuffer[(read + frame) & (SampleBufferCapacity - 1)]
                    : 0f;
                int offset = frame * channels;
                for (int channel = 0; channel < channels; channel++)
                    destination[offset + channel] = sample;
            }
            Volatile.Write(ref _sampleReadPosition, read + available);
        }

        private void PushSample(float sample)
        {
            int write = _sampleWritePosition;
            int read = Volatile.Read(ref _sampleReadPosition);
            if (write - read >= SampleBufferCapacity)
            {
                // Only the audio thread advances the read position. Dropping a
                // new sample here preserves the lock-free SPSC ownership model.
                Interlocked.Increment(ref _audioOverrunCount);
                return;
            }
            _sampleBuffer[write & (SampleBufferCapacity - 1)] = sample;
            Volatile.Write(ref _sampleWritePosition, write + 1);
        }

        private void ClockQuarterFrame()
        {
            _pulse[0].ClockEnvelope();
            _pulse[1].ClockEnvelope();
            _triangle.ClockLinearCounter();
            _noise.ClockEnvelope();
        }

        private void ClockHalfFrame()
        {
            _pulse[0].ClockLengthAndSweep();
            _pulse[1].ClockLengthAndSweep();
            _triangle.ClockLengthCounter();
            _noise.ClockLengthCounter();
        }

        private float GetMixedSample()
        {
            int p1 = _pulse[0].Output;
            int p2 = _pulse[1].Output;
            int pulseSum = p1 + p2;
            float pulse = pulseSum == 0 ? 0f : 95.88f / (8128f / pulseSum + 100f);
            float tndInput = _triangle.Output / 8227f + _noise.Output / 12241f;
            float tnd = tndInput == 0 ? 0f : 159.79f / (1f / tndInput + 100f);
            // The NES mixer outputs roughly 0..0.5 in normalized float units
            // with the exact nonlinear formula. Apply a fixed master gain so
            // Unity's AudioSource produces a clearly audible desktop signal.
            return Math.Max(-1f, Math.Min(1f, (pulse + tnd) * 2f));
        }
    }

    internal sealed class PulseChannel
    {
        private static readonly int[] LengthTable =
        {
            10, 254, 20, 2, 40, 4, 80, 6,
            160, 8, 60, 10, 14, 12, 26, 14,
            12, 16, 24, 18, 48, 20, 96, 22,
            192, 24, 72, 26, 16, 28, 32, 30
        };

        private readonly byte[] _registers = new byte[4];
        private int _timer;
        private int _timerReload;
        private int _sequence;
        private int _envelopeDivider;
        private int _envelopeDecay;
        private bool _envelopeStart;
        private int _sweepDivider;
        private bool _sweepReload;
        private readonly bool _complementSweep;

        public PulseChannel(bool complementSweep)
        {
            _complementSweep = complementSweep;
        }

        public bool Enabled;
        public int LengthCounter;
        public int Output
        {
            get
            {
                if (!Enabled || LengthCounter == 0 || _timerReload < 8)
                    return 0;
                int duty = (_registers[0] >> 6) & 3;
                int bit = (DutyTable[duty] >> _sequence) & 1;
                int volume = (_registers[0] & 0x10) != 0 ? (_registers[0] & 0x0F) : _envelopeDecay;
                return bit * volume;
            }
        }

        private static readonly int[] DutyTable = { 0x01, 0x03, 0x0F, 0xFC };

        public void Reset()
        {
            Array.Clear(_registers, 0, _registers.Length);
            _timer = 0;
            _timerReload = 0;
            _sequence = 0;
            _envelopeDivider = 0;
            _envelopeDecay = 0;
            _envelopeStart = false;
            _sweepDivider = 0;
            _sweepReload = false;
            Enabled = false;
            LengthCounter = 0;
        }

        public void WriteRegister(int index, byte value)
        {
            _registers[index] = value;
            switch (index)
            {
                case 0:
                    _envelopeStart = true;
                    break;
                case 1:
                    _sweepReload = true;
                    break;
                case 2:
                    _timerReload = (_timerReload & 0x700) | value;
                    break;
                case 3:
                    _timerReload = ((_registers[3] & 7) << 8) | (_registers[2] & 0xFF);
                    LengthCounter = LengthTable[(value >> 3) & 0x1F];
                    _sequence = 0;
                    _envelopeStart = true;
                    break;
            }
        }

        public void TickTimer()
        {
            if (_timer == 0)
            {
                _timer = _timerReload;
                _sequence = (_sequence + 1) & 7;
            }
            else
            {
                _timer--;
            }
        }

        public void ClockEnvelope()
        {
            int period = _registers[0] & 0x0F;
            if (_envelopeStart)
            {
                _envelopeStart = false;
                _envelopeDecay = 15;
                _envelopeDivider = period;
            }
            else if (_envelopeDivider > 0)
            {
                _envelopeDivider--;
            }
            else
            {
                _envelopeDivider = period;
                if (_envelopeDecay > 0)
                    _envelopeDecay--;
                else if ((_registers[0] & 0x20) != 0)
                    _envelopeDecay = 15;
            }
        }

        public void ClockLengthAndSweep()
        {
            if ((_registers[0] & 0x20) == 0 && LengthCounter > 0)
                LengthCounter--;

            int period = ((_registers[1] >> 4) & 7);
            if (_sweepDivider > 0)
                _sweepDivider--;
            else
            {
                _sweepDivider = period;
                if ((_registers[1] & 0x80) != 0 && (_registers[1] & 7) != 0)
                {
                    int shift = _registers[1] & 7;
                    int delta = _timerReload >> shift;
                    if ((_registers[1] & 8) != 0)
                    {
                        _timerReload -= delta;
                        // Pulse 1 uses one's complement for downward sweep; pulse
                        // 2 uses two's complement (one clock difference).
                        if (_complementSweep)
                            _timerReload--;
                    }
                    else
                        _timerReload += delta;
                    if (_timerReload < 0)
                        _timerReload = 0;
                }
            }

            if (_sweepReload)
            {
                _sweepReload = false;
                _sweepDivider = period;
            }
        }
    }

    /// <summary>
    /// NES triangle channel. This models the register-visible counters and the
    /// 32-step sequencer; DPCM and the analogue mixer are intentionally outside
    /// this first APU milestone.
    /// </summary>
    internal sealed class TriangleChannel
    {
        private static readonly int[] LengthTable =
        {
            10, 254, 20, 2, 40, 4, 80, 6,
            160, 8, 60, 10, 14, 12, 26, 14,
            12, 16, 24, 18, 48, 20, 96, 22,
            192, 24, 72, 26, 16, 28, 32, 30
        };

        private static readonly int[] Sequence =
        {
            15, 14, 13, 12, 11, 10, 9, 8,
            7, 6, 5, 4, 3, 2, 1, 0,
            0, 1, 2, 3, 4, 5, 6, 7,
            8, 9, 10, 11, 12, 13, 14, 15
        };

        private readonly byte[] _registers = new byte[4];
        private int _timer;
        private int _timerReload;
        private int _sequence;
        private int _linearCounter;
        private bool _linearReload;

        public bool Enabled;
        public int LengthCounter;

        public int Output
        {
            get
            {
                if (!Enabled || LengthCounter == 0 || _linearCounter == 0 || _timerReload < 2)
                    return 0;
                return Sequence[_sequence];
            }
        }

        public void Reset()
        {
            Array.Clear(_registers, 0, _registers.Length);
            _timer = 0;
            _timerReload = 0;
            _sequence = 0;
            _linearCounter = 0;
            _linearReload = false;
            Enabled = false;
            LengthCounter = 0;
        }

        public void WriteRegister(int index, byte value)
        {
            _registers[index] = value;
            switch (index)
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    _timerReload = (_timerReload & 0x700) | value;
                    break;
                case 3:
                    _timerReload = ((_registers[3] & 7) << 8) | _registers[2];
                    LengthCounter = LengthTable[(value >> 3) & 0x1F];
                    _linearReload = true;
                    break;
            }
        }

        public void TickTimer()
        {
            if (_timer == 0)
            {
                _timer = _timerReload;
                if (LengthCounter > 0 && _linearCounter > 0)
                    _sequence = (_sequence + 1) & 31;
            }
            else
            {
                _timer--;
            }
        }

        public void ClockLinearCounter()
        {
            bool control = (_registers[0] & 0x80) != 0;
            if (_linearReload)
                _linearCounter = _registers[0] & 0x7F;
            else if (_linearCounter > 0)
                _linearCounter--;
            if (!control)
                _linearReload = false;
        }

        public void ClockLengthCounter()
        {
            if ((_registers[0] & 0x80) == 0 && LengthCounter > 0)
                LengthCounter--;
        }
    }

    /// <summary>NES noise channel using the 15-bit linear feedback shift register.</summary>
    internal sealed class NoiseChannel
    {
        private static readonly int[] LengthTable =
        {
            10, 254, 20, 2, 40, 4, 80, 6,
            160, 8, 60, 10, 14, 12, 26, 14,
            12, 16, 24, 18, 48, 20, 96, 22,
            192, 24, 72, 26, 16, 28, 32, 30
        };

        private static readonly int[] NoisePeriods =
        {
            4, 8, 16, 32, 64, 96, 128, 160,
            202, 254, 380, 508, 762, 1016, 2034, 4068
        };

        private readonly byte[] _registers = new byte[4];
        private int _timer;
        private int _timerReload;
        private ushort _shiftRegister;
        private int _envelopeDivider;
        private int _envelopeDecay;
        private bool _envelopeStart;

        public bool Enabled;
        public int LengthCounter;

        public int Output
        {
            get
            {
                if (!Enabled || LengthCounter == 0 || (_shiftRegister & 1) != 0)
                    return 0;
                return (_registers[0] & 0x10) != 0 ? (_registers[0] & 0x0F) : _envelopeDecay;
            }
        }

        public void Reset()
        {
            Array.Clear(_registers, 0, _registers.Length);
            _timer = 0;
            _timerReload = NoisePeriods[0];
            _shiftRegister = 1;
            _envelopeDivider = 0;
            _envelopeDecay = 0;
            _envelopeStart = false;
            Enabled = false;
            LengthCounter = 0;
        }

        public void WriteRegister(int index, byte value)
        {
            _registers[index] = value;
            switch (index)
            {
                case 0:
                    _envelopeStart = true;
                    break;
                case 2:
                    _timerReload = NoisePeriods[value & 0x0F];
                    break;
                case 3:
                    LengthCounter = LengthTable[(value >> 3) & 0x1F];
                    _envelopeStart = true;
                    break;
            }
        }

        public void TickTimer()
        {
            if (_timer == 0)
            {
                _timer = _timerReload;
                int tap = (_registers[2] & 0x80) != 0 ? 6 : 1;
                int feedback = ((_shiftRegister & 1) ^ ((_shiftRegister >> tap) & 1)) & 1;
                _shiftRegister = (ushort)((_shiftRegister >> 1) | (feedback << 14));
            }
            else
            {
                _timer--;
            }
        }

        public void ClockEnvelope()
        {
            int period = _registers[0] & 0x0F;
            if (_envelopeStart)
            {
                _envelopeStart = false;
                _envelopeDecay = 15;
                _envelopeDivider = period;
            }
            else if (_envelopeDivider > 0)
            {
                _envelopeDivider--;
            }
            else
            {
                _envelopeDivider = period;
                if (_envelopeDecay > 0)
                    _envelopeDecay--;
                else if ((_registers[0] & 0x20) != 0)
                    _envelopeDecay = 15;
            }
        }

        public void ClockLengthCounter()
        {
            if ((_registers[0] & 0x20) == 0 && LengthCounter > 0)
                LengthCounter--;
        }
    }

    /// <summary>
    /// DMC register stub. It deliberately performs no memory DMA or audible
    /// output yet, but accepts all four DMC registers and keeps writes harmless
    /// for ROMs that initialise the complete APU block.
    /// </summary>
    internal sealed class DmcChannel
    {
        private readonly byte[] _registers = new byte[4];

        public bool Enabled;

        public void Reset()
        {
            Array.Clear(_registers, 0, _registers.Length);
            Enabled = false;
        }

        public void WriteRegister(int index, byte value)
        {
            _registers[index] = value;
        }
    }
}
