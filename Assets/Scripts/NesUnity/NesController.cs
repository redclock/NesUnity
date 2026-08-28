namespace NesUnity
{
    public sealed class NesController
    {
        public enum Button
        {
            A = 0,
            B = 1,
            Select = 2,
            Start = 3,
            Up = 4,
            Down = 5,
            Left = 6,
            Right = 7
        }

        private byte _buttons;
        private byte _shift;
        private bool _strobe;

        public void SetButton(Button button, bool pressed)
        {
            byte mask = (byte)(1 << (int)button);
            if (pressed)
                _buttons |= mask;
            else
                _buttons &= (byte)~mask;
        }

        public bool GetButton(Button button)
        {
            return (_buttons & (1 << (int)button)) != 0;
        }

        public void Reset()
        {
            _buttons = 0;
            _shift = 0;
            _strobe = false;
        }

        public byte Buttons => _buttons;

        public void Latch()
        {
            _shift = _buttons;
        }

        public void Write(byte value)
        {
            bool strobe = (value & 1) != 0;
            if (strobe)
            {
                _strobe = true;
                _shift = _buttons;
            }
            else
            {
                if (_strobe)
                    _shift = _buttons;
                _strobe = false;
            }
        }

        public byte Read()
        {
            byte result = (byte)(_strobe ? (_buttons & 1) : (_shift & 1));
            if (!_strobe)
                _shift = (byte)((_shift >> 1) | 0x80);
            return result;
        }
    }
}
