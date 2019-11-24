using System.Collections.Generic;
using Unity.Mathematics;

namespace MindCraft.MapGeneration.Utils
{
    public static class MapBoundsLookup
    {
        public static readonly int2[] ChunkRemove;

        public static readonly int2[] ChunkAdd;

        public static readonly int2[] ChunkGenaration;

        public static readonly int2[] MapDataRemove;

        public static readonly int2[] MapDataAdd;

        public static readonly int2[] MapDataGenaration;

        private const bool USE_RADIAL_BOUNDS = true;
        
        // make sure that map data are generated in advance as chunks render needs access to neighbours map data to generate chunk properly.
        private const int MAP_DATA_LOOKAHEAD = 3;

        // higher offset => more memory used, but less chunks to regenerate when returning to already visited chunks
        private const int REMOVE_RING_OFFSET = 10;

        static MapBoundsLookup()
        {
            if (USE_RADIAL_BOUNDS)
            {
                MapDataGenaration = GenerateCircle(VoxelLookups.VIEW_DISTANCE_IN_CHUNKS + MAP_DATA_LOOKAHEAD);
                MapDataAdd = GenerateRing(VoxelLookups.VIEW_DISTANCE_IN_CHUNKS + MAP_DATA_LOOKAHEAD, 2);
                MapDataRemove = GenerateRing(VoxelLookups.VIEW_DISTANCE_IN_CHUNKS + MAP_DATA_LOOKAHEAD + REMOVE_RING_OFFSET, 2);

                ChunkGenaration = GenerateCircle(VoxelLookups.VIEW_DISTANCE_IN_CHUNKS);
                ChunkAdd = GenerateRing(VoxelLookups.VIEW_DISTANCE_IN_CHUNKS, 2);
                ChunkRemove = GenerateRing(VoxelLookups.VIEW_DISTANCE_IN_CHUNKS + REMOVE_RING_OFFSET, 2);
            }
            else
            {
                MapDataGenaration = GenerateRect(VoxelLookups.VIEW_DISTANCE_IN_CHUNKS + MAP_DATA_LOOKAHEAD);
                MapDataAdd = GenerateRectRing(VoxelLookups.VIEW_DISTANCE_IN_CHUNKS + MAP_DATA_LOOKAHEAD, 2);
                MapDataRemove = GenerateRectRing(VoxelLookups.VIEW_DISTANCE_IN_CHUNKS + MAP_DATA_LOOKAHEAD + REMOVE_RING_OFFSET, 2);

                ChunkGenaration = GenerateRect(VoxelLookups.VIEW_DISTANCE_IN_CHUNKS);
                ChunkAdd = GenerateRectRing(VoxelLookups.VIEW_DISTANCE_IN_CHUNKS, 2);
                ChunkRemove = GenerateRectRing(VoxelLookups.VIEW_DISTANCE_IN_CHUNKS + REMOVE_RING_OFFSET, 2);
            }
        }

        #region Radial bounds

        /// <summary>
        /// Pick positions from square grid which are within circe of given radius
        /// </summary>
        /// <param name="radius">circle radius</param>
        /// <returns></returns>
        private static int2[] GenerateCircle(int radius)
        {
            var indexes = new List<int2>();

            var min = -radius;
            var max = radius;

            var radiusSqr = radius * radius;

            for (var iX = min; iX <= max; iX++)
            {
                for (var iY = min; iY <= max; iY++)
                {
                    if (IsQuadWithinCircle(iX, iY, radiusSqr))
                        indexes.Add(new int2(iX, iY));
                }
            }

            //TODO: sort by distance maybe? that way we'd know map data / chunks are generated first around the player

            return indexes.ToArray();
        }

