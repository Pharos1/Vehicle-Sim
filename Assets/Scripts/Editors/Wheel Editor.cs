using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Wheel))]
[CanEditMultipleObjects]
public class WheelEditor : Editor {
	Wheel w;

	SerializedObject so => serializedObject;

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		EditorGUILayout.Space(10);

		Wheel.showConfigList = EditorGUILayout.Foldout(Wheel.showConfigList, "Wheel Config");
		if (Wheel.showConfigList) {
			so.FindProperty("type").enumValueIndex = (int)(Wheel.WheelType)EditorGUILayout.EnumPopup("Wheel Type", w.type);
			so.FindProperty("isWheelPowering").boolValue = EditorGUILayout.Toggle("Is Wheel Powering?", w.isWheelPowering);
			so.FindProperty("isWheelBraking").boolValue = EditorGUILayout.Toggle("Is Wheel Braking?", w.isWheelBraking);
			so.FindProperty("Cbraking").floatValue = EditorGUILayout.FloatField("Braking Coeff", w.Cbraking);
		}
		
		Wheel.showCollisionList = EditorGUILayout.Foldout(Wheel.showCollisionList, "Wheel Collision Config");
		if (Wheel.showCollisionList) {
			so.FindProperty("layers").intValue = EditorGUILayout.IntField("Raycast Layers", w.layers);
			so.FindProperty("rays").intValue = EditorGUILayout.IntField("Rays per Layer", w.rays);
			so.FindProperty("useBetterCoverage").boolValue = EditorGUILayout.Toggle("Use Better Coverage", w.useBetterCoverage);

			EditorGUILayout.HelpBox("Better Coverage is designed to spread out rays across layers more evenly, giving better coverage of collision. Preferably, use the same number of rays and layers.", MessageType.Info);

			GUI.enabled = false; //Make grayed out read-only field
			EditorGUILayout.Toggle("Is Grounded?", w.isGrounded);
			GUI.enabled = true;
		}

		Wheel.showPhysicsList = EditorGUILayout.Foldout(Wheel.showPhysicsList, "Wheel Physics Config");
		if (Wheel.showPhysicsList) {
			so.FindProperty("mass").floatValue = EditorGUILayout.FloatField("Wheel Mass", w.mass);
			so.FindProperty("Crr").floatValue = EditorGUILayout.FloatField("Rolling Resistance", w.Crr);
			so.FindProperty("Cs").floatValue = EditorGUILayout.FloatField("Side Friction Coeff", w.Cs);
		}

		Wheel.showGizmosList = EditorGUILayout.Foldout(Wheel.showGizmosList, "Gizmos");
		if (Wheel.showGizmosList) {
			so.FindProperty("debugSize").boolValue = EditorGUILayout.Toggle("Debug Dimensions", w.debugSize);
			so.FindProperty("debugRays").boolValue = EditorGUILayout.Toggle("Debug Collisions", w.debugRays);
		}

		so.ApplyModifiedProperties();
	}

	private void OnEnable() {
		w = target as Wheel;
	}
}
