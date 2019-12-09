using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MindCraft.Common.Serialization
{
    public class BinaryWriter
    {
        const int BUFFER_LEN = 1024 * 32; // 32KB
        private byte[] _writeBuffer = new byte[BUFFER_LEN];
        private int _pos = 0;
        private int _size = 0;
        private float _writeTime = 0;

        public float WriteTime
        {
            get { return _writeTime; }
        }

        public byte[] ToArray()
        {
            byte[] array = new byte[_pos];
            Array.Copy(_writeBuffer, array, _pos);
            return array;
        }

        public byte[] GetWriteBuffer()
        {
            return _writeBuffer;
        }

        public int GetWritePos()
        {
            return _pos;
        }

        public void Begin()
        {
            // Save some space for size.
            _writeBuffer[0] = 0;
            _writeBuffer[1] = 0;
            _pos = 2;
            _writeTime = Time.time;
        }

        public void End()
        {
            // Write size.
            _size = _pos - 2;
            _writeBuffer[0] = (byte) ((_size >> 8) & 0xff);
            _writeBuffer[1] = (byte) (_size & 0xff);
        }

        // Not updated until End() is called.
        public ushort Size()
        {
            return (ushort) _size;
        }

        #region Write Basic Types

        // Bool.
        public void Write(bool value)
        {
            Write((byte) (value ? 1 : 0));
        }
        
        // Byte.
        public void Write(char value)
        {
            _writeBuffer[_pos++] = (byte) value;
        }

        public void Write(byte value)
        {
            _writeBuffer[_pos++] = value;
        }

        public void Write(sbyte value)
        {
            _writeBuffer[_pos++] = (byte) value;
        }

        public void Write(ref byte[] buffer, int size)
        {
            Debug.Assert(_pos + size <= _writeBuffer.Length, "Writing outside buffer");
            Array.Copy(buffer, 0, _writeBuffer, _pos, size);
            _pos += size;
        }

        // Short.
        public void Write(short value)
        {
            _writeBuffer[_pos++] = (byte) (value & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 8) & 0xff);
        }

        public void Write(ushort value)
        {
            _writeBuffer[_pos++] = (byte) (value & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 8) & 0xff);
        }

        // Int.
        public void Write(int value)
        {
            _writeBuffer[_pos++] = (byte) (value & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 8) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 16) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 24) & 0xff);
        }

        public void Write(uint value)
        {
            _writeBuffer[_pos++] = (byte) (value & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 8) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 16) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 24) & 0xff);
        }

        // Long.
        public void Write(long value)
        {
            _writeBuffer[_pos++] = (byte) (value & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 8) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 16) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 24) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 32) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 40) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 48) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 56) & 0xff);
        }

        public void Write(ulong value)
        {
            _writeBuffer[_pos++] = (byte) (value & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 8) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 16) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 24) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 32) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 40) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 48) & 0xff);
            _writeBuffer[_pos++] = (byte) ((value >> 56) & 0xff);
        }

        // http://sqlite.org/src4/doc/trunk/www/varint.wiki
        public void WritePacked(UInt32 value)
        {
            if (value <= 240)
            {
                Write((byte) value);
                return;
            }

            if (value <= 2287)
            {
                Write((byte) ((value - 240) / 256 + 241));
                Write((byte) ((value - 240) % 256));
                return;
            }

            if (value <= 67823)
            {
                Write((byte) 249);
                Write((byte) ((value - 2288) / 256));
                Write((byte) ((value - 2288) % 256));
                return;
            }

            if (value <= 16777215)
            {
                Write((byte) 250);
                Write((byte) (value & 0xFF));
                Write((byte) ((value >> 8) & 0xFF));
                Write((byte) ((value >> 16) & 0xFF));
                return;
            }

            // all other values of uint
            Write((byte) 251);
            Write((byte) (value & 0xFF));
            Write((byte) ((value >> 8) & 0xFF));
            Write((byte) ((value >> 16) & 0xFF));
            Write((byte) ((value >> 24) & 0xFF));
        }

        static UIntFloat s_FloatConverter;

        // Float.
        public void Write(float value)
        {
            s_FloatConverter.floatValue = value;
            Write(s_FloatConverter.intValue);
        }

        // Double.
        public void Write(double value)
        {
            s_FloatConverter.doubleValue = value;
            Write(s_FloatConverter.longValue);
        }

        // Decimal.
        public void Write(decimal value)
        {
            Int32[] bits = decimal.GetBits(value);
            Write(bits[0]);
            Write(bits[1]);
            Write(bits[2]);
            Write(bits[3]);
        }

        // String
        public void Write(string value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }
        
        // Position / Rotation
        public void Write(Vector3 value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
        }
        
        public void Write(Quaternion value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);
        }
        
        // Custom
        public void Write(IBinarySerializable data)
        {
            data.Serialize(this);   
        }
        
        public void Write(byte[] value)
        {
            byte length = (byte) value.Length;
            Write(length);
            Array.Copy(value, 0, _writeBuffer, _pos, value.Length);
            _pos += length;
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