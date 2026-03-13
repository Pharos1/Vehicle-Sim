using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
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
	[HideInInspector] public RaycastHit hit;
	[HideInInspector] public float closestPointDist;
	public bool grounded;

	float radius => w.radius;
	float width => w.width;
	float halfWidth => w.width / 2;


	public bool debugRays;

	[Header("Patch Settings")]
	public float rayDensity = 20f; // Multiplier for how many rays to pack in
	public float activeContactAngle = 90f; // Max angle of the patch (e.g., 90 deg = -45 to +45)
	[Range(1f, 3f)] public float groundConcentration = 1.5f; // 1 = uniform square, >1 = packed at the bottom
	public float penetrationAmount = .02f;
	public bool useBetterCoverage = true;

	bool initialized;

	private void Start() {
		init();
		generate(out rays);
	}

	public void init() {
		w = GetComponent<Wheel>();
		point = w.s.transform.position - w.s.transform.up * (w.s.restLength + radius);

		initialized = true;
	}
	public void generate(out List<Ray> rays) {
		rays = new List<Ray>();

		float maxZLength = radius * Mathf.Sin(activeContactAngle * 0.5f * Mathf.Deg2Rad);

		int cols = Mathf.Max(1, Mathf.RoundToInt(width * rayDensity));
		int rows = Mathf.Max(1, Mathf.RoundToInt(maxZLength * 2f * rayDensity));

		float colSpacing = cols > 1 ? width / (cols - 1) : 0;
		float rowSpacing = rows > 1 ? (maxZLength * 2f) / (rows - 1) : 0;

		for (int i = 0; i < cols; i++) {
			for (int j = 0; j < rows; j++) {
				float xBase = (cols > 1) ? (-width / 2f) + (i * colSpacing) : 0f;
				float t = (rows > 1) ? ((float)j / (rows - 1)) * 2f - 1f : 0f;

				float tBiased = Mathf.Sign(t) * Mathf.Pow(Mathf.Abs(t), groundConcentration);
				float zBase = tBiased * maxZLength;

				// FIX: Alternating offsets (+0.25 and -0.25) ensure the mathematical average Z remains exactly 0.
				if (useBetterCoverage && cols > 1) { // TODO: Better coverage still makes the wheel rotate.
					float offset = (rowSpacing * 0.25f) * (i % 2 == 0 ? 1f : -1f);
					zBase += offset;
				}

				Vector3 origin = new Vector3(xBase, 0, zBase);

				float zClamped = Mathf.Clamp(zBase, -radius, radius);
				float centerToPointDist = Mathf.Sqrt(radius * radius - (zClamped * zClamped));

				rays.Add(new Ray(origin, centerToPointDist));
			}
		}
	}
	public void updateCollision() {
		hit = new RaycastHit();
		hit.point = s.transform.position - s.transform.up * (s.restLength + radius);
		hit.distance = Vector3.Distance(s.transform.position, hit.point);
		hit.normal = s.transform.up; // Fallback

		point = normal = tractionDir = sideDir = Vector3.zero;
		otherHits.Clear();
		grounded = false;
		closestPointDist = radius;

		float totalWeight = 0f;
		int layerMask = ~(1 << LayerMask.NameToLayer("Car"));

		foreach (Ray ray in rays) {
			Vector3 worldOrigin = s.transform.TransformPoint(ray.origin);

			// Use the current spring length dictated by your suspension script
			float castDistance = Mathf.Clamp(s.springLength, s.minLength, s.maxLength) + ray.centerToPointDist + penetrationAmount;

			if (Physics.Raycast(worldOrigin, -s.transform.up, out RaycastHit tempHit, castDistance, layerMask)) {

				float d1 = tempHit.distance - ray.centerToPointDist;
				float d2 = hit.distance - closestPointDist;

				// Calculate penetration relative to current spring state
				float penetrationDepth = Mathf.Max(0, (s.springLength + ray.centerToPointDist + penetrationAmount - tempHit.distance) / penetrationAmount);

				if (penetrationDepth > 0) {
					float weight = penetrationDepth * penetrationDepth;

					normal += tempHit.normal * weight;
					point += tempHit.point * weight;
					totalWeight += weight;

					otherHits.Add(tempHit);
				}

				// Update main hit point if this is the deepest contact
				if (d1 < d2 || !grounded) {
					hit = tempHit;
					closestPointDist = ray.centerToPointDist;
					grounded = true;
				}
			}

			if (debugRays) {
				Debug.DrawRay(worldOrigin, -s.transform.up * castDistance, Color.blue);
			}
		}

		if (grounded && totalWeight > 0) {
			normal.Normalize();
			point /= totalWeight; // Complete the weighted average
		}
		else {
			// Only reset the spring length if the wheel is entirely airborne
			normal = s.transform.up;
			point = s.transform.position - s.transform.up * (s.restLength + radius);
			s.springLength = s.restLength;
		}

		tractionDir = Vector3.Cross(normal, -s.transform.right).normalized;
		sideDir = Vector3.Cross(normal, tractionDir).normalized;

		Debug.DrawRay(point, normal);
	}
	private void OnValidate() {
		if (initialized) {
			generate(out rays);
		}
	}
}
