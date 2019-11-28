using System;
using System.Diagnostics;
using MindCraft.MapGeneration.Utils;
using MindCraft.Tests;
using MindCraft.Tests.TestBounds;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TestsRunner : MonoBehaviour
{
    public int RoundCount = 100;

    // Start is called before the first frame update
    void Start()
    {
        RunTest(new TestBoundsRadial());
        //RunTest(new TestBoundsRectangular());
        //RunTest(new TestBoundsRectangularPreinitedArray());
        //RunTest(new TestBoundsRectangularPreinitedArrayForeach());
    }

    private void RunTest(IMindCraftTest test)
    {
        for (var iF = 0; iF < 6; iF++)
        {
            Debug.LogWarning($"<color=\"aqua\">TestsRunner.RunTest() : ===================== {iF} =====================</color>");
            for (var iV = 0; iV < 4; iV++)
            {

                Debug.LogWarning($"<color=\"aqua\">TestsRunner.RunTest() : {VoxelLookups.Neighbours[VoxelLookups.LightNeighbours[iF][iV][0]]} + " +
                                 $"{VoxelLookups.Neighbours[VoxelLookups.LightNeighbours[iF][iV][1]]}</color>");
            }
        }

        return;
            
            
        for (var i = 0; i < 100; i++)
        {
            var val = noise.cnoise(new float2(i / 20f - 2, 0.5f));
            var cube = transform.GetChild(i);
            
            //SetX(cube, Mathf.Floor(i / 10f));
            //SetY(cube, Mathf.Floor(i % 10f));
            
            SetX(cube, i * 1.2f);
            SetY(cube, 0);
            
            SetZ(cube, val * 10);
        }
        
        
        return;
        
        test.Init();

        var watch = new Stopwatch();
        watch.Start();

        for (var i = 0; i < RoundCount; i++)
        {
            test.Run();       
        }
        
        watch.Stop();
        
        Debug.LogWarning($"<color=\"aqua\">TestsRunner.RunTest({test.GetType()}) : elapsed: {watch.ElapsedMilliseconds}</color>");    
    }

    private void SetX(Transform transform, float val)
    {
        var pos = transform.position;
        pos.x = val;
        transform.position = pos;
    }
    
    private void SetY(Transform transform, float val)
    {
        var pos = transform.position;
        pos.y = val;
        transform.position = pos;
    }
    
    private void SetZ(Transform transform, float val)
    {
        var pos = transform.position;
        pos.z = val;
        transform.position = pos;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
