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

	[HideInInspector] public float signedSpeed;


	[HideInInspector] public float sWs; //Static Weight on Suspension
	[HideInInspector] public float Ws; //Static Weight on Suspension
	
	SolutionVector state;
	public float tau = 1;

	void Start() {
		w.s = this;
		w.car = car;

		rb = car.GetComponent<Rigidbody>();
		cc = car.GetComponent<Car>();

		springLength = restLength;

		minLength = restLength - springTravel;
		maxLength = restLength + springTravel;
	}
	private void Update() {
		w.steer();
	}
	void FixedUpdate() {
		minLength = restLength - springTravel;
		maxLength = restLength + springTravel;

		signedSpeed = Vector3.Dot(w.tractionDir, rb.GetPointVelocity(w.hit.point).normalized) * rb.GetPointVelocity(w.hit.point).magnitude;

		float accel = (Vector3.Dot(w.tractionDir, rb.GetAccumulatedForce()) / rb.mass);
		cc.calculateWeightDistribution(accel);

		sWs = w.type == Wheel.WheelType.FL || w.type == Wheel.WheelType.FR ? cc.sWf / 2f : cc.sWr / 2f;
		Ws = w.type == Wheel.WheelType.FL || w.type == Wheel.WheelType.FR ? cc.Wf / 2f : cc.Wr / 2f;
		
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
		//s.zmos.color = Color.red;
		//Gizmos.DrawWireSphere(transform.position + transform.up * -springLength, radius);
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
	private void calcDerivatives(SolutionVector x0, out SolutionVector dxdt) {
		dxdt = new SolutionVector();
		dxdt.sLength = (springLength - lastLength) / Time.fixedDeltaTime;

	}
	private void integrateRK4(float deltaTime) {
		SolutionVector k1, k2, k3, k4;
		SolutionVector x;








		// Runge-Kutta 4 integration     
		calcDerivatives(state, out k1);
		x = state;
		x.add(k1, 0.5f * deltaTime);
		calcDerivatives(x, out k2);
		x = state;
		x.add(k2, 0.5f * deltaTime);
		calcDerivatives(x, out k3);
		x = state;
		x.add(k3, deltaTime);
		calcDerivatives(x, out k4);

		state.add(k1, deltaTime / 6.0f);
		state.add(k2, deltaTime / 3.0f);
		state.add(k3, deltaTime / 3.0f);
		state.add(k4, deltaTime / 6.0f);
	}
}