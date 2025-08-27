using UnityEditor;

[CustomEditor(typeof(Suspension))]
public class SuspensionEditor : Editor {
	Suspension s;

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		EditorGUILayout.Space(10);

		s.showSuspensionList = EditorGUILayout.Foldout(s.showSuspensionList, "Suspension Config");
		if (s.showSuspensionList) {
			//Ck
			EditorGUILayout.BeginHorizontal();
			s.Ck = EditorGUILayout.Slider("Spring Stiffness", s.Ck, 0, 1);
			EditorGUILayout.EndHorizontal();

			//Cd
			EditorGUILayout.BeginHorizontal();
			s.Cd = EditorGUILayout.Slider("Damper Stiffness", s.Cd, 0, 1);
			EditorGUILayout.EndHorizontal();

            //Rest Length
            EditorGUILayout.BeginHorizontal();
            s.restLength = EditorGUILayout.FloatField("Rest Length", s.restLength);
            EditorGUILayout.EndHorizontal();

            //Spring Travel
            EditorGUILayout.BeginHorizontal();
            s.springTravel = EditorGUILayout.FloatField("Spring Travel", s.springTravel);
            EditorGUILayout.EndHorizontal();
        }
	}

	private void OnEnable() {
		s = target as Suspension;
	}
}
