using UnityEngine;
using UnityEngine.TextCore.Text;
using System;

public class Telemetry : MonoBehaviour {
    private bool showTelemetry = true;

    [SerializeField] private Transform car;

    [SerializeField] private Font font;

    private Rigidbody rb;
    private Car cc;
    [SerializeField] private bool pointFiltering; //Gives pixelated look (Due to some bug in Unity's code, forced point filtering for fonts in Unity 6 doesn't work and doesn't give errors)

    [SerializeField] private KeyCode hotkey = KeyCode.F1;
    [SerializeField] private int fontSize = 10;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int spacingCorrection = -10;

    private void Start() {
        rb = car.GetComponent<Rigidbody>();
        cc = car.GetComponent<Car>();
    }

    private void Update() {
        if (Input.GetKeyDown(hotkey)) {
            showTelemetry = !showTelemetry;
        }
    }

    private void OnGUI() {
        if (!showTelemetry)
            return;

        //Filtering
        if (pointFiltering) font.material.mainTexture.filterMode = FilterMode.Point;
        else font.material.mainTexture.filterMode = FilterMode.Bilinear;

        //Styles
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.font = font;
        style.fontSize = fontSize;
        style.normal.textColor = textColor;
        style.hover.textColor = textColor;
        style.alignment = TextAnchor.UpperLeft;
        style.wordWrap = false;

        GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
        centeredStyle.font = font;
        centeredStyle.fontSize = fontSize;
        centeredStyle.normal.textColor = textColor;
        centeredStyle.hover.textColor = textColor;
        centeredStyle.alignment = TextAnchor.UpperCenter;

        //GUI Rendering
        GUILayout.BeginArea(new Rect(10, 10, 500, 120), GUI.skin.box);

        GUILayout.Label("Telemetry" + " | \"" + hotkey.ToString() + "\" To Hide/Show", centeredStyle); GUILayout.Space(-5);

        float speedMS = Mathf.Abs(cc.V.y);
        float speedKMH = speedMS * 3.6f;
        float speedMPH = speedMS * 2.23694f;

        GUILayout.Label($"V: {speedMS:F1} m/s  {speedKMH:F1} km/h  {speedMPH:F1} mph", style); GUILayout.Space(-5);

        showWheelLabel(cc.wheels[0], style); GUILayout.Space(spacingCorrection);
        showWheelLabel(cc.wheels[1], style); GUILayout.Space(spacingCorrection);
        showWheelLabel(cc.wheels[2], style); GUILayout.Space(spacingCorrection);
        showWheelLabel(cc.wheels[3], style); GUILayout.Space(-5);

        GUILayout.Label($"Gear: {cc.curGear}  RPM: {cc.rpm:F0}", style); GUILayout.Space(-5);

        style.alignment = TextAnchor.LowerRight;
        style.wordWrap = true;
        style.contentOffset = new Vector2(10, 0);
        GUILayout.Label("Credits: Design from Edy's VPP");

        GUILayout.EndArea();
    }
    private void showWheelLabel(Wheel wheel, GUIStyle style) {
        float wheelRpm = (wheel.V.y / wheel.radius) * (60 / (2 * Mathf.PI));
        float Fs = wheel.s.suspensionForce;
        Vector2 S = Vector2.zero;
        Vector2 F = wheel.F;

        if (wheel.isGrounded) {
            GUILayout.Label($"Wheel{wheel.type}  : {wheelRpm,4:F0} rpm  Fs: {Fs,3:F0}  Sx: {S.x,2:F2}  Sy: {S.y,2:F2}  Fx: {F.x,6:F2}  Fy: {F.y,6:F2}", style);
        }
        else {
            GUILayout.Label($"Wheel{wheel.type}  : {wheelRpm,4:F0} rpm  --", style);
        }
    }
}
