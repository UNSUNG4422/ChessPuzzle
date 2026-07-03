using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageLoader : MonoBehaviour
{
    private const string HighestUnlockedStageIndexKey = "HighestUnlockedStageIndex";
    private const string LastPlayedStageIndexKey = "LastPlayedStageIndex";
    private const string HelpPageUnlockedKeyPrefix = "HelpPageUnlocked_";
    private const string HelpPageShownKeyPrefix = "HelpPageShown_";

    [SerializeField] private StageData currentStage;
    [SerializeField] private List<StageData> stages = new List<StageData>();
    [SerializeField] private int currentStageIndex = 0;
    [SerializeField] private int highestUnlockedStageIndex = 0;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI allClearText;
    [SerializeField] private GameObject allClearPanel;
    [SerializeField] private BoardGenerator boardGenerator;
    [SerializeField] private PuzzleManager puzzleManager;
    [Header("Camera Fit")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float cameraPadding = 1.0f;
    [SerializeField] private float minOrthographicSize = 3.5f;
    [SerializeField] private float maxOrthographicSize = 12f;
    [SerializeField] private Vector2 cameraOffset = Vector2.zero;
    [SerializeField] private GameObject stageSelectPanel;
    [SerializeField] private Transform stageGroupContainer;
    [SerializeField] private GameObject stageGroupPrefab;
    [SerializeField] private Transform stageButtonContainer;
    [SerializeField] private GameObject stageButtonPrefab;
    [Header("Stage Select Manual Layout")]
    [SerializeField] private int stageButtonsPerRow = 10;
    [SerializeField] private float stageButtonWidth = 68f;
    [SerializeField] private float stageButtonHeight = 68f;
    [SerializeField] private float stageButtonSpacingX = 14f;
    [SerializeField] private float stageButtonSpacingY = 10f;
    [SerializeField] private float categoryTitleHeight = 28f;
    [SerializeField] private float titleToButtonSpacing = 6f;
    [SerializeField] private float categorySpacing = 16f;
    [SerializeField] private Vector2 stageSelectStartPosition = new Vector2(20f, -20f);
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject helpListView;
    [SerializeField] private Transform helpButtonContainer;
    [SerializeField] private GameObject helpPageButtonPrefab;
    [SerializeField] private GameObject helpPageView;
    [SerializeField] private TextMeshProUGUI helpTitleText;
    [SerializeField] private TextMeshProUGUI helpMessageText;
    [SerializeField] private Image helpImage;
    [SerializeField] private List<HelpPageData> helpPages = new List<HelpPageData>();
    [SerializeField] private List<CategoryClearMessage> categoryClearMessages = new List<CategoryClearMessage>();
    [SerializeField, TextArea(1, 3)] private string defaultCategoryClearMessage = "ステージクリア！";
    [SerializeField, TextArea(1, 3)] private string allClearMessage = "ALL CLEAR!";
    [SerializeField] private bool unlockAllStagesOnStart = false;
    [SerializeField] private bool showTitleOnStart = true;
    [SerializeField] private AudioClip titleBGM;
    [SerializeField] private AudioClip gameBGM;
    [SerializeField] private AudioClip buttonSE;
    [SerializeField, Range(0f, 2f)] private float buttonSEVolume = 1f;
    [SerializeField] private AudioClip decisionSE;
    [SerializeField, Range(0f, 2f)] private float decisionSEVolume = 1f;
    [SerializeField] private AudioClip cancelSE;
    [SerializeField, Range(0f, 2f)] private float cancelSEVolume = 1f;
    [SerializeField] private AudioClip quitSE;
    [SerializeField, Range(0f, 2f)] private float quitSEVolume = 1f;
    [SerializeField] private AudioClip allClearSE;
    [SerializeField, Range(0f, 2f)] private float allClearSEVolume = 1f;

    private HelpPageData currentDisplayedHelpPage;
    private StageSelectOpenSource stageSelectOpenSource = StageSelectOpenSource.Game;

    private enum StageSelectOpenSource
    {
        Title,
        Pause,
        Game
    }

    private void Start()
    {
        SetAllClearPanelVisible(false);

        if (stages == null || stages.Count == 0)
        {
            Debug.LogWarning("StageLoader has no stages assigned.");
            return;
        }

        LoadStageProgress();

        if (unlockAllStagesOnStart)
        {
            UnlockAllStagesForDebug();
        }

        if (showTitleOnStart)
        {
            ShowTitle();
        }
        else
        {
            StartGame();
        }
    }

    public void LoadStageByIndex(int index)
    {
        SetPanelVisible(helpPanel, false);
        PlaySE(decisionSE, decisionSEVolume);
        LoadStageByIndexInternal(index);
    }

    private void LoadStageByIndexInternal(int index)
    {
        if (stages == null || index < 0 || index >= stages.Count)
        {
            Debug.LogWarning($"Stage index {index} is out of range.");
            return;
        }

        if (!IsStageUnlocked(index))
        {
            Debug.LogWarning("Cannot load a locked stage.");
            return;
        }

        currentStageIndex = index;
        SaveLastPlayedStageIndex(currentStageIndex);
        currentStage = stages[index];
        PlayBGM(gameBGM);
        HideGameplayClearUi();
        SetPanelVisible(helpPanel, false);
        SetPanelVisible(titlePanel, false);
        SetPanelVisible(pauseMenuPanel, false);
        SetPanelVisible(gameUIPanel, true);
        SetAllClearPanelVisible(false);
        LoadStage(currentStage);
        UpdateStageText();
        CloseStageSelectSilently();
        Debug.Log($"[HELP DEBUG] Loaded stage index={index}, stage={currentStage?.name}");
        Debug.Log($"[HELP DEBUG] unlock={currentStage.helpPageToUnlock}, everyTime={currentStage.helpPageToShowEveryTime}");
        ProcessStageHelp(currentStage);
    }

    public void LoadNextStage()
    {
        int nextStageIndex = currentStageIndex + 1;

        if (stages != null && nextStageIndex < stages.Count)
        {
            PlaySE(decisionSE, decisionSEVolume);
            LoadStageByIndexInternal(nextStageIndex);
            return;
        }

        ShowAllClear();
    }

    public void ReloadCurrentStage()
    {
        PlaySE(decisionSE, decisionSEVolume);
        LoadStageByIndexInternal(currentStageIndex);
    }

    public void ShowTitle()
    {
        PlayBGM(titleBGM);
        SetPanelVisible(helpPanel, false);
        SetPanelVisible(titlePanel, true);
        SetPanelVisible(pauseMenuPanel, false);
        CloseStageSelectSilently();
        SetPanelVisible(gameUIPanel, false);
        SetAllClearPanelVisible(false);
    }

    public void StartGame()
    {
        PlaySE(decisionSE, decisionSEVolume);
        SetPanelVisible(helpPanel, false);
        SetPanelVisible(titlePanel, false);
        SetPanelVisible(pauseMenuPanel, false);
        CloseStageSelectSilently();
        SetPanelVisible(gameUIPanel, true);
        SetAllClearPanelVisible(false);

        if (stages == null || stages.Count == 0)
        {
            Debug.LogWarning("StageLoader has no stages assigned.");
            return;
        }

        LoadStageByIndexInternal(GetClampedLastPlayedStageIndex());
    }

    public void ReturnToTitle()
    {
        PlaySE(cancelSE, cancelSEVolume);
        ShowTitle();
    }

    public void OpenPauseMenu()
    {
        PlaySE(buttonSE, buttonSEVolume);
        SetPanelVisible(helpPanel, false);
        SetPanelVisible(pauseMenuPanel, true);
    }

    public void ClosePauseMenu()
    {
        PlaySE(cancelSE, cancelSEVolume);
        SetPanelVisible(helpPanel, false);
        SetPanelVisible(pauseMenuPanel, false);
    }

    public void OpenStageSelectFromTitle()
    {
        OpenStageSelect(StageSelectOpenSource.Title);
    }

    public void OpenStageSelectFromPause()
    {
        OpenStageSelect(StageSelectOpenSource.Pause);
    }

    public void OpenHelp()
    {
        PlaySE(buttonSE, buttonSEVolume);
        ShowHelpPanel();
        ShowHelpList();
    }

    private void ShowHelpPanel()
    {
        EnsureHelpPanelBlocksRaycasts();
        SetPanelVisible(helpPanel, true);

        if (helpPanel != null)
        {
            helpPanel.transform.SetAsLastSibling();
            Debug.Log($"[HELP DEBUG] HelpPanel active={helpPanel.activeSelf}, title={currentDisplayedHelpPage?.title}");
        }
    }

    public void CloseHelp()
    {
        PlaySE(cancelSE, cancelSEVolume);
        SetPanelVisible(helpPanel, false);
    }

    public void ShowHelpList()
    {
        BuildHelpPageButtons();
        SetPanelVisible(helpListView, true);
        SetPanelVisible(helpPageView, false);
    }

    public void QuitGame()
    {
        if (quitSE != null)
        {
            PlaySE(quitSE, quitSEVolume);
        }
        else
        {
            PlaySE(cancelSE, cancelSEVolume);
        }

#if UNITY_EDITOR
        Debug.Log("Quit Game");
#else
        Application.Quit();
#endif
    }

    public bool IsStageUnlocked(int index)
    {
        return index <= highestUnlockedStageIndex;
    }

    public bool IsBlockingGameplayInput()
    {
        return IsPanelActive(helpPanel)
            || IsPanelActive(pauseMenuPanel)
            || IsPanelActive(titlePanel)
            || IsPanelActive(stageSelectPanel)
            || IsPanelActive(allClearPanel);
    }

    public void UnlockNextStage()
    {
        if (stages == null)
        {
            return;
        }

        int nextIndex = currentStageIndex + 1;
        if (nextIndex < stages.Count && nextIndex > highestUnlockedStageIndex)
        {
            highestUnlockedStageIndex = nextIndex;
            SaveStageProgress();
        }

        BuildStageSelectButtons();
    }

    public void ResetStageProgress()
    {
        highestUnlockedStageIndex = 0;
        SaveStageProgress();
        SaveLastPlayedStageIndex(0);
        ResetHelpPageProgress();
        PlayerPrefs.Save();
        BuildStageSelectButtons();
    }

    public void UnlockAllStagesForDebug()
    {
        if (stages == null || stages.Count == 0)
        {
            highestUnlockedStageIndex = 0;
            SaveStageProgress();
            return;
        }

        highestUnlockedStageIndex = stages.Count - 1;
        SaveStageProgress();
        BuildStageSelectButtons();
    }

    public void OpenStageSelect()
    {
        OpenStageSelect(GetStageSelectOpenSourceFromActivePanel());
    }

    private void OpenStageSelect(StageSelectOpenSource openSource)
    {
        stageSelectOpenSource = openSource;
        PlaySE(buttonSE, buttonSEVolume);
        SetPanelVisible(helpPanel, false);
        SetPanelVisible(titlePanel, false);
        SetPanelVisible(pauseMenuPanel, false);
        SetPanelVisible(gameUIPanel, false);
        BuildStageSelectButtons();

        if (stageSelectPanel != null)
        {
            stageSelectPanel.SetActive(true);
        }
    }

    public void CloseStageSelect()
    {
        PlaySE(cancelSE, cancelSEVolume);
        CloseStageSelectSilently();

        switch (stageSelectOpenSource)
        {
            case StageSelectOpenSource.Title:
                SetPanelVisible(titlePanel, true);
                SetPanelVisible(pauseMenuPanel, false);
                SetPanelVisible(gameUIPanel, false);
                break;
            case StageSelectOpenSource.Pause:
                SetPanelVisible(titlePanel, false);
                SetPanelVisible(pauseMenuPanel, true);
                SetPanelVisible(gameUIPanel, false);
                break;
            case StageSelectOpenSource.Game:
                SetPanelVisible(titlePanel, false);
                SetPanelVisible(pauseMenuPanel, false);
                SetPanelVisible(gameUIPanel, true);
                break;
        }
    }

    private void CloseStageSelectSilently()
    {
        if (stageSelectPanel != null)
        {
            stageSelectPanel.SetActive(false);
        }
    }

    public void LoadStage(StageData stageData)
    {
        if (stageData == null)
        {
            Debug.LogWarning("Cannot load a null StageData.");
            return;
        }

        currentStage = stageData;
        SetPanelVisible(helpPanel, false);
        SetAllClearPanelVisible(false);

        BoardGenerator targetBoardGenerator = boardGenerator != null
            ? boardGenerator
            : FindFirstObjectByType<BoardGenerator>();
        PuzzleManager targetPuzzleManager = puzzleManager != null
            ? puzzleManager
            : PuzzleManager.Instance != null
                ? PuzzleManager.Instance
                : FindFirstObjectByType<PuzzleManager>();

        if (targetBoardGenerator == null)
        {
            Debug.LogWarning("StageLoader could not find a BoardGenerator.");
        }
        else
        {
            targetBoardGenerator.LoadBoardText(stageData.boardText);
            targetBoardGenerator.GenerateBoard();
            FitCameraToBoard(targetBoardGenerator);
        }

        if (targetPuzzleManager == null)
        {
            Debug.LogWarning("StageLoader could not find a PuzzleManager.");
            return;
        }

        targetPuzzleManager.HideClearUiForStageStart();
        targetPuzzleManager.LoadStage(stageData);
    }

    private void HideGameplayClearUi()
    {
        PuzzleManager targetPuzzleManager = puzzleManager != null
            ? puzzleManager
            : PuzzleManager.Instance != null
                ? PuzzleManager.Instance
                : FindFirstObjectByType<PuzzleManager>();

        if (targetPuzzleManager != null)
        {
            targetPuzzleManager.HideClearUiForStageStart();
        }
    }

    private void UpdateStageText()
    {
        if (stageText != null)
        {
            stageText.text = $"Stage {currentStageIndex + 1}";
        }
    }

    private void BuildStageSelectButtons()
    {
        if (stageButtonPrefab == null)
        {
            Debug.LogWarning("StageLoader stageButtonPrefab is not assigned.");
            return;
        }

        Transform groupContainer = stageGroupContainer != null ? stageGroupContainer : stageButtonContainer;

        if (groupContainer == null)
        {
            Debug.LogWarning("StageLoader stageGroupContainer or stageButtonContainer is not assigned.");
            return;
        }

        DisableLayoutComponents(groupContainer.gameObject);
        ClearChildren(groupContainer);

        if (stages == null)
        {
            return;
        }

        float currentY = stageSelectStartPosition.y;

        foreach (StageCategory category in System.Enum.GetValues(typeof(StageCategory)))
        {
            List<int> stageIndices = GetStageIndicesByCategory(category);

            if (stageIndices.Count == 0)
            {
                continue;
            }

            StageGroupView stageGroup = CreateStageGroup(category, groupContainer);
            int buttonCount = stageIndices.Count;
            int buttonsPerRow = Mathf.Max(1, stageButtonsPerRow);
            int rowCount = Mathf.Max(1, Mathf.CeilToInt(buttonCount / (float)buttonsPerRow));
            float buttonAreaHeight = rowCount * stageButtonHeight + Mathf.Max(0, rowCount - 1) * stageButtonSpacingY;
            float groupHeight = categoryTitleHeight + titleToButtonSpacing + buttonAreaHeight + categorySpacing;

            ConfigureStageGroupManualLayout(stageGroup, currentY, groupHeight, buttonAreaHeight);

            for (int localIndex = 0; localIndex < stageIndices.Count; localIndex++)
            {
                GameObject buttonObject = CreateStageSelectButton(stageIndices[localIndex], stageGroup.buttonContainer);
                PositionStageSelectButton(buttonObject, localIndex);
            }

            currentY -= groupHeight;
        }
    }

    private StageSelectOpenSource GetStageSelectOpenSourceFromActivePanel()
    {
        if (IsPanelActive(titlePanel))
        {
            return StageSelectOpenSource.Title;
        }

        if (IsPanelActive(pauseMenuPanel))
        {
            return StageSelectOpenSource.Pause;
        }

        return StageSelectOpenSource.Game;
    }

    private List<int> GetStageIndicesByCategory(StageCategory category)
    {
        List<int> stageIndices = new List<int>();

        for (int i = 0; i < stages.Count; i++)
        {
            StageCategory stageCategory = stages[i] != null
                ? stages[i].stageCategory
                : StageCategory.Standard;

            if (stageCategory == category)
            {
                stageIndices.Add(i);
            }
        }

        return stageIndices;
    }

    private StageGroupView CreateStageGroup(StageCategory category, Transform groupContainer)
    {
        GameObject groupObject = stageGroupPrefab != null
            ? Instantiate(stageGroupPrefab, groupContainer)
            : CreateDefaultStageGroup(groupContainer);

        groupObject.name = $"{category}Group";
        DisableLayoutComponents(groupObject);

        TextMeshProUGUI titleText = FindChildComponentByName<TextMeshProUGUI>(groupObject.transform, "GroupTitleText");
        if (titleText != null)
        {
            titleText.text = category.ToString();
        }
        else
        {
            Debug.LogWarning($"{groupObject.name} does not have a GroupTitleText.");
        }

        Transform buttonContainer = FindChildTransformByName(groupObject.transform, "StageButtonContainer");
        if (buttonContainer == null)
        {
            Debug.LogWarning($"{groupObject.name} does not have a StageButtonContainer. Buttons will be added to the group root.");
            buttonContainer = groupObject.transform;
        }
        else
        {
            DisableLayoutComponents(buttonContainer.gameObject);
        }

        return new StageGroupView
        {
            groupObject = groupObject,
            buttonContainer = buttonContainer
        };
    }

    private void ConfigureStageGroupManualLayout(StageGroupView stageGroup, float currentY, float groupHeight, float buttonAreaHeight)
    {
        if (stageGroup == null || stageGroup.groupObject == null || stageGroup.buttonContainer == null)
        {
            return;
        }

        RectTransform groupRect = stageGroup.groupObject.GetComponent<RectTransform>();
        if (groupRect != null)
        {
            groupRect.anchorMin = new Vector2(0f, 1f);
            groupRect.anchorMax = new Vector2(0f, 1f);
            groupRect.pivot = new Vector2(0f, 1f);
            groupRect.anchoredPosition = new Vector2(stageSelectStartPosition.x, currentY);
            groupRect.sizeDelta = new Vector2(
                stageButtonsPerRow * stageButtonWidth + Mathf.Max(0, stageButtonsPerRow - 1) * stageButtonSpacingX,
                groupHeight);
        }

        TextMeshProUGUI titleText = FindChildComponentByName<TextMeshProUGUI>(stageGroup.groupObject.transform, "GroupTitleText");
        if (titleText != null)
        {
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(groupRect != null ? groupRect.sizeDelta.x : 0f, categoryTitleHeight);
        }

        RectTransform buttonContainerRect = stageGroup.buttonContainer as RectTransform;
        if (buttonContainerRect != null)
        {
            buttonContainerRect.anchorMin = new Vector2(0f, 1f);
            buttonContainerRect.anchorMax = new Vector2(0f, 1f);
            buttonContainerRect.pivot = new Vector2(0f, 1f);
            buttonContainerRect.anchoredPosition = Vector2.zero;
            buttonContainerRect.sizeDelta = new Vector2(groupRect != null ? groupRect.sizeDelta.x : 0f, buttonAreaHeight);
        }
    }

    private void DisableLayoutComponents(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return;
        }

        foreach (LayoutGroup layoutGroup in targetObject.GetComponents<LayoutGroup>())
        {
            layoutGroup.enabled = false;
        }

        ContentSizeFitter contentSizeFitter = targetObject.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = false;
        }

        LayoutElement layoutElement = targetObject.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.enabled = false;
        }
    }

    private GameObject CreateDefaultStageGroup(Transform groupContainer)
    {
        GameObject groupObject = new GameObject("StageGroup", typeof(RectTransform));
        groupObject.transform.SetParent(groupContainer, false);

        GameObject titleObject = new GameObject("GroupTitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(groupObject.transform, false);

        TextMeshProUGUI titleText = titleObject.GetComponent<TextMeshProUGUI>();
        titleText.fontSize = 24f;
        titleText.alignment = TextAlignmentOptions.Left;

        GameObject buttonContainerObject = new GameObject("StageButtonContainer", typeof(RectTransform));
        buttonContainerObject.transform.SetParent(groupObject.transform, false);

        return groupObject;
    }

    private GameObject CreateStageSelectButton(int stageIndex, Transform buttonContainer)
    {
        GameObject buttonObject = Instantiate(stageButtonPrefab, buttonContainer);
        StageSelectButton stageSelectButton = buttonObject.GetComponent<StageSelectButton>();

        if (stageSelectButton == null)
        {
            Debug.LogWarning($"{stageButtonPrefab.name} does not have a StageSelectButton component.");
            return buttonObject;
        }

        stageSelectButton.Setup(this, stageIndex, stages[stageIndex], IsStageUnlocked(stageIndex));
        return buttonObject;
    }

    private void PositionStageSelectButton(GameObject buttonObject, int localIndex)
    {
        if (buttonObject == null)
        {
            return;
        }

        DisableLayoutComponents(buttonObject);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (buttonRect == null)
        {
            return;
        }

        int buttonsPerRow = Mathf.Max(1, stageButtonsPerRow);
        int row = localIndex / buttonsPerRow;
        int col = localIndex % buttonsPerRow;
        float x = col * (stageButtonWidth + stageButtonSpacingX);
        float y = -(categoryTitleHeight + titleToButtonSpacing) - row * (stageButtonHeight + stageButtonSpacingY);

        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = new Vector2(x, y);
        buttonRect.sizeDelta = new Vector2(stageButtonWidth, stageButtonHeight);
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private Transform FindChildTransformByName(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private T FindChildComponentByName<T>(Transform root, string childName) where T : Component
    {
        Transform child = FindChildTransformByName(root, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private void LoadStageProgress()
    {
        highestUnlockedStageIndex = PlayerPrefs.GetInt(
            HighestUnlockedStageIndexKey,
            highestUnlockedStageIndex
        );
        highestUnlockedStageIndex = ClampHighestUnlockedStageIndex(highestUnlockedStageIndex);
    }

    private void SaveStageProgress()
    {
        highestUnlockedStageIndex = ClampHighestUnlockedStageIndex(highestUnlockedStageIndex);
        PlayerPrefs.SetInt(HighestUnlockedStageIndexKey, highestUnlockedStageIndex);
        PlayerPrefs.Save();
    }

    private void SaveLastPlayedStageIndex(int index)
    {
        PlayerPrefs.SetInt(LastPlayedStageIndexKey, ClampLastPlayedStageIndex(index));
        PlayerPrefs.Save();
    }

    private void FitCameraToBoard(BoardGenerator targetBoardGenerator)
    {
        if (targetBoardGenerator == null || !targetBoardGenerator.TryGetBoardBounds(out Bounds bounds))
        {
            return;
        }

        Camera cameraToFit = targetCamera != null ? targetCamera : Camera.main;

        if (cameraToFit == null)
        {
            Debug.LogWarning("StageLoader could not find a camera to fit to the board.");
            return;
        }

        if (!cameraToFit.orthographic)
        {
            Debug.LogWarning($"{cameraToFit.name} is not orthographic. Board camera fitting was skipped.");
            return;
        }

        float aspect = Mathf.Max(cameraToFit.aspect, 0.01f);
        float verticalSize = bounds.size.y / 2f + cameraPadding;
        float horizontalSize = bounds.size.x / (2f * aspect) + cameraPadding;
        float requiredSize = Mathf.Max(verticalSize, horizontalSize);
        float minSize = Mathf.Max(0.01f, minOrthographicSize);
        float maxSize = Mathf.Max(minSize, maxOrthographicSize);

        cameraToFit.orthographicSize = Mathf.Clamp(requiredSize, minSize, maxSize);

        Vector3 center = bounds.center;
        Vector3 cameraPosition = cameraToFit.transform.position;
        cameraToFit.transform.position = new Vector3(
            center.x + cameraOffset.x,
            center.y + cameraOffset.y,
            cameraPosition.z
        );
    }

    private int ClampHighestUnlockedStageIndex(int index)
    {
        if (stages == null || stages.Count == 0)
        {
            return 0;
        }

        return Mathf.Clamp(index, 0, stages.Count - 1);
    }

    private int GetClampedLastPlayedStageIndex()
    {
        return ClampLastPlayedStageIndex(PlayerPrefs.GetInt(LastPlayedStageIndexKey, 0));
    }

    private int ClampLastPlayedStageIndex(int index)
    {
        if (stages == null || stages.Count == 0)
        {
            return 0;
        }

        int clampedIndex = Mathf.Clamp(index, 0, stages.Count - 1);
        int clampedHighestUnlockedIndex = ClampHighestUnlockedStageIndex(highestUnlockedStageIndex);
        return Mathf.Min(clampedIndex, clampedHighestUnlockedIndex);
    }

    private void EnsureHelpPanelBlocksRaycasts()
    {
        if (helpPanel == null)
        {
            return;
        }

        CanvasGroup canvasGroup = helpPanel.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = helpPanel.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Image backgroundImage = helpPanel.GetComponent<Image>();

        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = true;
        }
    }

    private void ProcessStageHelp(StageData stageData)
    {
        if (stageData == null)
        {
            return;
        }

        bool hasEveryTimeHelp = stageData.helpPageToShowEveryTime != HelpPageType.None;
        ProcessHelpPageUnlock(stageData, !hasEveryTimeHelp);

        Debug.Log($"[HELP DEBUG] Checking everyTime help: {stageData.helpPageToShowEveryTime}");

        if (hasEveryTimeHelp)
        {
            Debug.Log($"[HELP DEBUG] Showing everyTime help: {stageData.helpPageToShowEveryTime}");
            ShowHelpPage(stageData.helpPageToShowEveryTime);
        }
    }

    private void ProcessHelpPageUnlock(StageData stageData, bool allowAutoShow)
    {
        if (stageData == null)
        {
            return;
        }

        HelpPageType helpPage = GetStageHelpPageToUnlock(stageData);

        if (helpPage == HelpPageType.None)
        {
            return;
        }

        UnlockHelpPage(helpPage);

        if (!allowAutoShow || !stageData.forceShowHelpOnFirstUnlock || IsHelpPageShown(helpPage))
        {
            return;
        }

        if (ShowHelpPage(helpPage))
        {
            SetHelpPageShown(helpPage);
        }
    }

    private HelpPageType GetStageHelpPageToUnlock(StageData stageData)
    {
        if (stageData.helpPageToUnlock != HelpPageType.None)
        {
            return stageData.helpPageToUnlock;
        }

        return currentStageIndex == 0 ? HelpPageType.HelpA_Basic : HelpPageType.None;
    }

    public bool ShowHelpPage(HelpPageType helpPage)
    {
        Debug.Log($"[HELP DEBUG] ShowHelpPage called: {helpPage}");

        if (!ApplyHelpPageContent(helpPage))
        {
            return false;
        }

        ShowHelpPanel();
        SetPanelVisible(helpListView, false);
        SetPanelVisible(helpPageView, true);
        return true;
    }

    private bool ApplyHelpPageContent(HelpPageType helpPage)
    {
        ResolveHelpTextReferences();

        HelpPageType resolvedHelpPage = helpPage == HelpPageType.None
            ? HelpPageType.HelpA_Basic
            : helpPage;
        HelpPageData pageData = GetHelpPageData(resolvedHelpPage);
        currentDisplayedHelpPage = pageData;

        if (pageData == null)
        {
            Debug.LogWarning($"[HELP DEBUG] HelpPageData not found for type={resolvedHelpPage}. HelpPages count={helpPages.Count}");
            return false;
        }

        if (helpTitleText != null)
        {
            helpTitleText.text = pageData.title;
        }

        if (helpMessageText != null)
        {
            helpMessageText.text = pageData.message;
        }

        ApplyHelpImage(pageData.image);

        return true;
    }

    private void ApplyHelpImage(Sprite sprite)
    {
        if (helpImage == null)
        {
            return;
        }

        if (sprite == null)
        {
            helpImage.gameObject.SetActive(false);
            return;
        }

        helpImage.gameObject.SetActive(true);
        helpImage.sprite = sprite;
        helpImage.preserveAspect = true;

        RectTransform imageRect = helpImage.rectTransform;
        RectTransform boundsRect = imageRect.parent as RectTransform;

        if (boundsRect == null)
        {
            return;
        }

        float maxWidth = boundsRect.rect.width;
        float maxHeight = boundsRect.rect.height;
        float spriteWidth = sprite.rect.width;
        float spriteHeight = sprite.rect.height;

        if (spriteWidth <= 0f || spriteHeight <= 0f || maxWidth <= 0f || maxHeight <= 0f)
        {
            return;
        }

        float spriteAspect = spriteWidth / spriteHeight;
        float boundsAspect = maxWidth / maxHeight;
        float finalWidth;
        float finalHeight;

        if (spriteAspect > boundsAspect)
        {
            finalWidth = maxWidth;
            finalHeight = maxWidth / spriteAspect;
        }
        else
        {
            finalHeight = maxHeight;
            finalWidth = maxHeight * spriteAspect;
        }

        imageRect.sizeDelta = new Vector2(finalWidth, finalHeight);
        imageRect.anchoredPosition = Vector2.zero;
    }

    private HelpPageData GetHelpPageData(HelpPageType type)
    {
        return helpPages.Find(page => page != null && page.helpPageType == type);
    }

    private void BuildHelpPageButtons()
    {
        if (helpButtonContainer == null)
        {
            Debug.LogWarning("StageLoader helpButtonContainer is not assigned.");
            return;
        }

        for (int i = helpButtonContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(helpButtonContainer.GetChild(i).gameObject);
        }

        if (helpPageButtonPrefab == null)
        {
            Debug.LogWarning("StageLoader helpPageButtonPrefab is not assigned.");
            return;
        }

        foreach (HelpPageData page in helpPages)
        {
            if (page == null || page.helpPageType == HelpPageType.None || !IsHelpPageUnlocked(page.helpPageType))
            {
                continue;
            }

            GameObject buttonObject = Instantiate(helpPageButtonPrefab, helpButtonContainer);
            HelpPageButton helpPageButton = buttonObject.GetComponent<HelpPageButton>();

            if (helpPageButton == null)
            {
                Debug.LogWarning($"{helpPageButtonPrefab.name} does not have a HelpPageButton component.");
                continue;
            }

            helpPageButton.Setup(this, page);
        }
    }

    private void UnlockHelpPage(HelpPageType helpPage)
    {
        if (helpPage == HelpPageType.None)
        {
            return;
        }

        PlayerPrefs.SetInt(GetHelpPageUnlockedKey(helpPage), 1);
        PlayerPrefs.Save();
    }

    private bool IsHelpPageUnlocked(HelpPageType helpPage)
    {
        return PlayerPrefs.GetInt(GetHelpPageUnlockedKey(helpPage), 0) != 0;
    }

    private string GetHelpPageUnlockedKey(HelpPageType helpPage)
    {
        return HelpPageUnlockedKeyPrefix + helpPage;
    }

    private bool IsHelpPageShown(HelpPageType helpPage)
    {
        return PlayerPrefs.GetInt(GetHelpPageShownKey(helpPage), 0) != 0;
    }

    private void SetHelpPageShown(HelpPageType helpPage)
    {
        PlayerPrefs.SetInt(GetHelpPageShownKey(helpPage), 1);
        PlayerPrefs.Save();
    }

    private string GetHelpPageShownKey(HelpPageType helpPage)
    {
        return HelpPageShownKeyPrefix + helpPage;
    }

    private void ResetHelpPageProgress()
    {
        PlayerPrefs.DeleteKey(GetHelpPageUnlockedKey(HelpPageType.HelpA_Basic));
        PlayerPrefs.DeleteKey(GetHelpPageUnlockedKey(HelpPageType.HelpB_ExactCover));
        PlayerPrefs.DeleteKey(GetHelpPageUnlockedKey(HelpPageType.HelpC_FixedPieces));
        PlayerPrefs.DeleteKey(GetHelpPageShownKey(HelpPageType.HelpA_Basic));
        PlayerPrefs.DeleteKey(GetHelpPageShownKey(HelpPageType.HelpB_ExactCover));
        PlayerPrefs.DeleteKey(GetHelpPageShownKey(HelpPageType.HelpC_FixedPieces));
    }

    private void ResolveHelpTextReferences()
    {
        if (helpPanel == null || (helpTitleText != null && helpMessageText != null))
        {
            return;
        }

        TextMeshProUGUI[] texts = helpPanel.GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI text in texts)
        {
            if (text == null)
            {
                continue;
            }

            if (helpTitleText == null)
            {
                helpTitleText = text;
                continue;
            }

            if (helpMessageText == null && text != helpTitleText)
            {
                helpMessageText = text;
                return;
            }
        }
    }

    private void SetAllClearPanelVisible(bool visible)
    {
        if (allClearPanel != null)
        {
            allClearPanel.SetActive(visible);
        }
    }

    public bool IsFinalStage()
    {
        return stages != null && stages.Count > 0 && currentStageIndex >= stages.Count - 1;
    }

    public bool IsLastStageOfCurrentCategory()
    {
        if (stages == null || currentStageIndex < 0 || currentStageIndex >= stages.Count || currentStage == null)
        {
            return false;
        }

        StageCategory currentCategory = currentStage.stageCategory;

        for (int i = currentStageIndex + 1; i < stages.Count; i++)
        {
            if (stages[i] != null && stages[i].stageCategory == currentCategory)
            {
                return false;
            }
        }

        return true;
    }

    public string GetCurrentCategoryClearMessage()
    {
        if (currentStage == null)
        {
            return defaultCategoryClearMessage;
        }

        StageCategory category = currentStage.stageCategory;

        foreach (CategoryClearMessage entry in categoryClearMessages)
        {
            if (entry != null && entry.category == category && !string.IsNullOrEmpty(entry.message))
            {
                return entry.message;
            }
        }

        return defaultCategoryClearMessage;
    }

    public void ShowCategoryClear()
    {
        ShowSpecialClear(GetCurrentCategoryClearMessage(), true, false);
    }

    public void ShowAllClear()
    {
        ShowSpecialClear(allClearMessage, false, true);
    }

    private void ShowSpecialClear(string message, bool showNextStageButton, bool playAllClearSound)
    {
        if (allClearText != null)
        {
            allClearText.text = message;
        }

        SetAllClearPanelVisible(true);
        if (playAllClearSound)
        {
            PlaySE(allClearSE, allClearSEVolume);
        }
        SetPanelVisible(helpPanel, false);
        SetPanelVisible(gameUIPanel, false);
        CloseStageSelectSilently();
        SetPanelVisible(pauseMenuPanel, false);
        SetPanelVisible(titlePanel, false);

        PuzzleManager targetPuzzleManager = puzzleManager != null
            ? puzzleManager
            : PuzzleManager.Instance != null
                ? PuzzleManager.Instance
                : FindFirstObjectByType<PuzzleManager>();

        if (targetPuzzleManager != null)
        {
            targetPuzzleManager.HideClearDisplay();
            targetPuzzleManager.SetNextStageButtonVisibleExternal(showNextStageButton);
        }

        if (allClearPanel == null)
        {
            Debug.Log(message);
        }
    }

    private void SetPanelVisible(GameObject panel, bool visible)
    {
        if (panel != null)
        {
            panel.SetActive(visible);
        }
    }

    private bool IsPanelActive(GameObject panel)
    {
        return panel != null && panel.activeInHierarchy;
    }

    private void PlayBGM(AudioClip clip)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(clip);
        }
    }

    private void PlaySE(AudioClip clip, float volumeScale = 1f)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(clip, volumeScale);
        }
    }

}

[System.Serializable]
public class CategoryClearMessage
{
    public StageCategory category;

    [TextArea(1, 3)]
    public string message;
}

public class StageGroupView
{
    public GameObject groupObject;
    public Transform buttonContainer;
}
