using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

using UnityEditor;
using System.Reflection;
using System;

namespace System.Runtime.CompilerServices {
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	internal sealed class CallerArgumentExpressionAttribute : Attribute {
		public CallerArgumentExpressionAttribute(string parameterName) {
			ParameterName = parameterName;
		}

		public string ParameterName { get; }
	}
}

public class DD : MonoBehaviour {
	private static readonly Dictionary<string, string> _debugValues = new Dictionary<string, string>();
	private static readonly StringBuilder _stringBuilder = new StringBuilder();

	/// <summary>
	/// Pass any variable, and the name will be captured automatically.
	/// Usage: DD.Var(myPlayerHealth);
	/// </summary>
	public static void Var(object value, [CallerArgumentExpression("value")] string name = "") {
		_debugValues[name] = value?.ToString() ?? "null";
	}

	// Optional: Explicit types if you prefer them, though Var() handles most cases.
	public static void DisplayFloat(float value, [CallerArgumentExpression("value")] string name = "") => Var(value, name);
	public static void DisplayInt(int value, [CallerArgumentExpression("value")] string name = "") => Var(value, name);
	public static void DisplayVector(Vector3 value, [CallerArgumentExpression("value")] string name = "") => Var(value, name);

	private float curTime;

	void LateUpdate() {
		if (_debugValues.Count == 0) return;

		curTime += Time.deltaTime;

		if (curTime > 10f) {
			curTime = 0f;
			Utils.ClearLogConsole();
		}

		_stringBuilder.Clear();
		bool first = true;

		foreach (var kvp in _debugValues) {
			if (!first) _stringBuilder.Append(" | ");
			_stringBuilder.Append($"{kvp.Key}: {kvp.Value}");
			first = false;
		}

		Debug.Log(_stringBuilder.ToString());
	}
}

public static class Utils {
	static MethodInfo _clearConsoleMethod;
	static MethodInfo clearConsoleMethod {
		get {
			if (_clearConsoleMethod == null) {
				Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
				Type logEntries = assembly.GetType("UnityEditor.LogEntries");
				_clearConsoleMethod = logEntries.GetMethod("Clear");
			}
			return _clearConsoleMethod;
		}
	}

	public static void ClearLogConsole() {
		clearConsoleMethod.Invoke(new object(), null);
	}
}