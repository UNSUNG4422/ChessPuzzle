using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StageSelectButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = Color.gray;

    private StageLoader stageLoader;
    private int stageIndex;
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

    public void Setup(StageLoader loader, int index, StageData stageData, bool unlocked)
    {
        stageLoader = loader;
        stageIndex = index;

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (label != null)
        {
            label.text = $"{index + 1}";
        }

        if (button != null)
        {
            button.interactable = unlocked;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = unlocked ? unlockedColor : lockedColor;
        }
    }

    private void OnButtonClicked()
    {
        if (stageLoader == null)
        {
            Debug.LogWarning("StageSelectButton has no StageLoader assigned.");
            return;
        }

        stageLoader.LoadStageByIndex(stageIndex);
    }
}
