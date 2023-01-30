using System;
using System.Runtime.CompilerServices;

namespace TinyCoin
{
    public class BinaryBuffer
    {
        private readonly object Mutex = new object();

        public BinaryBuffer()
        {
            Buffer = Array.Empty<byte>();
        }

        public BinaryBuffer(byte[] obj)
        {
            Buffer = obj;
            WriteOffset = (uint)obj.Length;
        }

        public byte[] Buffer { get; private set; }

        public uint ReadOffset { get; private set; }
        public uint WriteOffset { get; private set; }

        public void WriteSize(uint obj)
        {
            Write(obj);
        }

        public void Write<T>(T obj) where T : unmanaged
        {
            lock (Mutex)
            {
                uint length = (uint)Unsafe.SizeOf<T>();
                GrowIfNeeded(length);
                unsafe
                {
                    fixed (byte* b = Buffer)
                    {
                        //TODO: endianness
                        System.Buffer.MemoryCopy(&obj, b + WriteOffset, Buffer.Length - WriteOffset, length);
                    }
                }

                WriteOffset += length;
            }
        }

        public void Write<T>(T[] obj) where T : unmanaged
        {
            lock (Mutex)
            {
                uint size = (uint)obj.Length;
                WriteSize(size);

                uint length = size * (uint)Unsafe.SizeOf<T>();
                GrowIfNeeded(length);

                foreach (var o in obj)
                    Write(o);
            }
        }

        public void WriteRaw<T>(T[] obj) where T : unmanaged
        {
            lock (Mutex)
            {
                uint length = (uint)obj.Length * (uint)Unsafe.SizeOf<T>();
                GrowIfNeeded(length);

                foreach (var o in obj)
                    Write(o);
            }
        }

        public void Write(string obj)
        {
            lock (Mutex)
            {
                uint size = (uint)obj.Length;
                WriteSize(size);

                uint length = size * sizeof(char);
                GrowIfNeeded(length);

                foreach (char o in obj)
                    Write(o);
            }
        }

        public void WriteRaw(string obj)
        {
            lock (Mutex)
            {
                uint length = (uint)obj.Length * sizeof(char);
                GrowIfNeeded(length);

                foreach (char o in obj)
                    Write(o);
            }
        }

        private bool ReadSize(ref uint obj)
        {
            return Read(ref obj);
        }

        public bool Read<T>(ref T obj) where T : unmanaged
        {
            lock (Mutex)
            {
                uint length = (uint)Unsafe.SizeOf<T>();

                uint finalOffset = ReadOffset + length;
                if (Buffer.Length < finalOffset)
                    return false;

                unsafe
                {
                    fixed (T* p = &obj)
                    fixed (byte* b = Buffer)
                    {
                        //TODO: endianness
                        System.Buffer.MemoryCopy(b + ReadOffset, p, length, length);
                    }
                }

                ReadOffset = finalOffset;

                return true;
            }
        }

        public bool Read<T>(ref T[] obj) where T : unmanaged
        {
            lock (Mutex)
            {
                uint size = 0;
                if (!ReadSize(ref size))
                    return false;

                uint length = (uint)obj.Length * (uint)Unsafe.SizeOf<T>();

                uint finalOffset = ReadOffset + length;
                if (Buffer.Length < finalOffset)
                    return false;

                obj = new T[size];
                for (uint i = 0; i < size; i++)
                    if (!Read(ref obj[i]))
                        return false;

                return true;
            }
        }

        public bool Read(ref string obj)
        {
            lock (Mutex)
            {
                uint size = 0;
                if (!ReadSize(ref size))
                    return false;

                uint length = (uint)obj.Length * sizeof(char);

                uint finalOffset = ReadOffset + length;
                if (Buffer.Length < finalOffset)
                    return false;

                obj = string.Empty;
                for (uint i = 0; i < size; i++)
                {
                    char c = '\0';
                    if (!Read(ref c))
                        return false;
                    obj += c;
                }

                return true;
            }
        }

        private void GrowIfNeeded(uint writeLength)
        {
            uint finalLength = WriteOffset + writeLength;
            bool resizeNeeded = Buffer.Length <= finalLength;

            if (resizeNeeded)
            {
                byte[] tmp = Buffer;
                Array.Resize(ref tmp, (int)finalLength);
                Buffer = tmp;
            }
        }
    }
}
