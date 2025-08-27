using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraManager : MonoBehaviour {
    private CinemachineCamera cineCam;

    private void Start() {
        cineCam = GetComponent<CinemachineCamera>();
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
    }
}
