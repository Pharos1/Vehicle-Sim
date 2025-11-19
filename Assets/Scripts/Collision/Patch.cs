using UnityEngine;

public class Patch : MonoBehaviour {
    Ray originRay; 
    Ray[] rays; //Rays around origin ray

    //Dimensions
    float radius; //Radius of patch
    float depth; //Depth of patch

    Vector3 normal; //Avg normal
    Vector3 forwardDir;
    Vector3 sideDir;
    //This is still WIP, is stopped so I can program the RK4 integrator
    //void generate(Ray originRay, out Ray[] rays) {

       //rays = new Ray[];
//    }
}
