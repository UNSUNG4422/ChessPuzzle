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
    [SerializeField] private GameObject stageSelectPanel;
    [SerializeField] private Transform stageButtonContainer;
    [SerializeField] private GameObject stageButtonPrefab;
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

    private void Start()
    {
        SetAllClearPanelVisible(false);

        if (stages == null || stages.Count == 0)
        {
            Debug.LogWarning("StageLoader has no stages assigned.");
            return;
        }

        LoadStageProgress();

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
        SetPanelVisible(helpPanel, false);
        SetPanelVisible(titlePanel, false);
        SetPanelVisible(pauseMenuPanel, false);
        SetPanelVisible(gameUIPanel, true);
        SetAllClearPanelVisible(false);
        LoadStage(currentStage);
        UpdateStageText();
        CloseStageSelect();
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
        SetPanelVisible(helpPanel, false);
        SetPanelVisible(titlePanel, false);
        SetPanelVisible(pauseMenuPanel, false);
        SetPanelVisible(gameUIPanel, false);
        OpenStageSelect();
    }

    public void OpenStageSelectFromPause()
    {
        SetPanelVisible(helpPanel, false);
        SetPanelVisible(pauseMenuPanel, false);
        OpenStageSelect();
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

    public void OpenStageSelect()
    {
        PlaySE(buttonSE, buttonSEVolume);
        SetPanelVisible(helpPanel, false);
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
        }

        if (targetPuzzleManager == null)
        {
            Debug.LogWarning("StageLoader could not find a PuzzleManager.");
            return;
        }

        targetPuzzleManager.LoadStage(stageData);
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
        if (stageButtonContainer == null)
        {
            Debug.LogWarning("StageLoader stageButtonContainer is not assigned.");
            return;
        }

        if (stageButtonPrefab == null)
        {
            Debug.LogWarning("StageLoader stageButtonPrefab is not assigned.");
            return;
        }

        ConfigureStageButtonGrid();

        for (int i = stageButtonContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(stageButtonContainer.GetChild(i).gameObject);
        }

        if (stages == null)
        {
            return;
        }

        for (int i = 0; i < stages.Count; i++)
        {
            GameObject buttonObject = Instantiate(stageButtonPrefab, stageButtonContainer);
            StageSelectButton stageSelectButton = buttonObject.GetComponent<StageSelectButton>();

            if (stageSelectButton == null)
            {
                Debug.LogWarning($"{stageButtonPrefab.name} does not have a StageSelectButton component.");
                continue;
            }

            stageSelectButton.Setup(this, i, stages[i], IsStageUnlocked(i));
        }
    }

    private void ConfigureStageButtonGrid()
    {
        GridLayoutGroup gridLayoutGroup = stageButtonContainer.GetComponent<GridLayoutGroup>();

        if (gridLayoutGroup == null)
        {
            Debug.LogWarning("StageButtonContainer should have a Grid Layout Group component.");
            return;
        }

        gridLayoutGroup.cellSize = new Vector2(60f, 60f);
        gridLayoutGroup.spacing = new Vector2(10f, 10f);
        gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = 5;
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

    private void ShowAllClear()
    {
        if (allClearText != null)
        {
            allClearText.text = "ALL CLEAR!";
        }

        SetAllClearPanelVisible(true);
        PlaySE(allClearSE, allClearSEVolume);
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
        }

        if (allClearPanel == null)
        {
            Debug.Log("All stages clear!");
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
