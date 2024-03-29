﻿using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace TinyCoin;

public class BinaryBuffer
{
    private readonly object _mutex = new object();

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
        lock (_mutex)
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
        lock (_mutex)
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
        lock (_mutex)
        {
            uint length = (uint)obj.Length * (uint)Unsafe.SizeOf<T>();
            GrowIfNeeded(length);

            foreach (var o in obj)
                Write(o);
        }
    }

    public void Write(string obj)
    {
        lock (_mutex)
        {
            uint size = (uint)obj.Length;
            WriteSize(size);

            uint length = size * sizeof(byte);
            GrowIfNeeded(length);

            foreach (char o in obj)
                Write((byte)o);
        }
    }

    public void WriteRaw(string obj)
    {
        lock (_mutex)
        {
            uint length = (uint)obj.Length * sizeof(char);
            GrowIfNeeded(length);

            foreach (char o in obj)
                Write(o);
        }
    }

    public bool ReadSize(ref uint obj)
    {
        return Read(ref obj);
    }

    public unsafe bool Read<T>(ref T obj) where T : unmanaged
    {
        lock (_mutex)
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
        lock (_mutex)
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
        lock (_mutex)
        {
            uint size = 0;
            if (!ReadSize(ref size))
                return false;

            uint length = size * sizeof(byte);

            uint finalOffset = ReadOffset + length;
            if (Buffer.Length < finalOffset)
                return false;

            var objBld = new StringBuilder((int)size);
            for (uint i = 0; i < size; i++)
            {
                byte c = 0x00;
                if (!Read(ref c))
                    return false;
                objBld.Append((char)c);
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
