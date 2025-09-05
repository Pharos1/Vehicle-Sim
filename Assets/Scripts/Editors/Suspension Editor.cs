using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Suspension))]
[CanEditMultipleObjects]
public class SuspensionEditor : Editor {
	Suspension s;

	SerializedObject so => serializedObject;

	SerializedProperty suspensionList;

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		EditorGUILayout.Space(10);

        suspensionList = so.FindProperty("suspensionList");
        suspensionList.isExpanded = EditorGUILayout.Foldout(suspensionList.isExpanded, "Suspension Config");
		if (suspensionList.isExpanded) {
            EditorGUILayout.PropertyField(so.FindProperty("Ck"), new GUIContent("Spring Stiffness"));
            EditorGUILayout.PropertyField(so.FindProperty("Cd"), new GUIContent("Damper Stiffness"));
            EditorGUILayout.PropertyField(so.FindProperty("restLength"), new GUIContent("Rest Length"));
            EditorGUILayout.PropertyField(so.FindProperty("springTravel"), new GUIContent("Spring Travel"));
		}

		so.ApplyModifiedProperties();
	}

	private void OnEnable() {
		s = target as Suspension;
	}
}
