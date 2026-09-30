using UnityEngine;
using UnityEngine.InputSystem;

public class DebugMove : MonoBehaviour
{
    public float speed = 3f;
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector3 move = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) move += transform.forward;
        if (Keyboard.current.sKey.isPressed) move -= transform.forward;
        if (Keyboard.current.aKey.isPressed) move -= transform.right;
        if (Keyboard.current.dKey.isPressed) move += transform.right;

        move.y = -1f; // petite gravité simple pour rester collé au sol

        controller.Move(move * speed * Time.deltaTime);
    }
}
