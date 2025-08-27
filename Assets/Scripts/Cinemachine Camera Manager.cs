using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraManager : MonoBehaviour {
    private CinemachineCamera cineCam;
    private CinemachineOrbitalFollow cof;
    private Car cc;

    private void Start() {
        cineCam = GetComponent<CinemachineCamera>();
        cof = GetComponent<CinemachineOrbitalFollow>();
        cc = cineCam.LookAt.GetComponent<Car>();
    }
    private void Update() {
        //Cinemachine camera cursor
        if (Input.GetMouseButton(1)) {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            cineCam.GetComponent<CinemachineInputAxisController>().enabled = true;
        }
        else {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            cineCam.GetComponent<CinemachineInputAxisController>().enabled = false;
        }

        if (cc.V.y < -1 || cc.curGear == 0) {
            cof.HorizontalAxis.Center = 180;
        }
        else {
            cof.HorizontalAxis.Center = 0;
        }
    }
}
