using System;

namespace MindCraft.Data
{
    [Flags]
    public enum BlockMaskId
    {
        Top = 0x01,
        Middle = 0x02,
        Bottom = 0x04,
        ExceptTop = Middle | Bottom,
        ExceptBottom = Top | Middle,
        All = Top | Middle | Bottom,
    }
    
    public static class BlockMaskByte
    {
        public const byte TOP = (byte)BlockMaskId.Top;
        public const byte MIDDLE = (byte)BlockMaskId.Middle;
        public const byte BOTTOM = (byte)BlockMaskId.Bottom;
        public const byte EXCEPT_TOP = (byte)BlockMaskId.ExceptTop;
        public const byte EXCEPT_BOTTOM = (byte)BlockMaskId.ExceptBottom;
        public const byte ALL = (byte)BlockMaskId.All;
    }
}