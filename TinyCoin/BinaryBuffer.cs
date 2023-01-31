using System;
using System.Runtime.CompilerServices;
using System.Text;

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

        public unsafe void Write<T>(T obj) where T : unmanaged
        {
            lock (Mutex)
            {
                uint length = (uint)Unsafe.SizeOf<T>();
                GrowIfNeeded(length);
                fixed (byte* b = Buffer)
                {
                    System.Buffer.MemoryCopy(&obj, b + WriteOffset, Buffer.Length - WriteOffset, length);

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(Buffer, (int)WriteOffset, (int)length);
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

        public unsafe bool Read<T>(ref T obj) where T : unmanaged
        {
            lock (Mutex)
            {
                uint length = (uint)Unsafe.SizeOf<T>();

                uint finalOffset = ReadOffset + length;
                if (Buffer.Length < finalOffset)
                    return false;

                fixed (T* o = &obj)
                fixed (byte* b = Buffer)
                {
                    byte* p = (byte*)o;

                    System.Buffer.MemoryCopy(b + ReadOffset, p, length, length);

                    if (!BitConverter.IsLittleEndian)
                    {
                        byte* pStart = p;
                        byte* pEnd = p + length - 1;
                        for (int i = 0; i < length / 2; i++)
                        {
                            byte temp = *pStart;
                            *pStart++ = *pEnd;
                            *pEnd-- = temp;
                        }
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

                uint length = size * (uint)Unsafe.SizeOf<T>();

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

                uint length = size * sizeof(char);

                uint finalOffset = ReadOffset + length;
                if (Buffer.Length < finalOffset)
                    return false;

                var objBld = new StringBuilder((int)size);
                for (uint i = 0; i < size; i++)
                {
                    char c = '\0';
                    if (!Read(ref c))
                        return false;
                    objBld.Append(c);
                }

                obj = objBld.ToString();

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
