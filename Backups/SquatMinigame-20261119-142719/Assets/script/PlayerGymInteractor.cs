using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGymInteractor : MonoBehaviour
{
    private readonly HashSet<GymEquipmentInteractable> nearbyEquipment = new HashSet<GymEquipmentInteractable>();
    private GymEquipmentInteractable currentTarget;

    private void Update()
    {
        GymGameManager manager = GymGameManager.Instance;
        if (manager == null || manager.IsMiniGameOpen)
        {
            return;
        }

        currentTarget = FindNearestEquipment();
        manager.SetInteractionPrompt(currentTarget == null
            ? string.Empty
            : "按 E 開始：" + currentTarget.displayName + "訓練");

        Keyboard keyboard = Keyboard.current;
        if (currentTarget != null && keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            manager.OpenMiniGame(currentTarget, GetComponent<Player>());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GymEquipmentInteractable equipment = other.GetComponentInParent<GymEquipmentInteractable>();
        if (equipment != null)
        {
            nearbyEquipment.Add(equipment);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GymEquipmentInteractable equipment = other.GetComponentInParent<GymEquipmentInteractable>();
        if (equipment != null)
        {
            nearbyEquipment.Remove(equipment);
        }
    }

    private GymEquipmentInteractable FindNearestEquipment()
    {
        GymEquipmentInteractable nearest = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (GymEquipmentInteractable equipment in nearbyEquipment)
        {
            if (equipment == null)
            {
                continue;
            }

            float distance = (equipment.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearest = equipment;
                nearestDistance = distance;
            }
        }

        return nearest;
    }
}