using System.Reflection;
using UnityEngine;

// UI-package-independent tip loader for Unity 6000.6.3f1.
// Works with a component exposing a public string property named "text"
// (for example legacy UI Text or TextMeshProUGUI) without a compile-time dependency.
public class TipLoader : MonoBehaviour
{
    private Component textComponent;
    private PropertyInfo textProperty;

    void Start()
    {
        foreach (Component component in GetComponents<Component>())
        {
            if (component == null || component == this) continue;
            PropertyInfo property = component.GetType().GetProperty("text", typeof(string));
            if (property != null && property.CanWrite)
            {
                textComponent = component;
                textProperty = property;
                break;
            }
        }

        if (textComponent == null)
        {
            Debug.LogWarning("TipLoader: No text component found on " + gameObject.name);
            return;
        }

        LoadRandomTip();
    }

    private void LoadRandomTip()
    {
        if (Tips.TipCollection.Length < 1)
        {
            Debug.LogError("There are no tips! Please add some in Tips.cs");
            return;
        }

        int randomIndex = Random.Range(0, Tips.TipCollection.Length);
        string fetchedTip = Tips.TipCollection[randomIndex];
        textProperty.SetValue(textComponent, string.Format("Tip #{0}: {1}", randomIndex + 1, fetchedTip), null);
    }
}
