using Unity.Mathematics;
using UnityEngine;

namespace MindCraft.Utils
{
    public struct VoronoiResult
    {
        public readonly float MinDistToCell;
        public readonly float MinEdgeDistance;
        public readonly float2 ClosestCell;
        public readonly float2 SecondClosestCell;

        public VoronoiResult(float minDistToCell, float minEdgeDistance, float2 closestCell, float2 secondClosestCell)
        {
            MinDistToCell = minDistToCell;
            MinEdgeDistance = minEdgeDistance;
            ClosestCell = closestCell;
            SecondClosestCell = secondClosestCell;
        }
    }
    
    /// <summary>
    /// Custom Voronoi Noise algorithm taken from https://www.ronja-tutorials.com/2018/09/29/voronoi-noise.html
    /// Small tweaks to return as much data as possible, so it can be much more helpfull for terrain generation
    ///
    /// xy of returned float is closest cell coordinates
    /// z is distance to closest cell
    /// w is distance to closest border
    /// </summary>
    public static class CustomVoronoi
    {
        public static VoronoiResult voronoi(float2 value)
        {
            float2 baseCell = math.floor(value);
            
            //first pass to find the closest cell
            float2 closestCell = new float2();
            float2 toClosestCell = new float2();
            float minDistToCell = 10;
            
            float2 secondClosestCell = new float2();
            float minDistToSecondCell = 10;

            for (int x1 = -1; x1 <= 1; x1++)
            {
                for (int y1 = -1; y1 <= 1; y1++)
                {
                    float2 cell = baseCell + new float2(x1, y1);
                    float2 cellPosition = cell + rand2dTo2d(cell);
                    float2 toCell = cellPosition - value;
                    float distToCell = length(toCell);
                    
                    if (distToCell < minDistToCell)
                    {
                        //store previous closest cell
                        secondClosestCell = closestCell;
                        minDistToSecondCell = minDistToCell;
                        
                        //store new cell and its values
                        minDistToCell = distToCell;
                        closestCell = cell;
                        toClosestCell = toCell;
                    }
                    //if not closest we can still check for 2nd closest
                    else if(distToCell < minDistToSecondCell)
                    {
                        secondClosestCell = cell;
                        minDistToSecondCell = distToCell;    
                    }
                }
            }
            
            //second pass to find the distance to the closest edge
            float minEdgeDistance = 10;
            for(int x2=-1; x2<=1; x2++){
                for(int y2=-1; y2<=1; y2++){
                    float2 cell = baseCell + new float2(x2, y2);
                    float2 cellPosition = cell + rand2dTo2d(cell);
                    float2 toCell = cellPosition - value;

                    float2 diffToClosestCell = math.abs(closestCell - cell);
                    bool isClosestCell = diffToClosestCell.x + diffToClosestCell.y < 0.1f;
                    if(!isClosestCell){
                        float2 toCenter = (toClosestCell + toCell) * 0.5f;
                        float2 cellDifference = math.normalize(toCell - toClosestCell);
                        float edgeDistance = math.dot(toCenter, cellDifference);
                        minEdgeDistance = math.min(minEdgeDistance, edgeDistance);
                    }
                }
            }

            return new VoronoiResult(minDistToCell, minEdgeDistance, closestCell, secondClosestCell);
        }

        #region Custom rand functions

        public static readonly float2 DEFAULT_DOT_DIR = new float2(12.9898f, 78.233f);

        public static float length(float2 value)
        {
            return math.sqrt(math.pow(value.x, 2) + math.pow(value.y, 2));
        }

        public static float rand1dTo1d(float value, float mutator = 0.546f)
        {
            float random = math.frac(math.sin(value + mutator) * 143758.5453f);
            return random;
        }


        public static float2 rand2dTo2d(float2 value)
        {
            return new float2(
                              rand2dTo1d(value, new float2(12.989f, 78.233f)),
                              rand2dTo1d(value, new float2(39.346f, 11.135f))
                             );
        }

        public static float rand2dTo1d(float2 value, float2 dotDir)
        {
            float2 smallValue = math.sin(value);
            float random = math.dot(smallValue, dotDir);
            random = math.frac(math.sin(random) * 143758.5453f);
            return random;
        }

        public static float3 rand1dTo3d(float value)
        {
            return new float3(
                              rand1dTo1d(value, 3.9812f),
                              rand1dTo1d(value, 7.1536f),
                              rand1dTo1d(value, 5.7241f)
                             );
        }

        public static Color rand1dToColor(float value)
        {
            return new Color(
                             rand1dTo1d(value, 3.9812f),
                             rand1dTo1d(value, 7.1536f),
                             rand1dTo1d(value, 5.7241f)
                            );
        }

        #endregion
    }
}