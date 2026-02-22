using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using System;
using Unity.Profiling;
using UnityEngine.AI;

public class Wheel : MonoBehaviour {
	public Transform car;
	public Suspension s;
	private Rigidbody rb;
	private Car cc;
	private Transform wModel;

	public enum WheelType {
		FL,
		FR,
		RL,
		RR
	}

	[Header("Steering")]
	[HideInInspector] public float steerTime = 8f;
	[HideInInspector] private float wheelAngle;

	[Header("Wheel Dimensions")]
	[SerializeField] public float radius;
	[SerializeField] public float width; private float halfWidth => width / 2;

	[Header("Wheel Config")]
	[HideInInspector] public bool configList = false;
	[HideInInspector] public WheelType type;

	[HideInInspector] public bool isWheelPowering = true;
	[HideInInspector] public bool isWheelBraking = true;

	[HideInInspector] public float MaxTbraking = 2000f; //Max Breaking Torque. Fine tune if needed.

	[Header("Collision")]
	[HideInInspector] public bool collisionList = false;
	[HideInInspector] public int layers = 10;
	[HideInInspector] public int rays = 10;
	[HideInInspector] public bool useBetterCoverage = true; //For better coverage

	[HideInInspector] public RaycastHit hit;
	[HideInInspector] public float centerHitDist;
	[HideInInspector] public bool isGrounded;

	[Header("Wheel Physics")]
	[HideInInspector] public bool physicsList = false;
	[HideInInspector] public float mass = 10; //KG
	[HideInInspector] public float Crr = 0.012f;
	[HideInInspector] public float Cs = 30; //Side friction coeff

	[HideInInspector] public Vector3 tractionDir;
	[HideInInspector] public Vector3 sideDir;

	[HideInInspector] public Vector2 F; //Lateral(y) and Longitudinal(x) forces
	[HideInInspector] public Vector2 V; //Lateral(y) and Longitudinal(x) velocities

	//Gizmos
	[HideInInspector] public bool gizmosList = false;
	[HideInInspector] public bool debugSize = true;
	[HideInInspector] public bool debugRays;

	//To be organized
	float rotationPitch = 0;
	float kappaEff = 0;
	float alphaEff = 0;

	//Collision to be moved to editor
	public float penetrationAmount = .02f;
	List<RaycastHit> otherHits = new List<RaycastHit>();
	[HideInInspector] public Vector3 avgNormal;
	[HideInInspector] public Vector3 avgPoint;

	[HideInInspector] public float omega = 0;
	[HideInInspector] public float wheelRotationAngle = 0;

	public float internalDampCoeff = .25f;


	private void Start() {
		rb = car.GetComponent<Rigidbody>();
		cc = car.GetComponent<Car>();
		wModel = transform.GetChild(0);


		transform.SetParent(null, true); //Make the wheels not moved with their parents
	}
	private void Update() {
		if (!float.IsNaN(V.x)) {
			rotationPitch = wheelRotationAngle * Mathf.Rad2Deg;//Mathf.Rad2Deg * omega * Time.deltaTime;

			rotationPitch = Mathf.Repeat(rotationPitch, 360f);


			//TODO: I dont like all of this code, it's kinda based on guess & try but it seems to work
			//A big concern for me is that I set wModel local rot, which I believe could be affected by the scale of the model
			transform.position = s.transform.position - s.transform.up * s.springLength;
			transform.rotation = s.transform.rotation;

			wModel.localRotation = Quaternion.Euler(rotationPitch, 0, 0);
			//transform.SetPositionAndRotation(s.transform.position - s.transform.up * s.springLength/*Mathf.Clamp(s.springLength, s.minLength, s.maxLength)*/, s.transform.rotation * Quaternion.Euler(rotationPitch, 0, 0));
		}
	}
	private void FixedUpdate() {}
	public void steer(float steerAngle) {
		wheelAngle = Mathf.Lerp(wheelAngle, steerAngle, steerTime * Time.deltaTime);
		s.transform.localRotation = Quaternion.Euler(Vector3.up * wheelAngle);
	}

