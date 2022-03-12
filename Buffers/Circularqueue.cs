using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Circularbuffer
{
    public class Circularqueue
    {
        private ConcurrentQueue<byte[]> _queue;
        private int _maxsize = 0;
        private int _bufferedsize = 0;
        private int _size = 0;
        private byte[] temp = new byte[4096];
        public Circularqueue(int maxsize)
        {
            _queue = new ConcurrentQueue<byte[]>();
            _maxsize = maxsize;
        }
        public void add(byte[] b)
        {
            if (_size < _maxsize)
            {
                lock (_queue)
                {
                    _queue.Enqueue(b);
                    _size++;
                    _bufferedsize += b.Length ;
                }
            }
            else
            {
                Console.WriteLine("Circular buffer too small");
            }
        }
        public bool add(IntPtr b, short length)
        {
            if (_size < _maxsize)
            {
                lock (_queue)
                {
                    byte[] temp = new byte[length];
                    Marshal.Copy(b, temp, 0, length);
                    _queue.Enqueue(temp);
                    _size++;
                    _bufferedsize += length;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool add(byte[] b, int offset, int length)
        {
            if (_size < _maxsize)
            {
                lock (_queue)
                {
                    byte[] temp = new byte[length];
                    Buffer.BlockCopy(b, offset, temp, 0, length);
                    _queue.Enqueue(temp);
                    _size++;
                    _bufferedsize += length;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool get(out byte[]? b)
        {
            bool result;
            if (_size > 0)
            {
                lock (_queue)
                {
                    _size--;
                    result = _queue.TryDequeue(out b);
                    if (b != null)
                        _bufferedsize -= b.Length;
                }
            }
            else
            {
                result = false;
                b = null;
            }
            return result;
        }
        public byte[]? get()
        {
            bool result;
            byte[]? b;
            if (_size > 0)
            {
                lock (_queue)
                {
                    _size--;
                    result = _queue.TryDequeue(out b);
                    if (b != null)
                        _bufferedsize -= b.Length;
                }
            }
            else
            {
                result = false;
                b = null;
            }
            return b;
        }
        public bool peek(byte[]? b)
        {
            bool result;
            if (_size > 0)
            {
                lock (_queue)
                {
                    result = _queue.TryPeek(out b);
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        public int getBufferedsize()
        {
            return _bufferedsize;
        }

    }
}
