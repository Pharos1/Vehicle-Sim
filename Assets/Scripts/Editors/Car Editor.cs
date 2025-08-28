using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Car))]
[CanEditMultipleObjects]
public class CarEditor : Editor {
	Car car;
	SerializedObject so => serializedObject;

	SerializedProperty physicsList;
	SerializedProperty transmissionList;
	SerializedProperty engineList;
	SerializedProperty gizmosList;

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		EditorGUILayout.Space(10);

		physicsList = so.FindProperty("physicsList");
		physicsList.isExpanded = EditorGUILayout.Foldout(physicsList.isExpanded, "Car Physics");
		if (physicsList.isExpanded) {
			EditorGUILayout.PropertyField(so.FindProperty("dragCoeff"), new GUIContent("Drag Coeff"));
			EditorGUILayout.PropertyField(so.FindProperty("frontalArea"), new GUIContent("Frontal Area"));
			EditorGUILayout.PropertyField(so.FindProperty("airDensity"), new GUIContent("Air Density"));
		}

		transmissionList = so.FindProperty("transmissionList");
		transmissionList.isExpanded = EditorGUILayout.Foldout(transmissionList.isExpanded, "Transmission Config");
		if (transmissionList.isExpanded) {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(10);
			EditorGUILayout.PropertyField(so.FindProperty("gearRatios"));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.PropertyField(so.FindProperty("diffRatio"), new GUIContent("Final Drive Ratio", "Also called differential ratio."));
			EditorGUILayout.PropertyField(so.FindProperty("transmissionEfficiency"), new GUIContent("Transmission Eff.", "Efficiency of full transmission(gears, drivetrain, etc.)"));
		}

		engineList = so.FindProperty("engineList");
		engineList.isExpanded = EditorGUILayout.Foldout(engineList.isExpanded, "Engine Config");
		if (engineList.isExpanded) {
			EditorGUILayout.PropertyField(so.FindProperty("torqueCurve"));
		}

		gizmosList = so.FindProperty("gizmosList");
		gizmosList.isExpanded = EditorGUILayout.Foldout(gizmosList.isExpanded, "Gizmos");
		if (gizmosList.isExpanded) {
			EditorGUILayout.PropertyField(so.FindProperty("debugCG"), new GUIContent("Debug COM", "Center Of Mass/Geometry"));
		}

		so.ApplyModifiedProperties();
	}

	private void OnEnable() {
		car = target as Car;
	}
}
