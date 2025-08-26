using UnityEngine;
using UnityEngine.TextCore.Text;

public class Telemetry : MonoBehaviour {
    [SerializeField] private bool showTelemetry = true;

    [SerializeField] private Transform body;

    [SerializeField] private Font font;
    [SerializeField] private FontAsset fontass;

    private Rigidbody rb;
    public bool pointMode;

    private void Start() {
        rb = body.GetComponent<Rigidbody>();
    }

    void OnGUI() {
        if (!showTelemetry)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 12;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperLeft;
        style.font = font; // Monospace look

        if (pointMode) font.material.mainTexture.filterMode = FilterMode.Point;
        else font.material.mainTexture.filterMode = FilterMode.Bilinear;

        GUILayout.BeginArea(new Rect(10, 10, 400, 200), GUI.skin.box);
        GUILayout.Label("Telemetry", style);

        float speedMS = rb.velocity.magnitude;
        float speedKMH = speedMS * 3.6f;
        float speedMPH = speedMS * 2.23694f;

        GUILayout.Label($"V: {speedMS:F1} m/s  {speedKMH:F1} km/h  {speedMPH:F1} mph", style);
        GUILayout.Label($"WheelFl     : 0 rpm", style);

        //ShowWheel("WheelFL", wheelFL, style);
        //ShowWheel("WheelFR", wheelFR, style);
        //ShowWheel("WheelRL", wheelRL, style);
        //ShowWheel("WheelRR", wheelRR, style);

        GUILayout.EndArea();
    }

}
