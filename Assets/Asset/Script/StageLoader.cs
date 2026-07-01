using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageLoader : MonoBehaviour
{
    private const string HighestUnlockedStageIndexKey = "HighestUnlockedStageIndex";

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
            Debug.LogWarning("未解放のステージです。");
            return;
        }

        currentStageIndex = index;
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

        int targetIndex = currentStageIndex;

        if (targetIndex < 0 || targetIndex >= stages.Count)
        {
            targetIndex = 0;
        }

        if (!IsStageUnlocked(targetIndex))
        {
            targetIndex = Mathf.Clamp(highestUnlockedStageIndex, 0, stages.Count - 1);
        }

        LoadStageByIndexInternal(targetIndex);
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
        CloseStageSelectSilently();
        SetPanelVisible(titlePanel, false);
        SetPanelVisible(pauseMenuPanel, false);
        SetPanelVisible(gameUIPanel, true);
        SetPanelVisible(helpPanel, true);
    }

    public void CloseHelp()
    {
        PlaySE(cancelSE, cancelSEVolume);
        SetPanelVisible(helpPanel, false);
        SetPanelVisible(pauseMenuPanel, true);
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

        targetPuzzleManager.LoadPieceStocks(stageData.pieceStocks);
        targetPuzzleManager.ResetPuzzle();
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

    private int ClampHighestUnlockedStageIndex(int index)
    {
        if (stages == null || stages.Count == 0)
        {
            return 0;
        }

        return Mathf.Clamp(index, 0, stages.Count - 1);
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
