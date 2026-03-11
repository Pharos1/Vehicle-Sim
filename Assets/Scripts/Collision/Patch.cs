using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEngine.UI.Image;

public class Patch : MonoBehaviour {
	Wheel w;
	Suspension s => w.s;

	Ray originRay;
	[SerializeField] List<Ray> rays = new List<Ray>(); //Rays around origin ray

	//Dimensions
	//float radius; //Radius of patch
	float depth; //Depth of patch
	
	//Averages
	[HideInInspector] public Vector3 point = Vector3.zero;
	[HideInInspector] public Vector3 normal = Vector3.zero;
	[HideInInspector] public Vector3 tractionDir = Vector3.zero;
	[HideInInspector] public Vector3 sideDir = Vector3.zero;

	[Header("Collision")]
	public List<RaycastHit> otherHits = new List<RaycastHit>();

	[HideInInspector] public bool collisionList = false;
	public int layers = 10;
	public int raysCount = 10;
	public bool useBetterCoverage = true; //For better coverage

	[HideInInspector] public RaycastHit hit;
	[HideInInspector] public float closestPointDist;
	public bool grounded;

	float radius => w.radius;
	float width => w.width;
	float halfWidth => w.width / 2;

	public float penetrationAmount = .02f;

	public bool debugRays;

	private void Start() {
		init();
		generate(out rays);
	}

	public void init() {
		w = GetComponent<Wheel>();
		point = w.s.transform.position - w.s.transform.up * (w.s.restLength + radius);
		//.distance = Vector3.Distance(s.transform.position, hit.point);
	}
	public void generate(out List<Ray> rays) {
		rays = new List<Ray>();

		for (int i = 0; i < layers; i++) {
			for (int j = 1; j < raysCount + 1; j++) {
				float layerOffset = i * (width / (layers - 1));
				float rayOffset = j * (radius * 2 / (raysCount + 2 - 1));

				float centerToPointDist;

				if (useBetterCoverage && layers > 1) { //For better coverage
					float offsetDeviation = i * ((radius * 2 / (raysCount + 2 - 1)) / layers) * (i % 2 * -2 + 1);
					rayOffset += offsetDeviation;
				}

				Vector3 origin = Vector3.zero;

				if (layers > 1) {
					origin.x -= halfWidth;
					origin.x += layerOffset;
				}

				if (raysCount > 1) {
					origin.z -= radius; //Offset it to the most back
					origin.z += rayOffset; //Move to front by the calculated amount

					centerToPointDist = Mathf.Sqrt(radius * radius - Mathf.Pow(rayOffset - radius, 2)); //Using Pythagoreas theroem to find the length of the ray based on its position along the wheel.
				}
				else {
					centerToPointDist = radius;
				}

				rays.Add(new Ray(origin, centerToPointDist));
			}
		}
	}
	public void updateCollision() {
		hit = new RaycastHit(); //Set dummy data as to not glitch anything
		hit.point = s.transform.position - s.transform.up * (s.restLength + radius);
		hit.distance = Vector3.Distance(s.transform.position, hit.point);
		hit.normal = Vector3.zero;//transform.up;

		point = normal = tractionDir = sideDir = Vector3.zero;
		otherHits.Clear();
		grounded = false;
		closestPointDist = radius;

		for (int i = 0; i < rays.Count(); i++) {
			Ray ray = rays[i];
			Vector3 origin = s.transform.TransformPoint(ray.origin);

			if (Physics.Raycast(origin, -s.transform.up, out RaycastHit tempHit, Mathf.Clamp(s.springLength, s.minLength, s.maxLength) + ray.centerToPointDist + penetrationAmount, ~(1 << LayerMask.NameToLayer("Car")))) {
				float d1 = tempHit.distance - ray.centerToPointDist;
				float d2 = hit.distance - closestPointDist;

				otherHits.Add(tempHit);
				float penetrationDepth = Mathf.Max(0, (s.springLength + ray.centerToPointDist - tempHit.distance + penetrationAmount) / penetrationAmount);

				normal += penetrationDepth * tempHit.normal; //Contribute percentage of normal
				point += tempHit.point; //Contribute percentage of normal

				if (d1 < d2) {
					hit = tempHit;
					closestPointDist = ray.centerToPointDist;
					//hit.distance += penetrationAmount;

					grounded = true;
				}
			}

			if (debugRays) {
				Debug.DrawRay(origin, -s.transform.up * (s.springLength + ray.centerToPointDist), Color.blue);
				Debug.DrawRay(origin - s.transform.up * (s.springLength + ray.centerToPointDist), -s.transform.up * (s.maxLength + ray.centerToPointDist - (s.springLength + ray.centerToPointDist)), Color.green);
				//Debug.DrawRay(origin, -transform.up * (maxLength + length), Color.yellow);
			}
		}

		normal.Normalize();
		point /= otherHits.Count;

		if (!grounded) {
			normal = s.transform.up;
			point = s.transform.position - s.transform.up * (s.restLength + radius);
			s.springLength = s.restLength;
		}


		tractionDir = Vector3.Cross(normal, -s.transform.right).normalized;
		sideDir = Vector3.Cross(normal, tractionDir).normalized;
	}
}
