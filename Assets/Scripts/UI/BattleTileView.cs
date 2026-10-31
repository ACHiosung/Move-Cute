using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    public sealed class BattleTileView : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] Text label;
        [SerializeField] GameObject targetFrame;
        public void CaptureReferences()
        {
            background = GetComponent<Image>();
            label = GetComponentInChildren<Text>();
            targetFrame = transform.Find("TargetFrame").gameObject;
        }
        public void Render(Color color, string text)
        {
            if (background.color != color) background.color = color;
            if (label.text != text) label.text = text;
        }
        public void SetTarget(bool value)
        {
            if (targetFrame.activeSelf != value) targetFrame.SetActive(value);
        }
    }
}
