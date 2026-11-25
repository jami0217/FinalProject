using UnityEngine;

public class GymEquipmentInteractable : MonoBehaviour
{
    public string equipmentId;
    public string displayName;
    [TextArea] public string description;

    private void OnGUI()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 screenPosition = mainCamera.WorldToScreenPoint(transform.position);
        if (screenPosition.z <= 0f)
        {
            return;
        }

        Rect labelRect = new Rect(screenPosition.x - 100f, Screen.height - screenPosition.y - 42f, 200f, 28f);
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        string status = GymGameManager.Instance != null && GymGameManager.Instance.IsCompleted(equipmentId)
            ? " (已完成)"
            : string.Empty;
        GUI.Label(labelRect, displayName + status, style);
    }
}