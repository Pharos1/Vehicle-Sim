using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

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

    private void Start() {
		rb = car.GetComponent<Rigidbody>();
		cc = car.GetComponent<Car>();

		transform.SetParent(null, true); //Make the wheels not moved with their parents
	}
	private void Update() {
		if (!float.IsNaN(s.signedSpeed)) {
			float wheelOmega = s.signedSpeed / radius;

			rotationPitch += Mathf.Rad2Deg * wheelOmega * Time.deltaTime;
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
	public void calcAndApplyForces() {
		F = Vector2.zero;
		
		tractionDir = Vector3.Cross(avgNormal, -s.transform.right).normalized;//Quaternion.AngleAxis(90, transform.right) * contact.Value.normal;
		sideDir = Vector3.Cross(avgNormal, s.transform.forward).normalized;

		//Debug.DrawRay(transform.position - s.transform.up * (radius - penetrationAmount), tractionDir, Color.red);
		//Debug.DrawRay(transform.position - s.transform.up * (radius - penetrationAmount), sideDir, Color.magenta);

		//-Side friction or smth
		if (isGrounded) {
            F.x += -Vector3.Dot(sideDir, rb.GetPointVelocity(hit.point)) * 90f * Cs;
		}

		//Applying Forces
		//-Drive Force
		if (isWheelPowering && isGrounded) {
			float Fdrive = cc.Tdrive / radius;

			F.y += Fdrive;
		}

		//-Rolling Resistance
		if (isGrounded) {
			float normalForce = s.suspensionForce; //Suspension force is the force applied on the ground by the wheel. Acording to Newton's Third Law of Motion.
            float Frr = Crr * normalForce * -Mathf.Sign(V.y);

			F.y += Frr;
		}

		//-Breaking Force
		if (isWheelBraking && isGrounded && Input.GetKey(KeyCode.Space)) { //} && !ApproximatelyEquals(0, velInTractionDir.magnitude, 0.01f)) {
			float Fbraking = -Mathf.Sign(V.y) * Cbraking;

            //This is to prevent oscillation at near zero velocity
            //When near stopping makes Fbraking converge to zero at a square root rate.
			//When below certain speed(e.g .1f) then stop braking completely.
			if (Mathf.Abs(V.y) > 1f) F.y += Fbraking; 
            if (Mathf.Abs(V.y) < 1f && Mathf.Abs(V.y) > .1f) F.y += Fbraking * Mathf.Sqrt(Mathf.Abs(V.y));
        }

		rb.AddForceAtPosition(s.transform.TransformVector(new Vector3(F.x, 0, F.y)), avgPoint);
	}
}
