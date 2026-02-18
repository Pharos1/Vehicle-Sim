using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using System;
using Unity.Profiling;

public class Wheel : MonoBehaviour {
	public Transform car;
	public Suspension s;
	private Rigidbody rb;
	private Car cc;

	public enum WheelType {
		FL,
		FR,
		RL,
		RR
	}

    [Header("Steering")]
    [HideInInspector] public float steerTime = 8f;

    [HideInInspector] public float steerAngle;
    [HideInInspector] private float wheelAngle;

    [Header("Wheel Dimensions")]
    [SerializeField] public float radius;
	[SerializeField] public float width; private float halfWidth => width / 2;

	[Header("Wheel Config")]
    [HideInInspector] public bool configList = false;
    [HideInInspector] public WheelType type;

    [HideInInspector] public bool isWheelPowering = true;
	[HideInInspector] public bool isWheelBraking = true;

    [HideInInspector] public float Cbraking = 2000f; //Breaking Coefficent. Fine tune if needed.

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

    [HideInInspector] public Vector3 vLong; //Longitudinal velocity

    [HideInInspector] public Vector2 F; //Lateral(x) and Longitudinal(y) forces
    [HideInInspector] public Vector2 V; //Lateral(x) and Longitudinal(y) velocities

    //Gizmos
    [HideInInspector] public bool gizmosList = false;
    [HideInInspector] public bool debugSize = true;
    [HideInInspector] public bool debugRays;

    //To be organized
    float rotationPitch = 0;

	//Collision to be moved to editor
	public float penetrationAmount = .02f;
	List<RaycastHit> otherHits = new List<RaycastHit>();
	[HideInInspector] public Vector3 avgNormal;
	[HideInInspector] public Vector3 avgPoint;



    [HideInInspector] public float omega = 0;
    [HideInInspector] public float wheelRotationAngle = 0;
    float slipRatio = 0;

    public float internalDampCoeff = .25f;


