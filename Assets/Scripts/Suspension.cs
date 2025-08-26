using GLTFast.Schema;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

public class Suspension : MonoBehaviour {
	[Header("Components")]
	[SerializeField] private Transform car;
	[SerializeField] public Wheel w;
	private Rigidbody rb;
	private CarController cc;

	[Header("Suspension")]
	[SerializeField][Range(0, 1f)] private float Ck = 0.05f;
	[SerializeField][Range(0, 1f)] private float Cd = 0.20f;

	[SerializeField] public float restLength;
	[SerializeField] private float springTravel;

	[ReadOnly] private float minLength;
	[ReadOnly] public float maxLength;
	[ReadOnly] private float lastLength;
	[ReadOnly] public float springLength;
	[ReadOnly] private float springForce;
	[ReadOnly] private float damperForce;
	[ReadOnly] private float springVelocity;

	[ReadOnly] private float suspensionForce;



	public float signedSpeed;


    public float sWs; //Static Weight on Suspension
    public float Ws; //Static Weight on Suspension

    void Start() {
        w.s = this;
        w.car = car;

        rb = car.GetComponent<Rigidbody>();
		cc = car.GetComponent<CarController>();

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
	private void suspension() {
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

		//To fix force making car go forward/backward depending on rotation of car body
		Vector3 backForce = (w.hit.normal - transform.up) * suspensionForce * ((m1 + m2) / m2);
		backForce = Vector3.Dot(transform.forward, backForce) * transform.forward;
		rb.AddForceAtPosition(backForce, transform.position);
	}
}