        /// <summary>
        /// Pick positions from square grid which are on ring defined by outer radius and size
        /// </summary>
        /// <param name="radius">outer radius</param>
        /// <param name="ringSize">ring width</param>
        /// <returns></returns>
        private static int2[] GenerateRing(int radius, int ringSize = 1)
        {
            var indexes = new List<int2>();

            var min = -radius;
            var max = radius;

            var outerRadiusSqr = radius * radius;
            var innerRadius = (radius - ringSize) * (radius - ringSize);

            for (var iX = min; iX <= max; iX++)
            {
                for (var iY = min; iY <= max; iY++)
                {
                    if (IsQuadOutOfCircle(iX, iY, innerRadius) && IsQuadWithinCircle(iX, iY, outerRadiusSqr))
                        indexes.Add(new int2(iX, iY));
                }
            }

            return indexes.ToArray();
        }

        /// <summary>
        /// check if any of quad's corners is within circle.
        /// Quads considered 1 x 1 size with its position defined by 0,0 coords
        /// </summary>
        /// <param name="x">x position of quad</param>
        /// <param name="y">y position of quad</param>
        /// <param name="radiusSqr">circle pow2 radius</param>
        /// <returns>True of any of corners lies within circle</returns>
        private static bool IsQuadWithinCircle(int x, int y, int radiusSqr)
        {
            if ((x * x + y * y) <= radiusSqr)
                return true;

            if (((x + 1) * (x + 1) + y * y) <= radiusSqr)
                return true;

            if (((x + 1) * (x + 1) + (y + 1) * (y + 1)) <= radiusSqr)
                return true;

            if ((x * x + (y + 1) * (y + 1)) <= radiusSqr)
                return true;

            return false;
        }

        /// <summary>
        /// check if any of quad's corners lies out circle within circle.
        /// Quads considered 1 x 1 size with its position defined by 0,0 coords
        /// </summary>
        /// <param name="x">x position of quad</param>
        /// <param name="y">y position of quad</param>
        /// <param name="radiusSqr">circle pow2 radius</param>
        /// <returns>True of any of corners lies ou of circle</returns>
        private static bool IsQuadOutOfCircle(int x, int y, int radiusSqr)
        {
            if ((x * x + y * y) > radiusSqr)
                return true;

            if (((x + 1) * (x + 1) + y * y) > radiusSqr)
                return true;

            if (((x + 1) * (x + 1) + (y + 1) * (y + 1)) > radiusSqr)
                return true;

            if ((x * x + (y + 1) * (y + 1)) > radiusSqr)
                return true;

            return false;
        }

        #endregion

        #region Rectangular bounds

        /// <summary>
        /// Pick positions from square grid which are within circe of given radius
        /// </summary>
        /// <param name="radius">circle radius</param>
        /// <returns></returns>
        private static int2[] GenerateRect(int radius)
        {
            var indexes = new List<int2>();

            var min = -radius;
            var max = radius;

            for (var iX = min; iX <= max; iX++)
            {
                for (var iY = min; iY <= max; iY++)
                {
                    indexes.Add(new int2(iX, iY));
                }
            }

            //TODO: sort by distance maybe? that way we'd know map data / chunks are generated first around the player

            return indexes.ToArray();
        }

        /// <summary>
        /// Pick positions from square grid which are on rectangle defined by outer radius and size
        /// </summary>
        /// <param name="radius">outer radius</param>
        /// <param name="ringSize">ring width</param>
        /// <returns></returns>
        private static int2[] GenerateRectRing(int radius, int ringSize = 1)
        {
            var indexes = new List<int2>();

            var min = -radius;
            var max = radius;

            var minInner = -radius + ringSize;
            var maxInner = radius - ringSize;

            for (var iX = min; iX <= max; iX++)
            {
                for (var iY = min; iY <= max; iY++)
                {
                    if (iX < minInner + ringSize || iX > maxInner || iY < minInner + ringSize || iY > maxInner)
                        indexes.Add(new int2(iX, iY));
                }
            }

            return indexes.ToArray();
        }

        #endregion
    }
}