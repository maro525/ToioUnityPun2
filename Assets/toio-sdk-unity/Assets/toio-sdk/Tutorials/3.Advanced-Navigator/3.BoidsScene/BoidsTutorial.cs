using UnityEngine;
using toio.Navigation;

namespace toio.tutorial
{
    public class BoidsTutorial : MonoBehaviour
    {
        CubeManager cubeManager;
        bool started = false;

        async void Start()
        {
            cubeManager = new CubeManager();
            await cubeManager.MultiConnect(6);

            // get Cube (3) and Cube (5)
            CubeNavigator navigatorNotBoids = null;
            CubeNavigator navigatorBoids = null;
#if UNITY_EDITOR
            foreach (var navigator in cubeManager.navigators){
                if (navigator.cube.id == "Cube (5)")
                    navigatorNotBoids = navigator;
                else if (navigator.cube.id == "Cube (3)")
                    navigatorBoids = navigator;
            }
#else
            navigatorBoids = cubeManager.navigators[0];
            if (cubeManager.navigators.Count > 1)
                navigatorNotBoids = cubeManager.navigators[1];
#endif

            navigatorBoids.cube.TurnLedOn(0,255,0,0);    // Green
            navigatorNotBoids.cube.TurnLedOn(255,0,0,0); // Red

            // set to BOIDS only mode, except Cube (5) (Red)
            foreach (var navigator in cubeManager.navigators)
                if (navigator != navigatorNotBoids)
                    navigator.mode = CubeNavigator.Mode.BOIDS;

            // By default, all navigators are in one group of boids
            // here, separate Red cube from the group
            navigatorNotBoids.SetRelation(cubeManager.navigators, CubeNavigator.Relation.NONE);
            foreach (var navigator in cubeManager.navigators)
                navigator.SetRelation(navigatorNotBoids, CubeNavigator.Relation.NONE);

            Debug.Log(cubeManager.IsControllable(navigatorBoids.cube));
            started = true;
        }

        void Update()
        {
            if (!started) return;
            // ------ Sync ------
            foreach (var navigator in cubeManager.syncNavigators)
            {
                var mv = navigator.Navi2Target(400, 400, maxSpd:50).Exec();
            }
        }
    }

}
