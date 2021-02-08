using System;

namespace Wave.Filters
{
    public interface IWaveFilter : IDisposable
    {
        void ProcessBlock(byte[] buffer);
    }
}
