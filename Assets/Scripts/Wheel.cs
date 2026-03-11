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
	[HideInInspector] public Rigidbody rb;
	[HideInInspector] public Car cc;
	[HideInInspector] public Transform wModel;
	[HideInInspector] public Patch patch;

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

	[Header("Wheel Physics")]
	[HideInInspector] public bool physicsList = false;
	[HideInInspector] public float mass = 10; //KG
	[HideInInspector] public float Crr = 0.012f;

	[HideInInspector] public Vector2 F; //Lateral(y) and Longitudinal(x) forces
	[HideInInspector] public Vector2 V; //Lateral(y) and Longitudinal(x) velocities

	//Gizmos
	[HideInInspector] public bool gizmosList = false;
	[HideInInspector] public bool debugSize = true;

	[HideInInspector] public float omega = 0;
	[HideInInspector] public float wheelRotationAngle = 0;

	//To be organized
	public Vector2 L = new Vector2(.1f, 0.5f); //Relaxation lengths

	const float engineDragOnThrottle = 0.08f;
	const float engineDragNoThrottle = 0.08f;

	float[] b = new float[] { 1.5f, 0f, 1100, 0, 300, 0, 0, 0, -2, 0, 0, 0, 0, 0 };
	float[] a = new float[] { 1.4f, 0, 1100, 1100, 10, 0, 0, -2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

	private void Start() {
		rb = car.GetComponent<Rigidbody>();
		cc = car.GetComponent<Car>();
		wModel = transform.GetChild(0);
		patch = GetComponent<Patch>();

		transform.SetParent(null, true); //Make the wheels not moved with their parents

		Time.fixedDeltaTime = 0.01f;
	}
	private void Update() {
		if (!float.IsNaN(V.x)) {
			transform.position = s.transform.position - s.transform.up * s.springLength;
			transform.rotation = s.transform.rotation;

			wModel.localRotation = Quaternion.Euler(wheelRotationAngle * Mathf.Rad2Deg, 0, 0);
		}
	}

	public void steer(float steerAngle) {
		wheelAngle = Mathf.Lerp(wheelAngle, steerAngle, steerTime * Time.deltaTime);
		s.transform.localRotation = Quaternion.Euler(Vector3.up * wheelAngle);
	}
	public void updateDynamics() {
		//Prerequisite variables
		Vector3 velocityWorld = rb.GetPointVelocity(patch.point);

		V.x = Vector3.Dot(velocityWorld, patch.tractionDir);
		V.y = Vector3.Dot(velocityWorld, patch.sideDir);

		float V_low = .1f;
		float clampedVx = Mathf.Max(Mathf.Abs(V.x), V_low);

		float G = cc.gearRatios[cc.curGear] * cc.diffRatio;
		float Fz = Mathf.Max(0, s.suspensionForce) / 1000; //kN

		//-For geometric torque
		Vector3 wheelAxleWorldPosition = s.transform.position - s.transform.up * s.springLength;
		Vector3 leverArm = patch.point - wheelAxleWorldPosition;
		Vector3 surfaceTorqueVec = Vector3.Cross(leverArm, patch.normal * s.suspensionForce);

		float R_eff = Vector3.Distance(wheelAxleWorldPosition, patch.point);

		//Forces involved
		float T_rr = 0;
		float T_brake = 0;

		if (patch.grounded) {
			T_rr = Crr * s.suspensionForce * Mathf.Sign(V.x) * R_eff;
		}
		
		if (isWheelBraking && Input.GetKey(KeyCode.Space)) {
			T_brake = Mathf.Sign(V.x) * MaxTbraking;
		}
		
		//TODO: I need to make it so only the powering wheels should be accounted for

		//-Dampening/Friction
		float C_wheel = .3f; // Nm * s / rad
		float C_engine = Mathf.Lerp(engineDragNoThrottle, engineDragOnThrottle, Mathf.Abs(Input.GetAxis("Vertical")));//TODO: Can be more fine tuned, currently the values are garabage
		float C_eff = C_wheel + (C_engine * Mathf.Pow(G, 2));

		//Torques
		float T_engine = cc.Tengine * Input.GetAxis("Vertical");
		float T_engineToWheel = T_engine * G * cc.transmissionEfficiency * (isWheelPowering ? 1 : 0);
		float T_feedback = F.x * R_eff;
		float T_damping = C_eff * omega; //Axle Damping Torque/Friction
		float T_geometric = Vector3.Dot(surfaceTorqueVec, s.transform.right);

		float T_net = T_engineToWheel - T_brake - T_feedback - T_damping - T_rr + T_geometric;

		//Inertias TODO: Implement realistic axle torque damping coefficient with dampening ratios
		float I_wheel = 2.35f;
		float I_engine = .32f;
		float I_eff = I_wheel + (I_engine * Mathf.Pow(G, 2));
		
		//---Slips
		float torqueToStop = (omega * I_eff) / Time.fixedDeltaTime;
		if ((Mathf.Abs(T_brake) > Mathf.Abs(torqueToStop)) && Input.GetKey(KeyCode.Space)) {
			omega = 0;
		}

		float angularAccel = T_net / I_eff;

		//Omega
		RK4.Derivative omegaODE = (t, currentOmega) => angularAccel;
		omega = RK4.Integrate(omegaODE, Time.time, omega, Time.fixedDeltaTime);

		RK4.Derivative angleODE = (t, currentAngle) => omega;
		wheelRotationAngle = RK4.Integrate(angleODE, Time.time, wheelRotationAngle, Time.fixedDeltaTime); //In Rad
		wheelRotationAngle %= (2f * Mathf.PI); //Keep angle at sane ranges to avoid floating point errors
				
		//Slips
		float kappa = ((R_eff * omega - V.x) / clampedVx); // MF Expects percentages.
		float alpha = Mathf.Rad2Deg * -Mathf.Atan2(V.y, clampedVx);
		float gamma = 0; // Degrees
		
		//Friction Eclipse/Slip normalization
		float kappa_peak = .08f; //Percent
		float alpha_peak = 3.2f; //Deg
		float kappa_norm = kappa / kappa_peak;
		float alpha_norm = alpha / alpha_peak;

		float S_a = Mathf.Sqrt(Mathf.Pow(kappa_norm, 2) + Mathf.Pow(alpha_norm, 2));

		//TODO: Maybe self aligning force Mz
		F.x = PacejkaFx(Fz, S_a * kappa_peak, b) * kappa_norm / S_a;
		F.y = PacejkaFy(Fz, S_a * alpha_peak, gamma, a) * (alpha_norm / S_a);

		if (!patch.grounded || S_a < 0.001f || Fz < 0.01f) {
			F = Vector2.zero;
		}

		if (V.sqrMagnitude < 1f) {
			float lowSpeedScalar = Mathf.Max(0.1f, Mathf.Sqrt(V.magnitude));
			F *= lowSpeedScalar;
		}

		Vector3 totalForce = F.x * patch.tractionDir + F.y * patch.sideDir + s.suspensionForce * patch.normal;

		rb.AddForceAtPosition(totalForce, patch.point);
	}

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
	
	private void OnDrawGizmos() {
		if (debugSize) {
			Handles.color = Color.yellow;
			Handles.DrawWireDisc(s.transform.position - s.transform.up * s.springLength + s.transform.right * halfWidth, s.transform.right, radius);
			Handles.DrawWireDisc(s.transform.position - s.transform.up * s.springLength - s.transform.right * halfWidth, s.transform.right, radius);
		}

		if (EditorApplication.isPlaying) {
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(patch.point, .02f);
			
			Gizmos.color = Color.red;
			foreach (RaycastHit _ in patch.otherHits) {
				Gizmos.DrawWireSphere(_.point, .01f);
			}
		}
	}
}
