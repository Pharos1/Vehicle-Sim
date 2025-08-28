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
			EditorGUILayout.BeginHorizontal();
			so.FindProperty("type").enumValueIndex = (int)(Wheel.WheelType)EditorGUILayout.EnumPopup("Wheel Type", w.type);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			so.FindProperty("isWheelPowering").boolValue = EditorGUILayout.Toggle("Is Wheel Powering?", w.isWheelPowering);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			so.FindProperty("isWheelBraking").boolValue = EditorGUILayout.Toggle("Is Wheel Braking?", w.isWheelBraking);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			so.FindProperty("Cbraking").floatValue = EditorGUILayout.FloatField("Braking Coeff", w.Cbraking);
			EditorGUILayout.EndHorizontal();
		}
		
		Wheel.showCollisionList = EditorGUILayout.Foldout(Wheel.showCollisionList, "Wheel Collision Config");
		if (Wheel.showCollisionList) {
			EditorGUILayout.BeginHorizontal();
			so.FindProperty("layers").intValue = EditorGUILayout.IntField("Raycast Layers", w.layers);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			so.FindProperty("rays").intValue = EditorGUILayout.IntField("Rays per Layer", w.rays);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			so.FindProperty("useBetterCoverage").boolValue = EditorGUILayout.Toggle("Use Better Coverage", w.useBetterCoverage);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.HelpBox("Better Coverage is designed to spread out rays across layers more evenly, giving better coverage of collision. Preferably, use the same number of rays and layers.", MessageType.Info);

			EditorGUILayout.BeginHorizontal();
			GUI.enabled = false; //Make grayed out read-only field
			EditorGUILayout.Toggle("Is Grounded?", w.isGrounded);
			GUI.enabled = true;

			EditorGUILayout.EndHorizontal();
		}

		Wheel.showPhysicsList = EditorGUILayout.Foldout(Wheel.showPhysicsList, "Wheel Physics Config");
		if (Wheel.showPhysicsList) {
			EditorGUILayout.BeginHorizontal();
			so.FindProperty("mass").floatValue = EditorGUILayout.FloatField("Wheel Mass", w.mass);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			so.FindProperty("Crr").floatValue = EditorGUILayout.FloatField("Rolling Resistance", w.Crr);
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal();
			so.FindProperty("Cs").floatValue = EditorGUILayout.FloatField("Side Friction Coeff", w.Cs);
			EditorGUILayout.EndHorizontal();
		}

		Wheel.showGizmosList = EditorGUILayout.Foldout(Wheel.showGizmosList, "Gizmos");
		if (Wheel.showGizmosList) {
			EditorGUILayout.BeginHorizontal();
			so.FindProperty("debugSize").boolValue = EditorGUILayout.Toggle("Debug Dimensions", w.debugSize);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			so.FindProperty("debugRays").boolValue = EditorGUILayout.Toggle("Debug Collisions", w.debugRays);
			EditorGUILayout.EndHorizontal();
		}

		so.ApplyModifiedProperties();
	}

	private void OnEnable() {
		w = target as Wheel;
	}
}
