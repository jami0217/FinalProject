using UnityEngine;
using UnityEngine.SceneManagement;

public static class GymStageBootstrap
{
    // Equipment is saved in SampleScene so it is visible and editable before Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHandler()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "SampleScene")
        {
            return;
        }

        GymGameManager.EnsureInstance();

        Player player = Object.FindFirstObjectByType<Player>();
        if (player == null)
        {
            Debug.LogWarning("Gym setup skipped: SampleScene does not contain a Player component.");
            return;
        }

        ConfigurePlayer(player);
    }

    private static void ConfigurePlayer(Player player)
    {
        player.gameObject.tag = "Player";

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = player.gameObject.AddComponent<Rigidbody2D>();
        }
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        if (player.GetComponent<Collider2D>() == null)
        {
            CircleCollider2D playerCollider = player.gameObject.AddComponent<CircleCollider2D>();
            playerCollider.radius = 0.35f;
        }

        if (player.GetComponent<PlayerGymInteractor>() == null)
        {
            player.gameObject.AddComponent<PlayerGymInteractor>();
        }
    }
}
