using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public class Wheel : MonoBehaviour {
	public Transform car;
	public Suspension s;
	private Rigidbody rb;
	private CarController cc;

	public enum WheelType {
		FL,
		FR,
		RL,
		RR
	}

	[Header("Wheel Parameters")]
	[SerializeField] private bool debugSize = true;
	[SerializeField] public float radius;
	[SerializeField] public float width; private float halfWidth => width / 2;

	[Header("Wheel Properties")]
	[SerializeField] public WheelType type;

	[SerializeField] public bool isWheelPowering = true;
	[SerializeField] public bool isWheelBreaking = true;

	[SerializeField] private float Cbraking = 2000f; //Breaking Coefficent. Fine tune if needed.

	[Header("Collision")]
	[SerializeField] private bool debugRays;
	[SerializeField] private int layers = 10;
	[SerializeField] private int rays = 10;
	[SerializeField][Tooltip("Gives better coverage in higher number of rays and layers.")] private bool useBetterCoverage = true; //For better coverage

	[HideInInspector] public RaycastHit hit;
	[HideInInspector] public float centerHitDist;
	[SerializeField][ReadOnly] public bool grounded;

	[Header("Steering")]
	[SerializeField] public float steerTime = 8f;

	[HideInInspector] public float steerAngle;
	private float wheelAngle;

	[Header("Wheel Physics")]
	[SerializeField] public float mass = 10; //KG
	[SerializeField] private float Crr = 0.012f;

	[HideInInspector] public Vector3 tractionDir;
	[HideInInspector] public Vector3 sideDir;

	//To be organized
	float rotationPitch = 0;
	public Vector3 totalForce;
	[HideInInspector] public Vector3 wheelVelocityLS; //Local Space
	private float Fy;
	public float sideFriction = 30;

	[HideInInspector] public Vector3 vLong;

	private void Start() {
		rb = car.GetComponent<Rigidbody>();
		cc = car.GetComponent<CarController>();

		hit.point = s.transform.position - s.transform.up * (s.maxLength + radius);

		transform.SetParent(null, true); //Make the wheels not moved with their parents
	}
	private void Update() {
		if (!float.IsNaN(s.signedSpeed)) {
			float wheelOmega = s.signedSpeed / radius;

			rotationPitch += Mathf.Rad2Deg * wheelOmega * Time.deltaTime;
			rotationPitch = Mathf.Repeat(rotationPitch, 360f);


			transform.SetPositionAndRotation(s.transform.position - s.transform.up * s.springLength, s.transform.rotation * Quaternion.Euler(rotationPitch, 0, 0));
		}
	}
	private void FixedUpdate() {
		vLong = Vector3.Dot(tractionDir, rb.velocity) * tractionDir;
	}
	public void steer() {
		wheelAngle = Mathf.Lerp(wheelAngle, steerAngle, steerTime * Time.deltaTime);
		s.transform.localRotation = Quaternion.Euler(Vector3.up * wheelAngle);
	}
	private void OnDrawGizmos() {
		if (debugSize) {
			Handles.color = Color.yellow;
			Handles.DrawWireDisc(s.transform.position - s.transform.up * s.springLength + s.transform.right * halfWidth, s.transform.right, radius);
			Handles.DrawWireDisc(s.transform.position - s.transform.up * s.springLength - s.transform.right * halfWidth, s.transform.right, radius);
		}

		Gizmos.color = Color.red;
		Gizmos.DrawSphere(hit.point, .05f);
	}
	public void collision() {
		hit = new RaycastHit(); //Set dummy data as to not glitch anything

		hit.point = s.transform.position - s.transform.up * (s.restLength + radius);
		hit.distance = Vector3.Distance(s.transform.position, hit.point);
		hit.normal = Vector3.zero;//transform.up;

		centerHitDist = radius;
		grounded = false;

		for (int i = 0; i < layers; i++) {
			for (int j = 1; j < rays + 1; j++) { //The wonky + 1s and + 2s for the rays count is because when using, say, 10 rays, 2 of them are to the most left and right and their length is 0, so what I do here is ignore the side ones and just use the middle 10 rays
				float layerOffset = i * (width / (layers - 1));
				float rayOffset = j * (radius * 2 / (rays + 2 - 1));

				float length = radius;


				if (useBetterCoverage && layers > 1) { //For better coverage
					float offsetDeviation = i * ((radius * 2 / (rays + 2 - 1)) / layers) * (i % 2 * -2 + 1);
					rayOffset += offsetDeviation;
				}

				Vector3 origin = s.transform.position;

				if (layers > 1) {
					origin -= s.transform.right * halfWidth;
					origin += s.transform.right * layerOffset;
				}

				if (rays > 1) { //To prevent division by zero and do nothing if it is indeed 1
					origin -= s.transform.forward * radius; //Offset it to the most back
					origin += s.transform.forward * rayOffset; //Move to front by the calculated amount

					length = Mathf.Sqrt(radius * radius - Mathf.Pow(rayOffset - radius, 2)); //Using Pythagoreas theroem to find the length of the ray based on its position along the wheel.
				}

				if (debugRays) {
					Debug.DrawRay(origin, -s.transform.up * (s.springLength + length), Color.blue);
					Debug.DrawRay(origin - s.transform.up * (s.springLength + length), -s.transform.up * (s.maxLength + length - (s.springLength + length)), Color.green);
					//Debug.DrawRay(origin, -transform.up * (maxLength + length), Color.yellow);
				}

				if (Physics.Raycast(origin, -s.transform.up, out RaycastHit tempHit, s.maxLength + length, ~(1 << LayerMask.NameToLayer("Car")))) {
					float d1 = tempHit.distance - length;
					float d2 = hit.distance - centerHitDist;

					if (d1 < d2) {
						hit = tempHit;
						centerHitDist = length;
						grounded = true;
					}
				}
			}
		}
	}
	public void calcAndApplyForces() {
		totalForce = Vector3.zero;

		tractionDir = Vector3.Cross(hit.normal, -s.transform.right).normalized;//Quaternion.AngleAxis(90, transform.right) * contact.Value.normal;
		sideDir = Vector3.Cross(hit.normal, s.transform.forward).normalized;

		Debug.DrawRay(hit.point, tractionDir, Color.blue);
		Debug.DrawRay(hit.point, sideDir, Color.magenta);

		//-Side friction or smth
		if (grounded) {
			//wheelVelocityLS = transform.InverseTransformDirection(rb.GetPointVelocity(hit.point));

			//Fy = wheelVelocityLS.x * sideFriction;
			Fy = Vector3.Dot(sideDir, rb.GetPointVelocity(hit.point)) * 90f * sideFriction; //To degrees for correct representation of Slip Angle


			totalForce += Fy * -sideDir;
		}
		else {
			wheelVelocityLS = Vector3.zero;
		}
		//Applying Forces
		//-Drive Force
		if (isWheelPowering && grounded) {
			Vector3 Fdrive = tractionDir * cc.Tdrive / radius;

			totalForce += Fdrive;
		}

		//-Rolling Resistance
		if (grounded) {
			float normalForce = s.Ws * -Physics.gravity.y;
			Vector3 Frr = Crr * normalForce * -vLong.normalized;

			totalForce += Frr;
		}

		//-Breaking Force
		if (isWheelBreaking && grounded && Input.GetKey(KeyCode.Space)) { //} && !ApproximatelyEquals(0, velInTractionDir.magnitude, 0.01f)) {
			Vector3 Fbraking = -vLong.normalized * Cbraking;

			float velIncrease = Cbraking * Time.fixedDeltaTime / rb.mass;

			if ((vLong.magnitude - velIncrease) < 0.01f) {
				Fbraking = Vector3.zero;
			}

			totalForce += Fbraking;
		}

		rb.AddForceAtPosition(totalForce, hit.point);
		//Debug.Log(rb.GetAccumulatedForce());
		//Debug.Log(rb.GetAccumulatedTorque());
	}
}
