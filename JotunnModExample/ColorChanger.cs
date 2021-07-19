using Jotunn.Managers;
using UnityEngine;

namespace JotunnModExample
{
    internal class ColorChanger : MonoBehaviour
    {
        // Pointer to the Renderer being changed
        private Renderer r;

        void Start()
        {
            // Find the first Renderer in the GameObject
            r = GetComponentInChildren<Renderer>();
            r.sharedMaterial = r.material;

            // Display the ColorPicker
            GUIManager.Instance.CreateColorPicker(
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                r.sharedMaterial.color,  // Initial selected color in the picker
                "Choose your poison",  // Caption of the picker window
                SetColor,  // Callback delegate when the color in the picker changes
                ColorChosen,  // Callback delegate when the the window is closed
                true  // Whether or not the alpha channel should be editable
            );
            
            // Block input while showing the ColorPicker
            GUIManager.BlockInput(true);
        }

        // Delegate method called by the ColorPicker on every color change
        private void SetColor(Color currentColor)
        {
            r.sharedMaterial.color = currentColor;
        }

        // Delegate method called by the ColorPicker on close
        private void ColorChosen(Color finalColor)
        {
            GUIManager.BlockInput(false);
            Destroy(this);
        }
    }
}
