using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HelpPageButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    private StageLoader stageLoader;
    private HelpPageType helpPageType;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    public void Setup(StageLoader loader, HelpPageData page)
    {
        stageLoader = loader;

        if (page == null)
        {
            Debug.LogWarning("HelpPageButton was set up with null HelpPageData.");
            return;
        }

        helpPageType = page.helpPageType;

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (label == null)
        {
            label = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (label != null)
        {
            label.text = GetButtonTitle(page.title);
        }
    }

    private void OnButtonClicked()
    {
        if (stageLoader == null)
        {
            Debug.LogWarning("HelpPageButton has no StageLoader assigned.");
            return;
        }

        stageLoader.ShowHelpPage(helpPageType);
    }

    private string GetButtonTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return helpPageType.ToString();
        }

        string normalizedTitle = title.Replace("\r\n", "\n").Replace('\r', '\n');
        int lineBreakIndex = normalizedTitle.IndexOf('\n');
        return lineBreakIndex >= 0 ? normalizedTitle.Substring(0, lineBreakIndex) : normalizedTitle;
    }
}
