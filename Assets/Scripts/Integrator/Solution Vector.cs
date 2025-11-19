using UnityEngine;

public class SolutionVector : MonoBehaviour {
        public float sLength = 0;

        public void add(SolutionVector b, float factor = 1f) {
            sLength += factor * b.sLength;
        }
}