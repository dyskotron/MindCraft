using System.Diagnostics;
using MindCraft.Tests;
using MindCraft.Tests.TestBounds;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TestsRunner : MonoBehaviour
{
    public int RoundCount = 100;

    // Start is called before the first frame update
    void Start()
    {
        RunTest(new TestBoundsRadial());
        RunTest(new TestBoundsRectangular());
        RunTest(new TestBoundsRectangularPreinitedArray());
        RunTest(new TestBoundsRectangularPreinitedArrayForeach());
    }

    private void RunTest(IMindCraftTest test)
    {
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