    private void Start() {
		rb = car.GetComponent<Rigidbody>();
		cc = car.GetComponent<Car>();

		transform.SetParent(null, true); //Make the wheels not moved with their parents
	}
	private void Update() {
		if (!float.IsNaN(s.signedSpeed)) {
			rotationPitch = wheelRotationAngle;//Mathf.Rad2Deg * omega * Time.deltaTime;

            rotationPitch = Mathf.Repeat(rotationPitch, 360f);

			transform.SetPositionAndRotation(s.transform.position - s.transform.up * s.springLength/*Mathf.Clamp(s.springLength, s.minLength, s.maxLength)*/, s.transform.rotation * Quaternion.Euler(rotationPitch, 0, 0));
		}
	}
	private void FixedUpdate() {
        vLong = Vector3.Dot(tractionDir, rb.velocity) * tractionDir;

		Vector3 vLongLS = s.transform.InverseTransformVector(vLong);
        V.x = vLongLS.x;
        V.y = vLongLS.z;
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
        Gizmos.DrawSphere(hit.point, .02f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(avgPoint, .02f);

        Gizmos.color = Color.red;
        foreach (RaycastHit _ in otherHits) {
			Gizmos.DrawWireSphere(_.point, .01f);
		}
	}
	public void collision() {
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
    float Fx = 0;
    float Fy = 0;
    [SerializeField] float denomSlider = 128;
	public void calcAndApplyForces() {
		F = Vector2.zero;
		
		tractionDir = Vector3.Cross(avgNormal, -s.transform.right).normalized;//Quaternion.AngleAxis(90, transform.right) * contact.Value.normal;
		sideDir = Vector3.Cross(avgNormal, s.transform.forward).normalized;

		//Debug.DrawRay(transform.position - s.transform.up * (radius - penetrationAmount), tractionDir, Color.red);
		//Debug.DrawRay(transform.position - s.transform.up * (radius - penetrationAmount), sideDir, Color.magenta);

		float sideFric = 0;
		float Fdrive = 0, Frr = 0, Fbraking = 0;

		//-Side friction or smth
		if (isGrounded) {
            sideFric = -Vector3.Dot(sideDir, rb.GetPointVelocity(hit.point)) * 90f * Cs;
		}

		//Applying Forces
		//-Drive Force
		if (isWheelPowering && isGrounded) {
			Fdrive = cc.Tdrive / radius;
		}

		//-Rolling Resistance
		if (isGrounded) {
			float normalForce = s.suspensionForce; //Suspension force is the force applied on the ground by the wheel. Acording to Newton's Third Law of Motion.
            Frr = Crr * normalForce * -Mathf.Sign(V.y);
		}

		//-Breaking Force
		if (isWheelBraking && isGrounded && Input.GetKey(KeyCode.Space)) { //} && !ApproximatelyEquals(0, velInTractionDir.magnitude, 0.01f)) {
            //This is to prevent oscillation at near zero velocity
            //When near stopping makes Fbraking converge to zero at a square root rate.
			//When below certain speed(e.g .1f) then stop braking completely.
            Fbraking = -Mathf.Sign(V.y) * Cbraking;

            if (Mathf.Abs(V.y) < .1f)
                Fbraking = 0;
            else if (Mathf.Abs(V.y) < 1f)
				Fbraking *= Mathf.Sqrt(Mathf.Abs(V.y));
        }

		F.x += sideFric;
		F.y += Fdrive + Frr + Fbraking;
        
        //F.y  -= Fdrive;
        //rb.AddForceAtPosition(s.transform.TransformVector(new Vector3(F.x, 0, F.y)), avgPoint);



        //Hopefully Last Try ;(
        if (!isGrounded) {
            return;
        }
        Vector3 velocityWorld = rb.GetPointVelocity(avgPoint);
        Vector3 velocityLocal = transform.InverseTransformDirection(velocityWorld);
        float Vx = velocityLocal.z; 
        float Vy = velocityLocal.x;




        float G = cc.gearRatios[cc.curGear] * cc.diffRatio; //Total Gear Ratio

        float V_low = .3f;

        float Fz = Mathf.Max(0, s.suspensionForce);

        float T_engine = cc.Tengine * Input.GetAxis("Vertical");
        float T_brake = Fbraking * radius;
        float T_damp = internalDampCoeff * omega;


        float I_wheel = 2.35f;
        float I_engine = .32f;
        float I_eff = I_wheel + (I_engine * G * G);

        float netTorque = T_engine * G + T_brake - (Fx * radius) - T_damp;

        omega += (netTorque / I_eff) * Time.fixedDeltaTime;
        wheelRotationAngle += omega * Mathf.Rad2Deg * Time.fixedDeltaTime;

        float kappa = (radius * omega - Vx) / Mathf.Max(V_low, Mathf.Abs(Vx));

        float[] b = new float[] { 1.5f, 0f, 1100, 0, 300, 0, 0, 0, -2, 0, 0, 0, 0, 0 };
        Fx = PacejkaFx(Fz / 1000, kappa, b);


        float alpha = Mathf.Rad2Deg * -Mathf.Atan(Vy / Mathf.Max(V_low, Mathf.Abs(Vx)));
        float gamma = 0; //TODO: Camber is zero, could be changed

        float[] a = new float[] { 1.4f, 0, 1100, 1100, 10, 0, 0, -2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
        Fy = PacejkaFy(Fz / 1000, alpha, gamma, a);

        //Vector3 forceWorld = (tractionDir * Fx_comb) + (transform.right * Fy_comb);
        //rb.AddForceAtPosition(forceWorld, avgPoint);

        rb.AddForceAtPosition(Fx * tractionDir, avgPoint);
        rb.AddForceAtPosition(Fy * sideDir, avgPoint);
        Debug.Log(Fy);
        //Debug.Log("Start: " + V_low + " " + Fz + " " + netTorque + " " + omega + " " + wheelRotationAngle + " " + kappa);


        return;
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
        //Debug.Log(b[0]);
        float F = D * Mathf.Sin(C * Mathf.Atan(Bx1 - E * (Bx1 - Mathf.Atan(Bx1)))) + V;
        //Debug.Log(F);
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
        //Debug.Log(b[0]);
        float F = D * Mathf.Sin(C * Mathf.Atan(Bx1 - E * (Bx1 - Mathf.Atan(Bx1)))) + V;
        //Debug.Log(F);
        return F;
    }
    /*
    //if (!isGrounded) {
    //    return;
    //}

    //Pacejka MF
    omega = (float.IsNaN(omega) ? 0 : omega);
    slipRatio = (float.IsNaN(slipRatio) ? 0 : slipRatio);

    float I = mass * (radius * radius);

    float T_net = cc.Tdrive + Fx * radius + Fbraking * radius;

    omega += (T_net / I) * Time.fixedDeltaTime;
    float vWheel = omega * radius;

    float vLong = Vector3.Dot(rb.GetPointVelocity(avgPoint), tractionDir);
    float denom = vLong;//Mathf.Max(Mathf.Abs(vLong), Mathf.Abs(vWheel), 0.5f);

    float kappa = (vWheel - vLong) / denom; //Slip Ratio
    Fx = cc.Tdrive; //(float)pacejkaFx(new double[] { 1.65, 0.0, 1688.0, 0.0, 229.0, 0.0, 0.0, 0.0, -10.0, 0.0, 0.0, 0.0, 0.0 }, s.suspensionForce, kappa) / denomSlider;////////   


    //Vector3 tireForce = (Fx + Frr) * tractionDir + F.x * sideDir;
    if (isGrounded) {
        //rb.AddForceAtPosition(((cc.Tdrive * radius) - Fx + Frr + ) * tractionDir, avgPoint);
        //rb.AddForceAtPosition(T_net * tractionDir, avgPoint);  
        //rb.AddForceAtPosition(F.x * sideDir, avgPoint);
    }



    //MF


    //F_long *= Input.GetAxis("Vertical");
    //if (cc.Tdrive == 0) {
    //    F_long *= 0;
    //}
    //T_reaction += ((F.y * radius) - cc.Tdrive);

    //float bearingFrictionTorque = -0.1f * omega;



    //float vGround = Vector3.Dot(transform.forward, rb.GetPointVelocity(avgPoint));
    //slipRatio = (vWheel - vGround) / Mathf.Abs(vGround);
    //slipRatio = Mathf.Clamp(slipRatio, -2.5f, 2.5f);
    if (type == Wheel.WheelType.FR) {
        //Debug.Log("T_reaction" + T_reaction);
        Debug.Log("T_drive: " + cc.Tdrive);
        Debug.Log("V w:" + vWheel);
        Debug.Log("V g:" + V.y);
        Debug.Log("SR:" + kappa);
        Debug.Log("Omega: " + omega);
        Debug.Log("Fx: " + Fx);
    }

    //rb.AddForceAtPosition(tractionDir * (F_long + F.y), avgPoint);
    //rb.AddForceAtPosition(sideDir * (F.x), avgPoint);
}
// Pacejka params (example starting values; you must tune)
public float mu = 1.0f;            // surface friction coefficient
public float B_long = 10.0f;
public float C_long = 1.9f;
public float E_long = 0.97f;

public float B_lat = 8.0f;
public float C_lat = 2.0f;
public float E_lat = 1.0f;

// small epsilon for stability
const float EPS = 1e-4f;

// Magic Formula core
float Pacejka(float B, float C, float D, float E, float x) {
    // MF(x) = D * sin( C * atan( B*x - E*(B*x - atan(B*x)) ) )
    float bx = B * x;
    float a = Mathf.Atan(bx);
    float inner = bx - E * (bx - a);
    return D * Mathf.Sin(C * Mathf.Atan(inner));
}

// compute longitudinal force (positive forwards)
public float GetLongitudinalForce(float kappa, float Fz) {
    float D = mu * Fz; // peak proportional to normal load
    float Fx0 = Pacejka(B_long, C_long, D, E_long, kappa);
    return Fx0;
}

// compute lateral force (positive to the left of forward vector)
public float GetLateralForce(float alphaRad, float Fz) {
    float D = mu * Fz;
    float Fy0 = Pacejka(B_lat, C_lat, D, E_lat, alphaRad);
    return Fy0;
}

public double pacejkaFx(double[] b, double Fz, double slipRatio) {  //Fz is load
    double C = b[0];
    double D = (b[1] * Fz + b[2]) * Fz;
    double B = ((b[3] * Math.Pow(Fz, 2) + b[4] * Fz) * Math.Exp(-b[5] * Fz)) / (C * D);
    double E = b[6] * Math.Pow(Fz, 2) + b[7] * Fz + b[8];
    double Sh = b[9] * Fz + b[10];
    double Sv = 0;

    double Fx = D * Math.Sin(C * Math.Atan(B * (1 - E) * (slipRatio + Sh) + E * Math.Atan(B * (slipRatio + Sh)))) + Sv;
    return Fx;
}
    */
}
