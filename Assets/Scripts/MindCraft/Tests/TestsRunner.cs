using System.Diagnostics;
using MindCraft.Tests;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TestsRunner : MonoBehaviour
{
    public int RoundCount = 100;

    // Start is called before the first frame update
    void Start()
    {
        
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
