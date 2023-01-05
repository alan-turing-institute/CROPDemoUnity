using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public float moveSpeed;
    public float groundDrag;
    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Rigidbody rb;
    Vector3 moveDirection;

    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        //orientation.rotation = Quaternion.Euler(0,247.7f,0);
        //print("Setting rotation of Orientation transform");
    }

    private void MyInput() {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
    }

    // Update is called once per frame
    void Update()
    {
        MyInput();
        SpeedControl();

        rb.drag = groundDrag;
        
    }

    private void FixedUpdate() {
        MovePlayer();
    }

    private void MovePlayer() {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
    }

    private void SpeedControl() {
        Vector3 currentVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (currentVelocity.magnitude > moveSpeed) {
            Vector3 limitedVelocity = currentVelocity.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }
    }
}
