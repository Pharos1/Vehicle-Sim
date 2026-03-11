using GLTFast.Schema;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

public class Suspension : MonoBehaviour {
	[SerializeField] private Transform car;
	[SerializeField] public Wheel w;
	[HideInInspector] private Patch patch => w.patch;
	private Rigidbody rb;
	private Car cc;

	[Header("Suspension")]
	[HideInInspector] public bool suspensionList;
	[HideInInspector] public float Ck = 0.2f;
	[HideInInspector] public float Cd = 0.20f;
	private float maxCk;
	private float maxCd;


	[HideInInspector] public float restLength;
	[HideInInspector] public float springTravel;

	[ReadOnly] [HideInInspector] public float minLength;
	[ReadOnly] [HideInInspector] public float maxLength;
	[ReadOnly] private float lastLength;
	[ReadOnly] [HideInInspector] public float springLength;
	[ReadOnly] private float springForce;
	[ReadOnly] private float damperForce;

	[ReadOnly] [HideInInspector] public float suspensionForce;

	[HideInInspector] public float sWs; //Static Weight on Suspension
	[HideInInspector] public float Ws; //Static Weight on Suspension

	void Start() {
		w.s = this;
		w.car = car;

		rb = car.GetComponent<Rigidbody>();
		cc = car.GetComponent<Car>();

		springLength = restLength;

		minLength = restLength - springTravel;
		maxLength = restLength + springTravel;


		float m1 = rb.mass / 4f;
		maxCk = (m1 / (Time.fixedDeltaTime * Time.fixedDeltaTime)); //In N/m
		maxCd = (m1 / Time.fixedDeltaTime); //In N * s / m

	}
	void FixedUpdate() {
		minLength = restLength - springTravel;
		maxLength = restLength + springTravel;

		//signedSpeed = Vector3.Dot(w.tractionDir, rb.GetPointVelocity(w.hit.point).normalized) * rb.GetPointVelocity(w.hit.point).magnitude;

		patch.updateCollision();
		suspension();
		w.calcAndApplyForces();
	}
	private void OnDrawGizmos() {
#if UNITY_EDITOR
		//Debug.Log("Vector3" + rb.transform.InverseTransformPoint(Tools.handlePosition));
		// Only run this when *not* in play mode
		if (!EditorApplication.isPlaying) {
			springLength = restLength;
		}
#endif

		Gizmos.color = Color.green;
		Gizmos.DrawLine(transform.position, transform.position + transform.up * -springLength);
	}
	private void suspension() { //Based on h4tt3n's math https://www.gamedev.net/tutorials/programming/math-and-physics/towards-a-simpler-stiffer-and-more-stable-spring-r3227/
		suspensionForce = 0;

		if (!patch.grounded) return;

		//Calculate suspension forces
		lastLength = springLength;
		springLength = patch.hit.distance - w.patch.closestPointDist; //When having one raycast centerHitDist = radius

		//TODO: This I dont use as sometimes the wheel would enter the floor, when it the spring reaches its max compression it would  be more right to treat it as a stick that cant compress, but I cant seemm to figure out how to do such a thing
		//springLength = Mathf.Clamp(springLength, minLength, maxLength);

		float springVelocity = (springLength - lastLength) / Time.fixedDeltaTime;
		float displacement = springLength - restLength;

		springForce = -maxCk * Ck * displacement;
		damperForce = -maxCd * Cd * springVelocity;

		suspensionForce = (springForce + damperForce);

		Vector3 tractionDirLS = transform.InverseTransformDirection(w.patch.tractionDir);
		//rb.AddForceAtPosition(w.avgNormal *  Mathf.Max(0, suspensionForce), transform.position);
		//DD.DisplayVector(tractionDirLS);

		//Debug.DrawRay(patch.point, patch.normal);
		float Fg = -suspensionForce * w.patch.tractionDir.y;

		//rb.AddForceAtPosition(w.tractionDir * Fg, transform.position);
	}
}