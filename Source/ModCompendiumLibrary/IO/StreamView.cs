using System;
using System.IO;

namespace ModCompendiumLibrary.IO
{
    public class StreamView : Stream
    {
        private readonly Stream mSourceStream;
        private long mSourcePositionSave;
        private readonly long mStartPosition;
        private long mLength;

        public StreamView(Stream source, long startPosition, long length)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (startPosition < 0 || startPosition >= source.Length || (startPosition + length) > source.Length)
                throw new ArgumentOutOfRangeException(nameof(startPosition));

            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            mSourceStream = source;
            mStartPosition = startPosition;
            Position = 0;
            mLength = length;
        }

        public override void Flush()
        {
            mSourceStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    {
                        if (offset > mLength || offset > mSourceStream.Length)
                            throw new ArgumentOutOfRangeException(nameof(offset));

                        Position = offset;
                    }
                    break;
                case SeekOrigin.Current:
                    {
                        if ((Position + offset) > mLength || (Position + offset) > mSourceStream.Length)
                            throw new ArgumentOutOfRangeException(nameof(offset));

                        Position += offset;
                    }
                    break;
                case SeekOrigin.End:
                    {
                        Position = (mStartPosition + mLength) - offset;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            if (value > mSourceStream.Length)
                throw new ArgumentOutOfRangeException(nameof(value));

            mLength = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position == mLength)
                return 0;

            if ((Position + count) > mLength)
                count = (int)(mLength - Position);

            SaveSourcePosition();
            SetSourcePositionForSubstream();
            int result = mSourceStream.Read(buffer, offset, count);
            Position += count;
            RestoreSourcePosition();

            return result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => mSourceStream.CanRead;

        public override bool CanSeek => mSourceStream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => mLength;

        public override long Position { get; set; }

        public override int ReadByte()
        {
            if (Position == mLength)
                return -1;

            SaveSourcePosition();
            SetSourcePositionForSubstream();
            int value = mSourceStream.ReadByte();
            Position++;
            RestoreSourcePosition();

            return value;
        }

        public override void WriteByte(byte value)
        {
            throw new NotImplementedException();
        }

        /*
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            SaveSourcePosition();
            SetSourcePositionForSubstream();
            var value = mSourceStream.BeginRead(buffer, offset, count, callback, state);
            RestoreSourcePosition();

            return value;
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            SaveSourcePosition();
            SetSourcePositionForSubstream();
            var value = mSourceStream.BeginWrite(buffer, offset, count, callback, state);
            RestoreSourcePosition();

            return value;
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            SaveSourcePosition();
            SetSourcePositionForSubstream();
            var value = mSourceStream.CopyToAsync(destination, bufferSize, cancellationToken);
            RestoreSourcePosition();

            return value;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            SaveSourcePosition();
            SetSourcePositionForSubstream();
            var value = mSourceStream.EndRead(asyncResult);
            RestoreSourcePosition();

            return value;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            SaveSourcePosition();
            SetSourcePositionForSubstream();
            mSourceStream.EndWrite(asyncResult);
            RestoreSourcePosition();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            SaveSourcePosition();
            SetSourcePositionForSubstream();
            var value = mSourceStream.WriteAsync(buffer, offset, count, cancellationToken);
            RestoreSourcePosition();

            return value;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            SaveSourcePosition();
            SetSourcePositionForSubstream();
            var value = mSourceStream.ReadAsync(buffer, offset, count, cancellationToken);
            RestoreSourcePosition();

            return value;
        }
        */
        protected void SaveSourcePosition()
        {
            mSourcePositionSave = mSourceStream.Position;
        }

        protected void SetSourcePositionForSubstream()
        {
            mSourceStream.Position = (mStartPosition + Position);
        }

        protected void RestoreSourcePosition()
        {
            mSourceStream.Position = mSourcePositionSave;
        }
    }
}
