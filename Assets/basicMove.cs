using UnityEngine;
using toio;

public class basicMove : MonoBehaviour
{
    float intervalTime = 0.05f;
    float elapsedTime = 0;
    Cube cube;

    // Start is called before the first frame update
    async void Start()
    {
        var peripheral = await new NearestScanner().Scan();
        cube = await new CubeConnecter().Connect(peripheral);
    }

    // Update is called once per frame
    void Update()
    {
        if (null == cube) { return; }
        elapsedTime += Time.deltaTime;

        if(intervalTime < elapsedTime)
        {
            elapsedTime = 0.0f;
            cube.Move(50, -50, 200);
        }
    }
}
