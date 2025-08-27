using UnityEditor;

[CustomEditor(typeof(Suspension))]
[CanEditMultipleObjects]
public class SuspensionEditor : Editor {
	Suspension s;

    SerializedObject so => serializedObject;

    public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		EditorGUILayout.Space(10);

		Suspension.showSuspensionList = EditorGUILayout.Foldout(Suspension.showSuspensionList, "Suspension Config");
		if (Suspension.showSuspensionList) {
			//Ck
			EditorGUILayout.BeginHorizontal();
            so.FindProperty("Ck").floatValue = EditorGUILayout.Slider("Spring Stiffness", s.Ck, 0, 1);
			EditorGUILayout.EndHorizontal();

			//Cd
			EditorGUILayout.BeginHorizontal();
            so.FindProperty("Cd").floatValue = EditorGUILayout.Slider("Damper Stiffness", s.Cd, 0, 1);
			EditorGUILayout.EndHorizontal();

            //Rest Length
            EditorGUILayout.BeginHorizontal();
            so.FindProperty("restLength").floatValue = EditorGUILayout.FloatField("Rest Length", s.restLength);
            EditorGUILayout.EndHorizontal();

            //Spring Travel
            EditorGUILayout.BeginHorizontal();
            so.FindProperty("springTravel").floatValue = EditorGUILayout.FloatField("Spring Travel", s.springTravel);
            EditorGUILayout.EndHorizontal();
        }

        so.ApplyModifiedProperties();
    }

	private void OnEnable() {
		s = target as Suspension;
	}
}
