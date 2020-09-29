using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wave.Filters
{
    public class VolumeFilter : IWaveFilter
    {
        public double Volume
        {
            get
            {
                return (Db - DbMin) / (DbMax - DbMin);
            }
            set
            {
                Db = DbMin + value * (DbMax - DbMin);
            }
        }

        public double DbMax { get; set; }
        public double DbMin { get; set; }

        public double Db
        {
            get
            {
                return 20 * Math.Log10(Ratio);
            }
            private set
            {
                Ratio = Math.Pow(10, value / 20);
            }
        }

        public double Ratio { get; private set; }

        public VolumeFilter()
        {
            DbMax = 0;
            DbMin = -80;
            Volume = 1;
        }

        public void ProcessBlock(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i+=2)
            {
                short v = ToShort(buffer[i], buffer[i+1]);

                v = (short)(v * Ratio);

                FromShort(v, out byte byte1, out byte byte2);
                buffer[i] = byte1;
                buffer[i+1] = byte2;
            }
        }

        static short ToShort(short byte1, short byte2)
        {
            return (short)((byte2 << 8) + byte1);
        }

        static void FromShort(short number, out byte byte1, out byte byte2)
        {
            byte2 = (byte)(number >> 8);
            byte1 = (byte)(number & 255);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
