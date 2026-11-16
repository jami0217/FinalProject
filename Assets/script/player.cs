using UnityEngine;

public class Player : MonoBehaviour
{
    public int playerCount = 3;
    public string playerName = "Eva";
    public string playerLevel = "初級";
    public float playerSpeed = 2f;
    public float jumpForce = 5f;
    public Vector3 targetPosition;
    public Camera playerCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        //Debug.Log("moveX: " + moveX + " moveZ: " + moveZ); 
        Vector3 move = new Vector3(moveX, moveY, 0);
        transform.position += move * Time.deltaTime;
    
    }
}
