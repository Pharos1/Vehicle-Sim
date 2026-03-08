using System;

public static class RK4 {
	// Define a delegate signature for our differential equations: dy/dt = f(t, y)
	public delegate float Derivative(float t, float y);

	/// <summary>
	/// Integrates a 1D differential equation using 4th-Order Runge-Kutta.
	/// </summary>
	/// <param name="f">The derivative function</param>
	/// <param name="t">Current time</param>
	/// <param name="y">Current state value</param>
	/// <param name="h">Time step (usually fixedDeltaTime)</param>
	/// <param name="args">Any extra variables the derivative function needs</param>
	public static float Integrate(Derivative f, float t, float y, float h) {
		float k1 = f(t, y);
		float k2 = f(t + h * 0.5f, y + h * 0.5f * k1);
		float k3 = f(t + h * 0.5f, y + h * 0.5f * k2);
		float k4 = f(t + h, y + h * k3);

		return y + (h / 6f) * (k1 + 2f * k2 + 2f * k3 + k4);
	}
}