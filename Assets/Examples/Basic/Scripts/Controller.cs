using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class Controller : Agent
{
    public float speed = 10.0f;
    public Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.A))
            rb.AddForce(speed * Vector3.left);
        if (Input.GetKey(KeyCode.D))
            rb.AddForce(speed * Vector3.right);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int Discrete = actions.DiscreteActions[0];
        //float Continuous = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);

        Debug.Log("Discrete : " + Discrete);
        //Debug.Log("Continuous : " + Continuous);

    }
}
