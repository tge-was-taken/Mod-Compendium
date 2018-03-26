using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCompendiumLibrary.IO
{
    public class UncloseableStream : Stream, IDisposable
    {
        private readonly Stream mStream;

        public UncloseableStream( Stream stream )
        {
            mStream = stream;
        }

        public override void Flush()
        {
            mStream.Flush();
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            return mStream.Seek( offset, origin );
        }

        public override void SetLength( long value )
        {
            mStream.SetLength( value );
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            return mStream.Read( buffer, offset, count );
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            mStream.Write( buffer, offset, count );
        }

        public override bool CanRead => mStream.CanRead;

        public override bool CanSeek => mStream.CanSeek;

        public override bool CanWrite => mStream.CanWrite;

        public override long Length => mStream.Length;

        public override long Position
        {
            get => mStream.Position;
            set => mStream.Position = value;
        }

        public override void Close()
        {
            // Dummy
        }

        public new void Dispose()
        {
            // Dummy
        }

        public void ForceDispose()
        {
            base.Dispose();
        }
    }
}
