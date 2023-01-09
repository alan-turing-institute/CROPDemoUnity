using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// Attached to "StickFigure" GameObject - controls the movement of the stick figure through the farm.

public class PersonBehaviour : MonoBehaviour
{
    // we will either be moving, or rotating on the spot, or pausing
    enum State {
        Moving,
        RotatingLeft,
        RotatingRight,
        Paused
    }

    State currentState;

    // List of things the character can do
    public enum Action {
        Move,
        Rotate90Left,
        Rotate90Right,
        Pause,
        Rotate180
    }
    float targetRotation;
    // see how long we have paused, and allow limit to be set
    float pauseTime = 0F;
    public float pauseLength = 5F;
    // the character will step through a list of actions, defined by a GameObject, with a tag
    public List<GameObject>  actionLocations;
    // keep track of where we are in the list of actions/locations
    int actionIndex;
    // define via the inspector how fast the character moves
    public float moveSpeed;
    public float rotateSpeed;
    //fire an Event when we have moved or rotated to the desired position
    UnityEvent inPositionEvent = new UnityEvent();

    // Start is called before the first frame update
    void Start() {
        actionIndex = 0;
        transform.position = actionLocations[actionIndex].transform.position;
        SetStateFromTag();
        inPositionEvent.AddListener(NextAction);
    }

    void SetStateFromTag() {
        string actionTag = actionLocations[actionIndex].tag;
        Action result;
        if (Enum.TryParse(actionTag, out result)) {
            switch (result) {
                case Action.Move:
                    currentState = State.Moving;
                    break;
                case Action.Rotate90Left:
                    currentState =  State.RotatingLeft;
                    targetRotation = transform.localEulerAngles.y - 90F;
                    break;
                case Action.Rotate90Right:
                    currentState =  State.RotatingRight;
                    targetRotation = transform.localEulerAngles.y + 90F;
                    break;
                case Action.Rotate180:
                    currentState =  State.RotatingRight;
                    targetRotation = transform.localEulerAngles.y + 180F;     
                    break;           
                case Action.Pause:
                    currentState =  State.Paused;
                    // reset the pause counter
                    pauseTime = 0F;
                    break;
                default:
                    break;
            }
        }
        // clamp the targetRotation to be between -180 and 180
        if (targetRotation > 180) targetRotation = targetRotation - 360F;
    }

    // Update is called once per frame
    void Update() {
        CheckPositionRotationPause();
       
        if (currentState == State.Moving) {
            Vector3 moveDirection = (actionLocations[actionIndex].transform.position - transform.position);
            moveDirection.Normalize();
            Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x + movement.x, 
                                             transform.position.y, 
                                             transform.position.z + movement.z);
        } else if (currentState == State.RotatingLeft) {
            transform.Rotate(0,-10*rotateSpeed*Time.deltaTime,0);
        } else if (currentState == State.RotatingRight) {
            transform.Rotate(0, 10*rotateSpeed*Time.deltaTime,0);
        } else if (currentState == State.Paused) {
            pauseTime += Time.deltaTime;
        }
    }

    void CheckPositionRotationPause() {
        if (currentState == State.Paused) {
            if (pauseTime > pauseLength) {
                inPositionEvent.Invoke();
                return;
            }
        } else if (currentState == State.Moving) {
            float distance = Vector3.Distance(transform.position, actionLocations[actionIndex].transform.position);
            if (distance < 10F)  { 
                inPositionEvent.Invoke();
                return;
            }
        } else if ((currentState == State.RotatingLeft) || (currentState == State.RotatingRight)) {
            float currentRotation = transform.localEulerAngles.y;
            if (currentRotation > 180F) currentRotation =  currentRotation - 360F;
            float deltaRotation = Mathf.Abs(currentRotation - targetRotation);
            if (deltaRotation < 2F) {
                inPositionEvent.Invoke();
                return;
            }
        }
    }

    void NextAction() {
        actionIndex += 1;
        if (actionIndex == actionLocations.Count) actionIndex = 0;
        SetStateFromTag();
    }
}
