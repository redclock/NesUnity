using NesUnity.Mappers;

namespace NesUnity
{

    public partial class Cpu
    {
        private CpuMemory _memory;
        public int Cycle;

        public Cpu(MapperBase mapper)
        {
            _memory = new CpuMemory(mapper);
        }
    }

}