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
	[HideInInspector] public bool showSuspensionList;
	[HideInInspector] public float Ck = 0.2f;
	[HideInInspector] public float Cd = 0.20f;

	[HideInInspector] public float restLength;
	[HideInInspector] public float springTravel;

	[ReadOnly] private float minLength;
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
        if (!w.grounded) return;

        float m1 = rb.mass / 4f;
		float m2 = w.mass;

		//Calculate suspension forces
		float mred = (m1 * m2) / (m1 + m2); //Reduced mass

		lastLength = springLength;

        springLength = w.hit.distance - w.centerHitDist; //When having one raycast centerHitDist = radius
		//springLength = Mathf.Clamp(springLength, minLength, maxLength);
		springVelocity = (springLength - lastLength) / Time.fixedDeltaTime;

		float displacement = springLength - restLength;
		springForce = -(mred / (Time.fixedDeltaTime * Time.fixedDeltaTime)) * Ck * displacement;
		damperForce = -(mred / Time.fixedDeltaTime) * Cd * springVelocity;

		suspensionForce = (springForce + damperForce);

		//TODO: Experiment with this one here
		rb.AddForceAtPosition(transform.up * suspensionForce * (m1 + m2) / m2, transform.position);

		//TODO: this should be corrected, like research more and find a more elegant way to solve it
		//To fix force making car go forward/backward depending on rotation of car body
		Vector3 backForce = (w.hit.normal - transform.up) * suspensionForce * ((m1 + m2) / m2);
		backForce = Vector3.Dot(transform.forward, backForce) * transform.forward;
		rb.AddForceAtPosition(backForce, transform.position);
	}
}