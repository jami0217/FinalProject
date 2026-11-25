using System.Text;
using UnityEngine;

namespace FinalProject.Gym.Core
{
    public sealed class GymProgressHUD : MonoBehaviour
    {
        private GymGameBridge bridge;
        private GymCoreUI ui;
        private GymProgressManager source;
        public void Bind(GymGameBridge context, GymCoreUI view)
        {
            Unsubscribe(); bridge = context; ui = view;
            source = bridge.Progress;
            source.ProgressChanged += Refresh;
            if (ui.restartButton != null) ui.restartButton.onClick.AddListener(Restart);
            Refresh();
        }
        private void Update()
        {
            if (bridge == null || !bridge.isActiveAndEnabled || ui == null) return;
            if (ui.progressPanel != null) ui.progressPanel.SetActive(!bridge.IsMinigameActive && !bridge.IsDialogueOpen);
            if (ui.victoryPanel != null) ui.victoryPanel.SetActive(bridge.IsVictoryVisible);
        }
        private void Refresh()
        {
            if (bridge == null || ui == null) return;
            if (ui.progressText != null) ui.progressText.text = $"完成器材 {bridge.Progress.CompletedCount}/{bridge.Progress.TotalCount}";
            if (ui.stationStatusText == null) return;
            StringBuilder lines = new StringBuilder();
            foreach (GymStationDefinition station in bridge.Progress.Stations)
            {
                GymStationState state = bridge.Progress.GetState(station.id);
                lines.Append(state == GymStationState.Completed ? "✓ " : state == GymStationState.InProgress ? "… " : "○ ");
                lines.Append(station.displayName).Append(state == GymStationState.Completed ? " 已完成" : state == GymStationState.InProgress ? " 進行中" : " 未開始").Append('\n');
            }
            ui.stationStatusText.text = lines.ToString();
        }
        private void Restart() { if (bridge != null) bridge.RestartRound(); }
        private void Unsubscribe()
        {
            if (source != null) source.ProgressChanged -= Refresh;
            source = null;
            if (ui != null && ui.restartButton != null) ui.restartButton.onClick.RemoveListener(Restart);
        }
        private void OnDisable() { Unsubscribe(); }
        private void OnEnable() { if (Application.isPlaying && bridge != null && ui != null) Bind(bridge, ui); }
    }
}
