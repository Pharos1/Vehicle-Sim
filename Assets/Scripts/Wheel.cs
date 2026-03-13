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

	[Header("Dynamics Settings")]
	public float minVelocityThreshold = 0.1f;
	public float wheelInertia = 2.35f;
	public float engineInertia = 0.32f;
	public float wheelDamping = 0.3f;
	public float peakKappa = 0.07f;
	public float peakAlpha = 10.21f;
	const float engineDragOnThrottle = 0.08f;
	const float engineDragNoThrottle = 0.08f;

	float[] b = new float[] { 1.5f, 0f, 1100, 0, 300, 0, 0, 0, -2, 0, 0, 0, 0, 0 };
	float[] a = new float[] { 1.4f, 0, 1100, 1100, 10, 0, 0, -2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };


	// TODO: For proper geometric torque and wheels not being calculated in isolation, I need to link the axles where
	// wheels contribute omega/torques to the axis and them being linked, the axle will tell them what omega to use.

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

	public void Steer(float steerAngle) {
		wheelAngle = Mathf.Lerp(wheelAngle, steerAngle, steerTime * Time.deltaTime);
		s.transform.localRotation = Quaternion.Euler(Vector3.up * wheelAngle);
	}
	public void UpdateDynamics() {
		// 1. Early Exit for Airborne or Unloaded Wheels
		float Fz = Mathf.Max(0, s.suspensionForce) / 1000f; // kN
		if (!patch.grounded || Fz < 0.01f) {
			ResetAirborneForces();
			return;
		}

		// 2. Velocities & Basic State
		Vector3 velocityWorld = rb.GetPointVelocity(patch.point);
		Vector2 velocityLocal = new Vector2(
			Vector3.Dot(velocityWorld, patch.tractionDir),
			Vector3.Dot(velocityWorld, patch.sideDir)
		);

		float clampedVx = Mathf.Max(Mathf.Abs(velocityLocal.x), minVelocityThreshold);
		float gearRatio = cc.gearRatios[cc.curGear] * cc.diffRatio;

		Vector3 axleWorldPos = s.transform.position - s.transform.up * s.springLength;
		float effectiveRadius = Vector3.Distance(axleWorldPos, patch.point);

		// 3. Calculate Torques
		float netTorque = CalculateNetTorque(velocityLocal.x, effectiveRadius, gearRatio, Fz, axleWorldPos);

		// 4. Update Angular Velocity (Omega)
		UpdateWheelRotation(velocityLocal.x, netTorque, gearRatio);

		// 5. Calculate Slips
		Vector2 normalizedSlips = CalculateNormalizedSlips(velocityLocal, clampedVx, effectiveRadius);

		// 6. Calculate Pacejka Forces
		F = CalculateTireForces(Fz, normalizedSlips);
		ApplyLowSpeedDamping(ref F, velocityLocal);

		// 7. Apply to Rigidbody
		Vector3 totalForce = (F.x * patch.tractionDir) +
							 (F.y * patch.sideDir) +
							 (s.suspensionForce * patch.normal);

		rb.AddForceAtPosition(totalForce, patch.point);
	}

	// --- Helper Methods ---
	private float CalculateNetTorque(float vx, float R_eff, float gearRatio, float Fz, Vector3 axleWorldPos) {
		float T_rr = Crr * (Fz * 1000f) * Mathf.Sign(vx) * R_eff;
		float T_brake = CalculateBrakeTorque(vx);

		float engineDrag = Mathf.Lerp(engineDragNoThrottle, engineDragOnThrottle, Mathf.Abs(Input.GetAxis("Vertical")));
		float effectiveDamping = wheelDamping + (engineDrag * gearRatio * gearRatio) / cc.numOfDriveWheels;

		float T_engineToWheel = cc.Tengine * Input.GetAxis("Vertical") * gearRatio * cc.transmissionEfficiency * (isWheelPowering ? 1 : 0);
		float T_feedback = F.x * R_eff; // F.x is cached from the previous frame's Pacejka
		float T_damping = effectiveDamping * omega;

		// Geometric Torque: I recommend scaling this down (e.g., * 0.1f) if wheels keep fighting each other.
		Vector3 leverArm = patch.point - axleWorldPos;
		Vector3 surfaceTorqueVec = Vector3.Cross(leverArm, patch.normal * (Fz * 1000f));
		float T_geometric = Vector3.Dot(surfaceTorqueVec, s.transform.right);

		return T_engineToWheel - T_brake - T_feedback - T_damping - T_rr;// + T_geometric;
	}
	private float CalculateBrakeTorque(float vx) {
		float T_brake = 0f;

		if (isWheelBraking && Input.GetKey(KeyCode.Space)) {
			T_brake = Mathf.Sign(vx) * MaxTbraking;
			if (Mathf.Abs(vx) < 0.1f) T_brake = 0;
			else if (Mathf.Abs(vx) < 1f) T_brake *= Mathf.Sqrt(Mathf.Abs(vx));
		}

		return T_brake;
	}
	private void UpdateWheelRotation(float vx, float netTorque, float gearRatio) {
		float effectiveInertia = wheelInertia + (engineInertia * gearRatio * gearRatio) / cc.numOfDriveWheels;

		// Simple Euler integration is usually sufficient and much faster than RK4 for wheel rotation
		float angularAccel = netTorque / effectiveInertia;

		// Brake lock logic
		float torqueToStop = (omega * effectiveInertia) / Time.fixedDeltaTime;
		if (Input.GetKey(KeyCode.Space) && isWheelBraking && Mathf.Abs(CalculateBrakeTorque(vx)) > Mathf.Abs(torqueToStop)) {
			omega = 0;
			angularAccel = 0;
		}

		omega += angularAccel * Time.fixedDeltaTime;

		wheelRotationAngle += omega * Time.fixedDeltaTime;
		wheelRotationAngle %= (2f * Mathf.PI);
	}
	private Vector2 CalculateNormalizedSlips(Vector2 velocityLocal, float clampedVx, float R_eff) {
		// Longitudinal Slip (Kappa)
		// Formula: \kappa = \frac{R_{eff} \cdot \omega - V_x}{V_{clamped}}
		float kappa = (R_eff * omega - velocityLocal.x) / clampedVx;
		DD.DisplayFloat(kappa);

		// Lateral Slip Angle (Alpha)
		float alpha = Mathf.Rad2Deg * -Mathf.Atan2(velocityLocal.y, clampedVx);

		return new Vector2(kappa / peakKappa, alpha / peakAlpha);
	}
	private Vector2 CalculateTireForces(float Fz, Vector2 normalizedSlips) {
		// Combined slip scalar: S_a = \sqrt{\kappa_{norm}^2 + \alpha_{norm}^2}
		float slipMagnitude = Mathf.Sqrt(normalizedSlips.x * normalizedSlips.x + normalizedSlips.y * normalizedSlips.y);

		if (slipMagnitude < 0.001f) return Vector2.zero;

		float gamma = 0f; // Camber angle

		float fx = PacejkaFx(Fz, slipMagnitude * peakKappa, b) * (normalizedSlips.x / slipMagnitude);
		float fy = PacejkaFy(Fz, slipMagnitude * peakAlpha, gamma, a) * (normalizedSlips.y / slipMagnitude);

		return new Vector2(fx, fy);
	}
	private void ApplyLowSpeedDamping(ref Vector2 forces, Vector2 velocityLocal) {
		if (velocityLocal.sqrMagnitude < 1f) {
			float lowSpeedScalar = Mathf.Max(0.1f, Mathf.Sqrt(velocityLocal.magnitude));
			forces *= lowSpeedScalar;
		}
	}
	private void ResetAirborneForces() {
		F = Vector2.zero;
		// Allow the wheel to slowly spin down when in the air
		omega = Mathf.Lerp(omega, 0, Time.fixedDeltaTime * 2f);
		wheelRotationAngle += omega * Time.fixedDeltaTime;
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
