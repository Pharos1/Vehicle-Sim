using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Ray {
	public Vector3 origin;
	public float centerToPointDist;
	
	public Ray(Vector3 origin, float centerToPointDist) {
		this.origin = origin;
		this.centerToPointDist = centerToPointDist;
	}
}