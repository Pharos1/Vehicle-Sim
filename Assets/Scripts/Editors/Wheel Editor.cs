using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Wheel))]
[CanEditMultipleObjects]
public class WheelEditor : Editor {
	Wheel w;

	SerializedObject so => serializedObject;

	SerializedProperty configList;
	SerializedProperty collisionList;
	SerializedProperty physicsList;
	SerializedProperty gizmosList;

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		EditorGUILayout.Space(10);

		configList = so.FindProperty("configList");
		configList.isExpanded = EditorGUILayout.Foldout(configList.isExpanded, "Wheel Config");
		if (configList.isExpanded) {
            EditorGUILayout.PropertyField(so.FindProperty("type"), new GUIContent("Wheel Type"));
            EditorGUILayout.PropertyField(so.FindProperty("isWheelPowering"), new GUIContent("Is Wheel Powering?"));
            EditorGUILayout.PropertyField(so.FindProperty("isWheelBraking"), new GUIContent("Is Wheel Braking?"));
            EditorGUILayout.PropertyField(so.FindProperty("Cbraking"), new GUIContent("Braking Coeff"));
		}

        collisionList = so.FindProperty("collisionList");
        collisionList.isExpanded = EditorGUILayout.Foldout(collisionList.isExpanded, "Wheel Collision Config");
		if (collisionList.isExpanded) {
            EditorGUILayout.PropertyField(so.FindProperty("layers"), new GUIContent("Raycast Layers"));
            EditorGUILayout.PropertyField(so.FindProperty("rays"), new GUIContent("Rays per Layer"));
            EditorGUILayout.PropertyField(so.FindProperty("useBetterCoverage"), new GUIContent("Use Better Coverage"));

			EditorGUILayout.HelpBox("Better Coverage is designed to spread out rays across layers more evenly, giving better coverage of collision. Preferably, use the same number of rays and layers.", MessageType.Info);

			GUI.enabled = false; //Make grayed out read-only field
            EditorGUILayout.PropertyField(so.FindProperty("isGrounded"), new GUIContent("Is Grounded?"));
            GUI.enabled = true;
		}

        physicsList = so.FindProperty("physicsList");
        physicsList.isExpanded = EditorGUILayout.Foldout(physicsList.isExpanded, "Wheel Physics Config");
		if (physicsList.isExpanded) {
            EditorGUILayout.PropertyField(so.FindProperty("mass"), new GUIContent("Wheel Mass", "Changes weight of body(body.mass = kerbWeight - massOfAllWheels)"));
            EditorGUILayout.PropertyField(so.FindProperty("Crr"), new GUIContent("Rolling Resistance", "Opposite to movement force that is applied because of the elasticity of tyres. Prevents car from rolling freely in low speeds."));
            EditorGUILayout.PropertyField(so.FindProperty("Cs"), new GUIContent("Side Friction Coeff"));
		}

        gizmosList = so.FindProperty("gizmosList");
        gizmosList.isExpanded = EditorGUILayout.Foldout(gizmosList.isExpanded, "Gizmos");
		if (gizmosList.isExpanded) {
            EditorGUILayout.PropertyField(so.FindProperty("debugSize"), new GUIContent("Debug Dimensions"));
            EditorGUILayout.PropertyField(so.FindProperty("debugRays"), new GUIContent("Debug Collisions"));
		}

		so.ApplyModifiedProperties();
	}

	private void OnEnable() {
		w = target as Wheel;
	}
}
