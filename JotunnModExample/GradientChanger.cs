using Jotunn.Managers;
using UnityEngine;

namespace JotunnModExample
{
    internal class GradientChanger : MonoBehaviour
    {
        // Pointer to the Renderer being changed
        private Renderer r;
        // Pointer to the resulting gradiant
        private Gradient g;

        void Start()
        {
            // Find the first Renderer in the GameObject
            r = GetComponentInChildren<Renderer>();
            r.sharedMaterial = r.material;
            
            // Create an empty gradient
            g = new Gradient();

            // Display the ColorPicker
            GUIManager.Instance.CreateGradientPicker(
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                new Gradient(),  // Initial gradient being used
                "Gradiwut?",  // Caption of the GradientPicker window
                SetGradient,  // Callback delegate when the gradient changes
                GradientFinished  // Callback delegate when thw window is closed
            );

            // Block input while showing the GradientPicker
            GUIManager.BlockInput(true);
        }

        // Cycle the gradients in the Update loop
        private void Update()
        {
            r.sharedMaterial.color = g.Evaluate(0.5f + Mathf.Sin(Time.time * 2f) * 0.5f);
        }

        // Delegate method
        private void SetGradient(Gradient currentGradient)
        {
            g = currentGradient;
        }

        // Delegate method called by the GradientPicker on close
        public void GradientFinished(Gradient finishedGradient)
        {
            GUIManager.BlockInput(false);
        }
    }
}
