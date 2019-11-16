namespace MapGeneration
{
    public enum VoxelTypeId
    {   
        Air,
        HardRock,
        Rock,
        Dirt,
        DirtWithGrass,
    }

    public static class VoxelTypeByte
    {
        public const byte AIR = (byte)VoxelTypeId.Air;
        public const byte HARD_ROCK = (byte)VoxelTypeId.HardRock;
        public const byte ROCK = (byte)VoxelTypeId.Rock;
        public const byte DIRT = (byte)VoxelTypeId.Dirt;
        public const byte DIRT_WITH_GRASS = (byte)VoxelTypeId.DirtWithGrass;
    }
}