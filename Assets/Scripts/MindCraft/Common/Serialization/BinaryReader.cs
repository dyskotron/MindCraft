using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace MindCraft.Common.Serialization
{
    public class BinaryReader
    {
        private byte[] _readBuffer;
        private int _pos = 0;
        private int _startPos = 0;
        private ushort _size = 0;

        StringBuilder _stringBuilder = new StringBuilder();

        // Helper.
        public BinaryReader(byte[] _readBuffer, int _offset)
        {
            SetBuffer(_readBuffer, _offset);
        }

        public void SetBuffer(byte[] _readBuffer, int _offset)
        {
            try
            {
                this._readBuffer = _readBuffer;
                _size = (ushort) ((ushort) (this._readBuffer[_offset] << 8) | (ushort) (this._readBuffer[_offset + 1]));
                _pos = _offset + 2;
                _startPos = _pos;
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception in SetBuffer. Buffer Size " + _readBuffer.Length + ". Offset: " + _offset);
            }
        }

        public ushort Size()
        {
            return _size;
        }

        public bool AtEnd()
        {
            return _pos >= (_startPos + _size);
        }

        #region Read Basic Types
        
        //Bool
        public bool ReadBool()
        {
            return ReadByte() == 1;
        }
        
        // Byte.
        public char ReadChar()
        {
            return (char) _readBuffer[_pos++];
        }

        public byte ReadByte()
        {
            return _readBuffer[_pos++];
        }

        public sbyte ReadSByte()
        {
            return (sbyte) _readBuffer[_pos++];
        }

        public void ReadBytes(ref byte[] buffer, int size)
        {
            Debug.Assert(_pos + size <= _readBuffer.Length, "Reading outside buffer");
            Array.Copy(_readBuffer, _pos, buffer, 0, size);
            _pos += size;
        }

        // Short.
        public short ReadShort()
        {
            short value = _readBuffer[_pos++];
            value |= (short) (_readBuffer[_pos++] << 8);

            return value;
        }

        public ushort ReadUShort()
        {
            ushort value = _readBuffer[_pos++];
            value |= (ushort) (_readBuffer[_pos++] << 8);
            
            return value;
        }

        // Int.
        public int ReadInt()
        {
            int value = _readBuffer[_pos++];
            value |= _readBuffer[_pos++] << 8;
            value |= _readBuffer[_pos++] << 16;
            value |= _readBuffer[_pos++] << 24;

            return value;
        }

        public uint ReadUInt()
        {
            uint value = (uint) _readBuffer[_pos++];
            value |= (uint) (_readBuffer[_pos++] << 8);
            value |= (uint) (_readBuffer[_pos++] << 16);
            value |= (uint) (_readBuffer[_pos++] << 24);

            return value;
        }

        // Long.
        public long ReadLong()
        {
            long value = (long) _readBuffer[_pos++];
            value |= (long) ((long) (_readBuffer[_pos++]) << 8);
            value |= (long) ((long) (_readBuffer[_pos++]) << 16);
            value |= (long) ((long) (_readBuffer[_pos++]) << 24);
            value |= (long) ((long) (_readBuffer[_pos++]) << 32);
            value |= (long) ((long) (_readBuffer[_pos++]) << 40);
            value |= (long) ((long) (_readBuffer[_pos++]) << 48);
            value |= (long) ((long) (_readBuffer[_pos++]) << 56);

            return value;
        }

        public ulong ReadULong()
        {
            ulong value = (ulong) _readBuffer[_pos++];
            value |= (ulong) ((ulong) (_readBuffer[_pos++]) << 8);
            value |= (ulong) ((ulong) (_readBuffer[_pos++]) << 16);
            value |= (ulong) ((ulong) (_readBuffer[_pos++]) << 24);
            value |= (ulong) ((ulong) (_readBuffer[_pos++]) << 32);
            value |= (ulong) ((ulong) (_readBuffer[_pos++]) << 40);
            value |= (ulong) ((ulong) (_readBuffer[_pos++]) << 48);
            value |= (ulong) ((ulong) (_readBuffer[_pos++]) << 56);

            return value;
        }

        public UInt32 ReadPacked()
        {
            byte a0, a1, a2, a3, a4;
            
            a0 = ReadByte();
            if (a0 < 241)
                return a0;

            a1 = ReadByte();
            if (a0 >= 241 && a0 <= 248)
                return (UInt32) (240 + 256 * (a0 - 241) + a1);

            a2 = ReadByte();
            if (a0 == 249)
                return (UInt32) (2288 + 256 * a1 + a2);

            a3 = ReadByte();
            if (a0 == 250)
                return a1 + (((UInt32) a2) << 8) + (((UInt32) a3) << 16);

            a4 = ReadByte();
            if (a0 >= 251)
                return a1 + (((UInt32) a2) << 8) + (((UInt32) a3) << 16) + (((UInt32) a4) << 24);

            throw new IndexOutOfRangeException("ReadPacked(UInt32) failure: " + a0);
        }

        static UIntFloat s_FloatConverter;

        // Float.
        public float ReadFloat()
        {
            s_FloatConverter.intValue = ReadUInt();
            return s_FloatConverter.floatValue;
        }

        // Double.
        public double ReadDouble()
        {
            s_FloatConverter.longValue = ReadULong();
            return s_FloatConverter.doubleValue;
        }

        // Decimal.
        public decimal ReadDecimal()
        {
            Int32[] bits = new Int32[4];
            bits[0] = ReadInt();
            bits[1] = ReadInt();
            bits[2] = ReadInt();
            bits[3] = ReadInt();
            return new decimal(bits);
        }
        
        // String.
        public string ReadString()
        {
            _stringBuilder.Clear();
            
            var length = ReadInt();
            for (int i = 0; i < length; i++)
            {
                _stringBuilder.Append(ReadChar());
            }

            return _stringBuilder.ToString();
        }
        
        // Position / Rotation.
        public Vector3 ReadVector3()
        {
            var x = ReadFloat();
            var y = ReadFloat();
            var z = ReadFloat();

            return new Vector3(x, y, z);
        }
        
        public Quaternion ReadQuaternion()
        {
            var x = ReadFloat();
            var y = ReadFloat();
            var z = ReadFloat();
            var w = ReadFloat();

            return new Quaternion(x, y, z, w);
        }
        
        // Custom.
        public T Read<T>() where T : IBinarySerializable, new()
        {
            var data = new T();
            data.Deserialize(this);
            return data;
        }
        
        public byte[] ReadByteArray()
        {
            byte length = ReadByte();
            byte[] data = new byte[length];
            Array.Copy(_readBuffer,_pos, data,0, length);

            _pos += length;
            
            return data;
        }

        #endregion

        #region Float conversion

        // Helpers for float conversion
        [StructLayout(LayoutKind.Explicit)]
        internal struct UIntFloat
        {
            [FieldOffset(0)] public float floatValue;

            [FieldOffset(0)] public uint intValue;

            [FieldOffset(0)] public double doubleValue;

            [FieldOffset(0)] public ulong longValue;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct UIntDecimal
        {
            [FieldOffset(0)] public ulong longValue1;

            [FieldOffset(8)] public ulong longValue2;

            [FieldOffset(0)] public decimal decimalValue;
        }

        internal class FloatConversion
        {
            public static float ToSingle(uint value)
            {
                UIntFloat uf = new UIntFloat();
                uf.intValue = value;
                return uf.floatValue;
            }

            public static double ToDouble(ulong value)
            {
                UIntFloat uf = new UIntFloat();
                uf.longValue = value;
                return uf.doubleValue;
            }

            public static decimal ToDecimal(ulong value1, ulong value2)
            {
                UIntDecimal uf = new UIntDecimal();
                uf.longValue1 = value1;
                uf.longValue2 = value2;
                return uf.decimalValue;
            }
        }

        #endregion
    };
}