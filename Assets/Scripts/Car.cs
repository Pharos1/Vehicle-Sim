using System.Runtime.CompilerServices;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Car : MonoBehaviour {
    private Rigidbody rb;

    [Header("Car Physics")]
    [HideInInspector] public float sWf; //Static weight front
    [HideInInspector] public float sWr;

    [HideInInspector] public float Wf;
    [HideInInspector] public float Wr;

    [Header("Drag")]
    [HideInInspector] public Vector3 Fdrag;

    [SerializeField] private float dragCoeff = 0.29f;
    [SerializeField] private float frontalArea = 1.85806f;
    [SerializeField] private float airDensity = 1.225f;


    [Header("Wheels")]
    [SerializeField] public Wheel[] wheels;
    private Suspension[] suspensions;

    [HideInInspector] public int numOfDriveWheels;

    //Car Config
    [Header("Car State")]
    [SerializeField] public int curGear = 1;

    [Header("Transmission Config")]
    [SerializeField] public float[] gearRatios; //0 - reverse, all other are as shown in inspector
    [SerializeField] public float diffRatio = 3.42f;
    [SerializeField] public float transmissionEfficiency = .7f; //Guess from Marco Monster

    //Constants
    [Header("Engine Config")]
    [SerializeField] public AnimationCurve torqueCurve;

    [HideInInspector] public float Tdrive;

    [Header("Car Specs")]
    [SerializeField] public float kerbWeight = 1460f;
    [SerializeField] private float wheelBase; //All are in meters
    [SerializeField] private float rearTrack;
    [SerializeField] private float turnRadius;

    //Steering
    private float ackermannAngleLeft;
    private float ackermannAngleRight;
    
    //To be ordered
    float v0 = 0;

    [HideInInspector] public float rpm;
    [HideInInspector] public Vector2 V;

    [SerializeField] private bool debugCG = true;

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
        steer();
    }
    void FixedUpdate() {
        Vector3 vLong = Vector3.Dot(transform.forward, rb.velocity) * transform.forward;

        Vector3 vLongLS = transform.InverseTransformVector(vLong);
        V.x = vLongLS.x;
        V.y = vLongLS.z;

        //TODO: clear this some more, like everything is inside fixedUpdate, also this has to be redone, things like velinWheelDir are to be changed
        //Prerequisite variables
        //-Gears
        float gearRatio = gearRatios[curGear];

        //Air Resistance
        Fdrag = -0.5f * dragCoeff * frontalArea * airDensity * (rb.velocity * rb.velocity.magnitude);
        rb.AddForceAtPosition(Fdrag, rb.position); //This is in the center. Not realistic but good enough.

        //Calculating RPM
        float velInWheelDir = Vector3.Dot(rb.velocity, wheels[0].tractionDir);
        rpm = velInWheelDir / wheels[0].radius; //TODO: here we are assuming every wheels has the same radius
        rpm = rpm * gearRatio * diffRatio * (60 / (2 * Mathf.PI)); /*To Convert from rad/s to rpm/min */
        rpm = Mathf.Abs(rpm);

        rpm = Mathf.Clamp(rpm, 1000, 6000); //If rpm is zero then the car will never start. If too high just doesn't make sense

        //Torque of Engine
        float Tengine = LookupTorqueCurve(rpm);

       //Debug.Log(rpm);
        //Debug.Log("kph: " + rb.velocity.magnitude * 3600 / 1000);

        Tdrive = Tengine * gearRatio * diffRatio * transmissionEfficiency;
        Tdrive *= Input.GetAxis("Vertical");
        if (numOfDriveWheels != 0) {
            Tdrive /= numOfDriveWheels; //Torque is divided between wheels
        }
        else
            Tdrive = 0;

        //RPM test
        float F = 0f;
        foreach (Wheel w in wheels) {
            F += w.F.x;
        }

        float a = F / kerbWeight;
        float v = a * Time.deltaTime + v0;

        float newrpm = v / wheels[0].radius; //TODO: here we are assuming every wheels has the same radius
        newrpm *= gearRatio * diffRatio * (60 / (2 * Mathf.PI)); /*To Convert from rad/s to rpm/min */
        //newrpm = Mathf.Abs(rpm);

        //Debug.Log("RPM Calc: " + newrpm);
        v0 = v;
    }
    private float LookupTorqueCurve(float rpm) { //In Nm
        return torqueCurve.Evaluate(rpm);
    }
    private void input() {
        //Gear Changing
        if (Input.GetKeyDown(KeyCode.UpArrow) && curGear < gearRatios.Length - 1) curGear++;
        if (Input.GetKeyDown(KeyCode.DownArrow) && curGear > 0) curGear--;

        //if (Input.GetKeyDown(KeyCode.Escape)) {
        //    Cursor.visible = true;
        //    cineCam.GetComponent<CinemachineInputAxisController>().enabled = false;
        //}
        //else if(Input.GetMouseButtonDown(0)) {
        //    Cursor.visible = false;
        //    cineCam.GetComponent<CinemachineInputAxisController>().enabled = true;
        //}

        //Reset car position and orientation
        if (Input.GetKeyDown(KeyCode.R)) {
            
            transform.position += new Vector3(0, 2, 0);
            transform.rotation = Quaternion.identity;
        }
    }
    public void calculateWeightDistribution(float bodyAccel) { //TODO: can be done better I think that my suspesnion code is the problem as it isnt bug free, I think I've messed up something there but have to check later on
                                                               //TODO: I prefer to not use weight distribution because unity seems to do that for me, I'm simulating suspension after all and the forces Im making sure they are correctly placed.
                                                               //Thats why Im setting accel to zero from the wheel script. The weight distribution will only be useful for calculating max grip and when will the wheel slip, also how much torque will actually be applied.
        Vector3 frontAxle = (suspensions[0].transform.position + suspensions[1].transform.position) / 2f; //TODO: Support multiple wheels and number of axis
        Vector3 rearAxle = (suspensions[2].transform.position + suspensions[3].transform.position) / 2f;

        float L = (rearAxle - frontAxle).magnitude;

        Vector3 CG = rb.worldCenterOfMass;

        float W = rb.mass * -Physics.gravity.y;

        float b = (CG - frontAxle).magnitude;//Mathf.Abs(Vector3.Dot(CG - frontAxle, transform.forward));
        float c = L - b;

        float h = 0f;
        foreach (Suspension s in suspensions) {
            //h += Mathf.Abs(Vector3.Dot(CG - w.hit.point, transform.up));
            h += Mathf.Abs(Vector3.Dot(CG - (transform.position - transform.up * (s.springLength + s.w.radius)), transform.up));
        }
        h /= 4; //I think this is the best way to estimate it. My idea is that there is an imaginary plane under the wheels touching the contact points and the height of CG is the height from CG to the center of this plane.

        sWf = (c / L) * W; //These are in newtons so they have to be devided by 10 when using them for unity forces
        sWr = (b / L) * W; //If this is not called from a fixedUpdate then the delta should be different.
                           //TODO: have to implement  Fmax = mu * W for further calculations.
                           //float projectionAmount = ;

        Wf = (c / L) * W - (h / L) * rb.mass * bodyAccel;
        Wr = (b / L) * W + (h / L) * rb.mass * bodyAccel;

        sWf /= -Physics.gravity.y; //For using it in unity XD
        sWr /= -Physics.gravity.y;

        Wf /= -Physics.gravity.y;
        Wr /= -Physics.gravity.y;
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
                w.steerAngle = ackermannAngleLeft;
            }
            else if (w.type == Wheel.WheelType.FR) {
                w.steerAngle = ackermannAngleRight;
            }
        }
    }
    private void OnDrawGizmos() {
        if (debugCG) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GetComponent<Rigidbody>().worldCenterOfMass, .2f);
        }
    }
}
