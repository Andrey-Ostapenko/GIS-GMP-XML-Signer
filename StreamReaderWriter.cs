using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace gisgmp_signer
{
    class StreamReaderWriter: IDisposable
    {
        private StreamReader streamReader;
        private StreamWriter streamWriter;
        private Stream baseStream;
        public bool AutoFlush
        {
            get
            {
                return streamWriter.AutoFlush;
            }
            set
            {
                streamWriter.AutoFlush = value;
            }
        }

        public StreamReaderWriter(Stream stream)
        {
            baseStream = stream;
            streamReader = new StreamReader(baseStream);
            streamWriter = new StreamWriter(baseStream);
        }

        void IDisposable.Dispose()
        {
            streamReader.Dispose();
            streamWriter.Dispose();
        }

        public void Dispose()
        {
            try
            {
                if (baseStream != null)
                {
                    streamReader.Dispose();
                    streamWriter.Dispose();
                }
            }
            catch // (Exception E)
            {
                
            }
        }

        public int Read()
        {
            char[] buffer = new char[256];
            int i = streamReader.Read(buffer, 0, 256);
            return i;
        }

        public string ReadLine()
        {
            return streamReader.ReadLine();
        }

        public string ReadToEnd()
        {
            return streamReader.ReadToEnd();
        }

        public void WriteLine(string value)
        {
            streamWriter.WriteLine(value);
        }

        public void Write(string value)
        {
            streamWriter.Write(value);
        }

        public void Flush()
        {
            streamWriter.Flush();
        }
    }
}