	public void collision() {
		//TODO: this will be reworked
		hit = new RaycastHit(); //Set dummy data as to not glitch anything
		hit.point = s.transform.position - s.transform.up * (s.restLength + radius);
		hit.distance = Vector3.Distance(s.transform.position, hit.point);
		hit.normal = Vector3.zero;//transform.up;

		otherHits.Clear();
		avgNormal = Vector3.zero;
		avgPoint = Vector3.zero;


		centerHitDist = radius;
		isGrounded = false;

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

				if (Physics.Raycast(origin, -s.transform.up, out RaycastHit tempHit, Mathf.Clamp(s.springLength, s.minLength, s.maxLength) + length + penetrationAmount, ~(1 << LayerMask.NameToLayer("Car")))) {
					float d1 = tempHit.distance - length;
					float d2 = hit.distance - centerHitDist;

					otherHits.Add(tempHit);
					float penetrationDepth = Mathf.Max(0, (s.springLength + length - tempHit.distance + penetrationAmount) / penetrationAmount);

					avgNormal += penetrationDepth * tempHit.normal; //Contribute percentage of normal
					avgPoint += tempHit.point; //Contribute percentage of normal

					if (d1 < d2) {
						hit = tempHit;
						centerHitDist = length;
						//hit.distance += penetrationAmount;
						isGrounded = true;
					}
				}

			}
		}

		avgNormal = avgNormal.normalized;
		avgPoint /= otherHits.Count;

		//TODO: Note: this is to remove the x dir of the normal, this should not be used combined with pacejka, this is because I dont have such an advanced model at the time
		//avgNormal -= Vector3.Dot(s.transform.right, avgNormal) * s.transform.right;

		if (!isGrounded) {
			avgNormal = s.transform.up;
			avgPoint = s.transform.position - s.transform.up * (s.restLength + radius);
			s.springLength = s.restLength;
		}
	}

	float oldKappa = 0;
	//float kappa = 0;
	float kappaDiff = 0;

	public void calcAndApplyForces() {
		F = Vector2.zero;


		tractionDir = Vector3.Cross(avgNormal, -s.transform.right).normalized;//Quaternion.AngleAxis(90, transform.right) * contact.Value.normal;
		sideDir = Vector3.Cross(avgNormal, s.transform.forward).normalized;

		//Vel calc
		Vector3 velocityWorld = rb.GetPointVelocity(avgPoint);
		Vector3 velocityLocal = transform.InverseTransformDirection(velocityWorld);
		V.x = velocityLocal.z;
		V.y = velocityLocal.x;

		//Debug.DrawRay(transform.position - s.transform.up * (radius - penetrationAmount), tractionDir, Color.red);
		//Debug.DrawRay(transform.position - s.transform.up * (radius - penetrationAmount), sideDir, Color.magenta);

		float Fdrive = 0, Frr = 0, Tbraking = 0;

		//Applying Forces
		//-Drive Force
		if (isWheelPowering && isGrounded) {
			Fdrive = cc.Tdrive / radius;
		}

		//-Rolling Resistance
		if (isGrounded) {
			float normalForce = s.suspensionForce; //Suspension force is the force applied on the ground by the wheel. Acording to Newton's Third Law of Motion.
			Frr = Crr * normalForce * Mathf.Sign(V.x);
		}
		
		//-Breaking Force
		if (isWheelBraking && isGrounded && Input.GetKey(KeyCode.Space)) { //} && !ApproximatelyEquals(0, velInTractionDir.magnitude, 0.01f)) {
			//This is to prevent oscillation at near zero velocity
			//When near stopping makes Fbraking converge to zero at a square root rate.
			//When below certain speed(e.g .1f) then stop braking completely.
			Tbraking = Mathf.Sign(V.x) * MaxTbraking;

			if (Mathf.Abs(V.x) < .1f)
				Tbraking = 0;
			else if (Mathf.Abs(V.x) < 1f)
				Tbraking *= Mathf.Sqrt(Mathf.Abs(V.x));
		}
		
		//TODO: I need to make it so only the powering gears should be accounted for

		float G = cc.gearRatios[cc.curGear] * cc.diffRatio; //Total Gear Ratio TODO: should see if gear eff should be accounted for
		float V_low = .2f;
		float Fz = Mathf.Max(0, s.suspensionForce) / 1000; //kN

		//-Dampening/Friction
		float C_wheel = .3f; // Nm * s / rad
		const float engineDragOnThrottle = 0.08f;
		const float engineDragNoThrottle = 0.15f;
		float C_engine = Mathf.Lerp(engineDragNoThrottle, engineDragOnThrottle, Mathf.Abs(Input.GetAxis("Vertical")));//TODO: Can be more fine tuned, currently the values are garabage
		float C_eff = C_wheel + (C_engine * Mathf.Pow(G, 2));

		//Torques
		float T_engine = cc.Tengine * Input.GetAxis("Vertical");
		float T_engineToWheel = T_engine * G * cc.transmissionEfficiency;
		float T_brake = Tbraking;
		float T_feedback = F.x * radius;
		float T_axle = C_eff * omega; //Axle Damping Torque/Friction

		//Inertias TODO: Implement realistic axle torque damping coefficient with dampening ratios
		float I_wheel = 2.35f;
		float I_engine = .32f;
		float I_eff = I_wheel + (I_engine * Mathf.Pow(G, 2));


		//Pacejka Prep
		float netTorque = T_engineToWheel - T_brake - T_feedback - T_axle - Frr * radius;//T_engine * G + T_brake - T_mf - C_eff;
		float angularAccel = netTorque / I_eff;

		omega += angularAccel * Time.fixedDeltaTime;
		wheelRotationAngle += omega * Time.fixedDeltaTime; //In rad
		
		//Quick fix TODO
		float threshold = 1f;
		if (isWheelBraking && Input.GetKey(KeyCode.Space) && Mathf.Abs(omega) < threshold) {
			omega = 0;
		}

		//Slips
		float kappa = ((radius * omega - V.x) / Mathf.Max(V_low, Mathf.Abs(V.x))) * 100; // MF Expects percentages.
		float alpha = Mathf.Rad2Deg * -Mathf.Atan2(V.y, Mathf.Max(V_low, Mathf.Abs(V.x))); // Degrees
		float gamma = 0; //TODO: Camber is zero, could be changed // Degrees

		float sigma_peak = 8f; //Percent
		float alpha_peak = 3.2f; //Deg
		float sigma_norm = kappa / sigma_peak; 
		float alpha_norm = alpha / alpha_peak;

		float S_a = Mathf.Sqrt(Mathf.Pow(sigma_norm, 2) + Mathf.Pow(alpha_norm, 2));

		//Debug.Log(V + " | alpha: " + alpha + " | kappa: " + kappa + " | EffKappa: " + kappaEff + " | omega: " + omega);
		float[] b = new float[] { 1.5f, 0f, 1100, 0, 300, 0, 0, 0, -2, 0, 0, 0, 0, 0 };
		float[] a = new float[] { 1.4f, 0, 1100, 1100, 10, 0, 0, -2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
		
		//TODO: Maybe self aligning force Mz
		F.x = PacejkaFx(Fz, S_a * sigma_peak, b) * (sigma_norm / S_a);
		F.y = PacejkaFy(Fz, S_a * alpha_peak, gamma, a) * (alpha_norm / S_a);

		
		if (!isGrounded) {
			F = Vector2.zero;
		}

		if (V.magnitude < 1) {
			float lowSpeedScalar = Mathf.Sqrt(V.magnitude);
			F *= lowSpeedScalar;
		}

		rb.AddForceAtPosition(F.x * tractionDir, avgPoint);
		rb.AddForceAtPosition(F.y * sideDir, avgPoint);
	}
	//TODO: aligning moment
	float PacejkaFx(float Fz, float kappa, float[] b) {
		float C = b[0];
		float D = Fz * (b[1] * Fz + b[2]);
		float BCD = (b[3] * Fz * Fz + b[4] * Fz) * Mathf.Exp(-b[5] * Fz);
		float B = BCD / (C * D);
		float H = b[9] * Fz + b[10];
		float E = (b[6] * Fz * Fz + b[7] * Fz + b[8]) * (1 - b[13] * Mathf.Sign(kappa + H));
		float V = b[11] * Fz + b[12];
		float Bx1 = B * (kappa + H); //Composite
		float F = D * Mathf.Sin(C * Mathf.Atan(Bx1 - E * (Bx1 - Mathf.Atan(Bx1)))) + V;

		return F;
	}
	float PacejkaFy(float Fz, float alpha, float gamma, float[] a) {
		float C = a[0];
		float D = Fz * (a[1] * Fz + a[2]) * (1 - a[15] * gamma * gamma);
		float BCD = a[3] * Mathf.Sin(Mathf.Atan(Fz / a[4]) * 2) * (1 - a[5] * Mathf.Abs(gamma));
		float B = BCD / (C * D);
		float H = a[8] * Fz + a[9] + a[10] * gamma;
		float E = (a[6] * Fz + a[7]) * (1 - (a[16] * gamma + a[17]) * Mathf.Sign(alpha + H));
		float V = a[11] * Fz + a[12] + (a[13] * Fz + a[14]) * gamma * Fz;
		float Bx1 = B * (alpha + H); //Composite
		float F = D * Mathf.Sin(C * Mathf.Atan(Bx1 - E * (Bx1 - Mathf.Atan(Bx1)))) + V;

		return F;
	}

	bool ApproximatelyEquals(float a, float b, float epsilon) {
		if (Math.Abs(a - b) < epsilon) {
			return true;
		}
		else {
			return false;
		}
	}

	private void OnDrawGizmos() {
		if (debugSize) {
			Handles.color = Color.yellow;
			Handles.DrawWireDisc(s.transform.position - s.transform.up * s.springLength + s.transform.right * halfWidth, s.transform.right, radius);
			Handles.DrawWireDisc(s.transform.position - s.transform.up * s.springLength - s.transform.right * halfWidth, s.transform.right, radius);
		}

		Gizmos.color = Color.red;
		Gizmos.DrawSphere(hit.point, .02f);

		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(avgPoint, .02f);

		Gizmos.color = Color.red;
		foreach (RaycastHit _ in otherHits) {
			Gizmos.DrawWireSphere(_.point, .01f);
		}
	}
}
