using GLTFast.Schema;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

public class Suspension : MonoBehaviour {
	[SerializeField] private Transform car;
	[SerializeField] public Wheel w;

	private Rigidbody rb;
	private Car cc;

	[Header("Suspension")]
	[HideInInspector] public bool suspensionList;
	[HideInInspector] public float Ck = 0.2f;
	[HideInInspector] public float Cd = 0.20f;

	[HideInInspector] public float restLength;
	[HideInInspector] public float springTravel;

	[ReadOnly] [HideInInspector] public float minLength;
	[ReadOnly] [HideInInspector] public float maxLength;
	[ReadOnly] private float lastLength;
	[ReadOnly] [HideInInspector] public float springLength;
	[ReadOnly] private float springForce;
	[ReadOnly] private float damperForce;
	[ReadOnly] private float springVelocity;

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
	}
	void FixedUpdate() {
		minLength = restLength - springTravel;
		maxLength = restLength + springTravel;

		//signedSpeed = Vector3.Dot(w.tractionDir, rb.GetPointVelocity(w.hit.point).normalized) * rb.GetPointVelocity(w.hit.point).magnitude;

		w.collision();
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
		if (!w.isGrounded) return;

		float m1 = rb.mass / 4f;

		//Calculate suspension forces
		lastLength = springLength;
		springLength = w.hit.distance - w.centerHitDist; //When having one raycast centerHitDist = radius
		//TODO: This I dont use as sometimes the wheel would enter the floor, when it the spring reaches its max compression it would  be more right to treat it as a stick that cant compress, but I cant seemm to figure out how to do such a thing
		//springLength = Mathf.Clamp(springLength, minLength, maxLength);
		springVelocity = (springLength - lastLength) / Time.fixedDeltaTime;

		float displacement = springLength - restLength;
		springForce = -(m1 / (Time.fixedDeltaTime * Time.fixedDeltaTime)) * Ck * displacement;
		damperForce = -(m1 / Time.fixedDeltaTime) * Cd * springVelocity;

		suspensionForce = (springForce + damperForce);

		//TODO: Experiment with this one here
		//Vector3 z = transform.forward * Vector3.Dot(transform.forward, w.avgNormal);
		//Vector3 y = transform.up * Vector3.Dot(transform.up, w.avgNormal);
		//Vector3 x = transform.right * Vector3.Dot(transform.right, w.avgNormal);

		//Debug.DrawRay(w.hit.point, Vector3.Normalize(transform.position - w.avgPoint));
		Vector3 dir = w.avgNormal - transform.right * Vector3.Dot(transform.right, w.avgNormal);// transform.forward * Vector3.Dot(transform.forward, w.avgNormal) + transform.up * Vector3.Dot(transform.up,w.avgNormal);
		rb.AddForceAtPosition(dir * Mathf.Max(0, suspensionForce), transform.position);

		if (w.type == Wheel.WheelType.RL || w.type == Wheel.WheelType.RR) {
			//rb.AddForceAtPosition(w.avgNormal * suspensionForce, transform.position);
		}
		//TODO: this should be corrected, like research more and find a more elegant way to solve it
		//To fix force making car go forward/backward depending on rotation of car body
		Vector3 backForce = (w.avgNormal - transform.up) * suspensionForce;
		backForce = Vector3.Dot(transform.forward, backForce) * transform.forward;
		//rb.AddForceAtPosition(backForce, transform.position);
	}
}