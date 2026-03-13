using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Car : MonoBehaviour {
	private Rigidbody rb;

	//Wheels & Suspension
	[SerializeField] public Wheel[] wheels;
	[HideInInspector] public Suspension[] suspensions;
	[HideInInspector] public int numOfDriveWheels;

	[SerializeField] public float kerbWeight = 1460f;
	[SerializeField] private float wheelBase; //All are in meters
	[SerializeField] private float rearTrack;
	[SerializeField] private float turnRadius;

	//Car Physics
	[HideInInspector] public bool physicsList = false;
	[HideInInspector] public float dragCoeff = 0.29f;
	[HideInInspector] public float frontalArea = 1.85806f;
	[HideInInspector] public float airDensity = 1.225f;
	
	[HideInInspector] public Vector3 Fdrag;

	[HideInInspector] public float Tengine = 0;

	//Car State
	[HideInInspector] public int curGear = 1;

	//Transmission Config
	[HideInInspector] public bool transmissionList = false;
	[HideInInspector] public float[] gearRatios; //0 - reverse, others - as shown
	[HideInInspector] public float diffRatio = 3.42f; //Synonymous to final drive ratio
	[HideInInspector] public float transmissionEfficiency = .85f;

	//Engine Config/State
	[HideInInspector] public bool engineList = false;
	[HideInInspector] public AnimationCurve torqueCurve;
	[HideInInspector] public float rpm;

	//Steering
	private float ackermannAngleLeft;
	private float ackermannAngleRight;

	[HideInInspector] public Vector2 V;

	//Gizmos
	[HideInInspector] public bool gizmosList = false;
	[HideInInspector] public bool debugCG = true;

	[SerializeField] public float antiRoll = .7f;

	private void Start() {
		rb = transform.GetComponent<Rigidbody>();

		suspensions = new Suspension[wheels.Length];

		for (int i = 0; i < wheels.Length; i++) {
			suspensions[i] = wheels[i].s;
		}

		//Drive Wheels counting
		numOfDriveWheels = 0;
		foreach (Wheel w in wheels) {
			if (w.isWheelPowering) numOfDriveWheels++;
		}

		//Calculate mass based on total mass and the masses of the wheels
		rb.mass = kerbWeight;
		foreach (Wheel w in wheels) {
			rb.mass -= w.mass;
		}
	}
	void Update() {
		input();
	}
	void FixedUpdate() {
		//TODO: clear this some more, like everything is inside fixedUpdate, also this has to be redone, things like velinWheelDir are to be changed
		//Prerequisite variables
		//-Gears
		float gearRatio = gearRatios[curGear];

		//Air Resistance
		Fdrag = -0.5f * dragCoeff * frontalArea * airDensity * (rb.velocity * rb.velocity.magnitude);
		//rb.AddForceAtPosition(Fdrag, rb.position); //TODO: apply drag to the Center of Pressure aka aerodynamic center

		//TODO rpm should be calculated by the avg of the powering wheels, currently it is hardcoded to be the front wheels
		float wheelOmega = ((wheels[0].omega + wheels[1].omega) / 2);
		rpm = wheelOmega * gearRatio * diffRatio * (60 / (2 * Mathf.PI)); /*To Convert from rad/s to rpm */
		
		rpm = Mathf.Abs(rpm);
		rpm = Mathf.Clamp(rpm, 700, 8000); //If rpm is zero then the car will never start. If too high just doesn't make sense

		//Torque of Engine
		Tengine = LookupTorqueCurve(rpm);

		//if (numOfDriveWheels != 0)
		//	Tengine /= numOfDriveWheels; //Torque is divided between wheels
		//else
		//	Tengine = 0;

		Vector3 velocityWorld = rb.velocity;
		Vector3 velocityLocal = transform.InverseTransformDirection(velocityWorld);
		V.x = velocityLocal.z;
		V.y = velocityLocal.x;

		//antiRollBars(wheels[0], wheels[1]); //Also commented out inside the func
		//antiRollBars(wheels[2], wheels[3]);

		steer();
	}
	private float LookupTorqueCurve(float rpm) { //In Nm
		return torqueCurve.Evaluate(rpm);
	}
	private void input() {
		//Gear Changing
		if (Input.GetKeyDown(KeyCode.UpArrow) && curGear < gearRatios.Length - 1) curGear++;
		if (Input.GetKeyDown(KeyCode.DownArrow) && curGear > 0) curGear--;

		//Reset car position and orientation
		if (Input.GetKeyDown(KeyCode.R)) {
			
			transform.position += new Vector3(0, 2, 0);
			transform.rotation = Quaternion.identity;
		}
	}
	private void steer() {
		float steerInput = Input.GetAxis("Horizontal");

		if (steerInput > 0) { //Turing right
			ackermannAngleLeft = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius + rearTrack / 2)) * steerInput;
			ackermannAngleRight = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius - rearTrack / 2)) * steerInput;
		}
		else if (steerInput < 0) { //Turing left
			ackermannAngleLeft = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius - rearTrack / 2)) * steerInput;
			ackermannAngleRight = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius + rearTrack / 2)) * steerInput;
		}
		else {
			ackermannAngleLeft = 0;
			ackermannAngleRight = 0;
		}

		foreach (Wheel w in wheels) {
			if (w.type == Wheel.WheelType.FL) {
				w.Steer(ackermannAngleLeft);
			}
			else if (w.type == Wheel.WheelType.FR) {
				w.Steer(ackermannAngleRight);
			}
		}
	}
	private void antiRollBars(Wheel wheelL, Wheel wheelR) {
		//Anti roll bars
		float travelL;
		float travelR;

		//if (wheelL.isGrounded)
			travelL = (wheelL.s.springLength - wheelL.s.restLength) / (wheelL.s.springTravel);
		//else
		//    travelL = 1f;
		//if (wheelR.isGrounded)
			travelR = (wheelR.s.springLength - wheelR.s.restLength) / (wheelR.s.springTravel);
		//else
		//    travelR = 1f;

		var antiRollForce = (travelL - travelR) * antiRoll * ((rb.mass / 4f) / (Time.fixedDeltaTime * Time.fixedDeltaTime)) * ((wheelL.s.Ck + wheelR.s.Ck) / 2) * ((wheelL.s.springTravel + wheelR.s.springTravel) / 2);// * antiRoll;// * -(rb.mass / (Time.fixedDeltaTime * Time.fixedDeltaTime) * wheelL.s.Ck);


		//if (wheelL.isGrounded)
			//rb.AddForceAtPosition(wheelL.s.transform.up * -antiRollForce, wheelL.s.transform.position);
		//if (wheelR.isGrounded)
			//rb.AddForceAtPosition(wheelR.s.transform.up * antiRollForce, wheelR.s.transform.position);
	}
	private void OnDrawGizmos() {
		if (debugCG) {
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(GetComponent<Rigidbody>().worldCenterOfMass, .2f);
		}
	}
}
