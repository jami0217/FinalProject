using FinalProject.Gym.Core;
using FinalProject.Gym.Squat;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GymGameManager : MonoBehaviour
{
    public static GymGameManager Instance { get; private set; }
    public bool IsMiniGameOpen { get; private set; }
    public bool InteractionsBlocked { get; set; }
    public bool UseExternalHud { get; set; }
    private GymProgressManager progress;
    public GymProgressManager Progress
    {
        get
        {
            if (progress == null) progress = GetComponent<GymProgressManager>();
            if (progress == null) progress = gameObject.AddComponent<GymProgressManager>();
            return progress;
        }
    }
    public bool IsSquatMiniGameOpen => IsMiniGameOpen && activeEquipment != null && activeEquipment.equipmentId == "Squat";

    [SerializeField] private SquatMinigameController squatMinigame;

    private GymEquipmentInteractable activeEquipment;
    private Player activePlayer;
    private string interactionPrompt = string.Empty;
    private int activeSceneHandle;

    public static GymGameManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GymGameManager existing = FindFirstObjectByType<GymGameManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject managerObject = new GameObject("GymGameManager");
        return managerObject.AddComponent<GymGameManager>();
    }

    public bool IsCompleted(string equipmentId)
    {
        return Progress.IsCompleted(equipmentId);
    }

    public void SetInteractionPrompt(string prompt)
    {
        interactionPrompt = InteractionsBlocked ? string.Empty : prompt;
    }

    public void OpenMiniGame(GymEquipmentInteractable equipment, Player player)
    {
        if (equipment == null || IsMiniGameOpen || InteractionsBlocked)
        {
            return;
        }

        activeEquipment = equipment;
        activeSceneHandle = equipment.gameObject.scene.handle;
        activePlayer = player;
        IsMiniGameOpen = true;
        interactionPrompt = string.Empty;
        Progress.StartStation(equipment.equipmentId);

        if (activePlayer != null)
        {
            activePlayer.enabled = false;
        }

        if (IsSquatMiniGameOpen)
        {
            if (squatMinigame == null)
                squatMinigame = FindFirstObjectByType<SquatMinigameController>(FindObjectsInactive.Include);
            if (squatMinigame == null)
                squatMinigame = SquatMinigameUIFactory.Create(transform);
            if (!squatMinigame.Open(CompleteSquatMiniGame, CloseMiniGame))
                CloseMiniGame();
        }
    }

    private void CompleteSquatMiniGame()
    {
        if (IsSquatMiniGameOpen && squatMinigame != null && squatMinigame.HasCompleted)
            Progress.CompleteStation(activeEquipment.equipmentId);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable() { SceneManager.sceneUnloaded += OnSceneUnloaded; }
    private void OnDisable() { SceneManager.sceneUnloaded -= OnSceneUnloaded; }
    private void OnSceneUnloaded(Scene scene)
    {
        if (!IsMiniGameOpen || scene.handle != activeSceneHandle) return;
        if (squatMinigame != null && squatMinigame.IsOpen) squatMinigame.CloseForSceneChange();
        if (IsMiniGameOpen) CloseMiniGame();
    }

    private void OnGUI()
    {
        if (IsMiniGameOpen)
        {
            if (!IsSquatMiniGameOpen) DrawMiniGame();
            return;
        }

        if (!UseExternalHud) DrawProgressHud();

        if (!InteractionsBlocked && !string.IsNullOrEmpty(interactionPrompt))
        {
            GUIStyle promptStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20
            };
            GUI.Box(new Rect(Screen.width * 0.5f - 210f, Screen.height - 78f, 420f, 52f), interactionPrompt, promptStyle);
        }
    }

    private void DrawProgressHud()
    {
        int completedCount = Progress.CompletedCount;
        GUIStyle hudStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 18,
            padding = new RectOffset(14, 14, 8, 8)
        };

        GUI.Box(new Rect(16f, 16f, 290f, 48f), "完成器材 " + completedCount + " / " + Progress.TotalCount, hudStyle);

        if (Progress.AllCompleted)
        {
            GUIStyle winStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 26,
                fontStyle = FontStyle.Bold
            };
            GUI.Box(new Rect(Screen.width * 0.5f - 190f, 76f, 380f, 56f), "恭喜達成好身材！", winStyle);
        }

    }

    private void DrawMiniGame()
    {
        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);

        float panelWidth = Mathf.Min(560f, Screen.width - 32f);
        float panelHeight = 330f;
        Rect panel = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);
        GUI.Box(panel, GUIContent.none);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            wordWrap = true
        };
        GUI.Label(new Rect(panel.x + 24f, panel.y + 32f, panel.width - 48f, 54f), activeEquipment.displayName + "訓練", titleStyle);
        GUI.Label(new Rect(panel.x + 36f, panel.y + 100f, panel.width - 72f, 72f), activeEquipment.description, bodyStyle);

        float buttonWidth = Mathf.Min(360f, panel.width - 48f);
        Rect completeButton = new Rect(panel.center.x - buttonWidth * 0.5f, panel.y + 196f, buttonWidth, 48f);
        Rect returnButton = new Rect(panel.center.x - buttonWidth * 0.5f, panel.y + 254f, buttonWidth, 42f);

        if (GUI.Button(completeButton, "完成訓練並返回健身房"))
        {
            Progress.CompleteStation(activeEquipment.equipmentId);
            CloseMiniGame();
        }

        if (GUI.Button(returnButton, "返回健身房"))
        {
            CloseMiniGame();
        }
    }

    private void CloseMiniGame()
    {
        IsMiniGameOpen = false;
        activeEquipment = null;

        if (activePlayer != null)
        {
            activePlayer.enabled = true;
            activePlayer = null;
        }
    }

    public void ResetForNewRound()
    {
        if (squatMinigame != null) squatMinigame.ResetForNewRound();
        if (IsMiniGameOpen) CloseMiniGame();
        Progress.ResetRound();
        interactionPrompt = string.Empty;
    }
}
