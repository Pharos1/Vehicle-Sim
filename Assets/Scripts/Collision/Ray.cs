using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Ray {
	public Vector3 origin;
	public float centerToPointDist;


	//Old WIP Ideas
	public float lenToOrigin; //Length to origin ray of Patch.cs
	public float lenToRad; //Length to radius

	public Ray(Vector3 origin, float centerToPointDist) {
		this.origin = origin;
		this.centerToPointDist = centerToPointDist;
	}
}