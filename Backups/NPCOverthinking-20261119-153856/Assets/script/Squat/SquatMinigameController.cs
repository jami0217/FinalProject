using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace FinalProject.Gym.Squat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SquatMinigameView))]
    public sealed class SquatMinigameController : MonoBehaviour
    {
        [SerializeField] private SquatGameSettings settings = new SquatGameSettings();
        [SerializeField] private Key timingKey = Key.Space;
        [SerializeField] private Key returnKey = Key.Escape;
        [SerializeField] private TMP_FontAsset chineseFont;
        [SerializeField] private SquatMinigameView view;

        private SquatGameModel model;
        private SquatGameSettings sessionSettings;
        private TMP_FontAsset generatedFont;
        private Action onCompleted;
        private Action onClosed;
        private bool confirmingReturn;
        private bool completionReported;
        private int openingFrame;
        private GameObject previousSelection;
        private float displayedPerfectStart = float.NaN;
        private float displayedPerfectEnd = float.NaN;

        public bool IsOpen { get; private set; }
        public bool HasCompleted => model != null && model.State == SquatGameState.Completed;

        private void Awake()
        {
            if (view == null) view = GetComponent<SquatMinigameView>();
            view.SetVisible(false);
        }

        private void OnValidate()
        {
            settings = (settings ?? new SquatGameSettings()).NormalizedCopy();
            if (timingKey == Key.None) timingKey = Key.Space;
            if (returnKey == timingKey) returnKey = Key.None;
        }

        public bool Open(Action completed, Action closed)
        {
            if (IsOpen) return false;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (!gameObject.activeInHierarchy || !enabled)
            {
                Debug.LogWarning("Squat UI cannot open: enable its controller and its parent GameObjects.", this);
                return false;
            }
            if (view == null) view = GetComponent<SquatMinigameView>();
            if (view == null || !view.BindReferences())
            {
                Debug.LogWarning("Squat UI links are incomplete. Use Tools > Gym > Create Squat UI In Current Scene, or assign the SquatMinigameView fields.", this);
                return false;
            }

            if (model == null || HasCompleted)
            {
                sessionSettings = (settings ?? new SquatGameSettings()).NormalizedCopy();
                model = new SquatGameModel(sessionSettings);
                model.Begin();
                completionReported = false;
            }
            onCompleted = completed;
            onClosed = closed;
            confirmingReturn = false;
            openingFrame = Time.frameCount;
            IsOpen = true;
            view.BindButtons(RequestReturn, Resume, Close);
            displayedPerfectStart = displayedPerfectEnd = float.NaN;
            ConfigureFont();
            EnsureEventSystem();
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            view.SetVisible(true);
            Render();
            return true;
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount == openingFrame) return;
            Keyboard keyboard = Keyboard.current;
            if (Pressed(keyboard, returnKey))
            {
                if (confirmingReturn) Resume();
                else RequestReturn();
                return;
            }
            if (confirmingReturn) return;

            model.Tick(Time.deltaTime, Pressed(keyboard, timingKey), Time.frameCount);
            if (HasCompleted && !completionReported)
            {
                completionReported = true;
                onCompleted?.Invoke();
            }
            Render();
        }

        private void RequestReturn()
        {
            if (!IsOpen) return;
            if (HasCompleted) Close();
            else
            {
                confirmingReturn = true;
                Render();
            }
        }

        private void Resume()
        {
            confirmingReturn = false;
            openingFrame = Time.frameCount;
            Render();
        }

        private void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            confirmingReturn = false;
            view.SetVisible(false);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
            previousSelection = null;
            Action callback = onClosed;
            onClosed = null;
            onCompleted = null;
            callback?.Invoke();
        }

        private void OnDisable()
        {
            if (IsOpen) Close();
        }

        private void OnDestroy()
        {
            if (view != null) view.UnbindButtons(RequestReturn, Resume, Close);
            if (generatedFont != null)
            {
                foreach (Texture2D atlas in generatedFont.atlasTextures)
                    if (atlas != null) Destroy(atlas);
                if (generatedFont.material != null) Destroy(generatedFont.material);
                Destroy(generatedFont);
            }
        }

        private void Render()
        {
            if (displayedPerfectStart != model.PerfectStart || displayedPerfectEnd != model.PerfectEnd)
            {
                view.ConfigureZones(sessionSettings, model.PerfectStart, model.PerfectEnd);
                displayedPerfectStart = model.PerfectStart;
                displayedPerfectEnd = model.PerfectEnd;
            }
            string keyLabel = timingKey == Key.Space ? "空白鍵" : timingKey.ToString();
            view.Render(model, keyLabel, confirmingReturn);
        }

        private static bool Pressed(Keyboard keyboard, Key key)
        {
            return keyboard != null && key != Key.None && keyboard[key].wasPressedThisFrame;
        }

        private void ConfigureFont()
        {
            if (chineseFont != null)
            {
                view.ApplyFont(chineseFont);
                return;
            }
            if (generatedFont == null)
            {
                string[] families = { "PingFang TC", "Microsoft JhengHei", "Noto Sans CJK TC", "Noto Sans CJK SC", "Arial Unicode MS" };
                foreach (string family in families)
                {
                    // Use only a local font for this UI; never change teammates' global TMP settings.
                    generatedFont = TMP_FontAsset.CreateFontAsset(family, "Regular", 48);
                    if (generatedFont != null) break;
                }
                if (generatedFont == null)
                    Debug.LogWarning("No supported CJK system font found. Assign a Traditional Chinese TMP Font Asset to SquatMinigameController > Chinese Font.", this);
            }
            view.ApplyFont(generatedFont);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject events = new GameObject("SquatUIEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
        }
    }
}
