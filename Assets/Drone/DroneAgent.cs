using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using PA_DronePack;

public class DroneAgent : Agent
{
    private PA_DroneController doScript;

    public DroneSetting area;
    public GameObject goal;

    float preDist;

    private Transform agentTrans;
    private Transform goalTrans;

    private Rigidbody agent_Rigidbody;

    public override void Initialize()
    {
        doScript = gameObject.GetComponent<PA_DroneController>();
        agentTrans = gameObject.transform;
        goalTrans = goal.transform;

        agent_Rigidbody = gameObject.GetComponent<Rigidbody>();

        // Academy.Instance.AgentPreStep : ������Ʈ�� �� ������ �����ϱ� ������ ȣ��Ǵ� ������ �̺�Ʈ, �߻��� ������ �ۼ��� WaitTimeInference �Լ��� ȣ���.
        Academy.Instance.AgentPreStep += WaitTimeInference;
    }

    // �� ���� ������Ʈ�� ���� ���¸� �Է��� �ִ� �Լ�,,
    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("CollectObservations");

        // .AddObservation : Unity.MLAgents.Sensors �� ���ǵǾ��ִ� �Լ��� ȯ�濡�� ����ϴ� ���� ���� ���� �߰��ϴ� �Լ�
        // �� ���� �ϳ��� VectorSensor�� ���̽� �ڵ�� ���޵Ǹ�, �� ������ ������ ����Ƽ ML-Agents�� ���� �������� �̿�

        // �Ÿ�����
        sensor.AddObservation(agentTrans.position - goalTrans.position);

        // �ӵ�����
        sensor.AddObservation(agent_Rigidbody.velocity);

        // ���ӵ�����
        sensor.AddObservation(agent_Rigidbody.angularVelocity);
    }


    // OnActionReceived : Unity.MLAgents�� Agent Ŭ������ ���ǵ� �Լ�, �� ���� ���̽� �ڵ�κ��� �ൿ ����� ���޹޾��� �� ������Ʈ�� �ൿ�� ó���ϰ� ������ ����, ���Ǽҵ� ���� ���θ� �����ϴ� �Լ�
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        Debug.Log("OnActionReceived");

        // �� ���� -0.01�� ������ �־�, ���� �������� �� ������ ������ ũ�� ��.
        // ���̽� �ڵ忡�� ���޵� �׼��� ActionBuffers ������ ����Ƽ �ڵ�� ���޵�.
        // ActionBuffers : Unity.MLAgents.Actuators �� ���ǵ� ����ü
        AddReward(-0.01f);

        var actions = actionBuffers.ContinuousActions;

        float moveX = Mathf.Clamp(actions[0], -1, 1f);
        float moveY = Mathf.Clamp(actions[1], -1, 1f);
        float moveZ = Mathf.Clamp(actions[2], -1, 1f);

        // ��� ����
        doScript.DriveInput(moveX);
        doScript.StrafeInput(moveY);
        doScript.LiftInput(moveZ);

        // ���� �� ���� ����
        // ��а� ������ ������ ���Ÿ� ���� ũ��
        float distance = Vector3.Magnitude(goalTrans.position - agentTrans.position);

        if (distance <= 0.5f)
        {
            // ������ ���
            SetReward(1f);
            EndEpisode();
        }
        else if(distance > 10f)
        {
            // ������ ���
            SetReward(-1f);
            EndEpisode();
        }
        else
        {
            // �̵� ���� �ƴ� ���, �Ÿ� ��ȭ����ŭ ������ �ް� preDist�� distance�� ������Ʈ �� �� ��� ����
            float reward = preDist - distance;
            SetReward(reward);
            preDist = distance;
        }

        //
    }

    // OnEpisodeBegin : Unity.MLAgents�� Agent Ŭ������ ���ǵ� ������ ���Ǽҵ尡 ������ �� ȯ���� �ʱ�ȭ�ϴ� �Լ�
    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin");

        //
        area.AreaSetting();

        preDist = Vector3.Magnitude(goalTrans.position - agentTrans.position);
    }

    // C# �� in ���� ���� �Ű������� ���۷����� ������. ���۷����� ���޹��� ActionBuffers ������ actionsOut�� ������� Ŀ���� �Է°��� ����
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("Heuristic");

        var continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
        continuousActionsOut[2] = Input.GetAxis("Mouse ScrollWheel");
    }

    public float DecisionWaitingTime = 5f;
    float m_currentTime = 0f;
    // WaitTimeInference �Լ������� DecisionRequest �Լ��� �߰���. DecisionRequest ȣ���� �޾ƾ� ���� �ൿ ������ �����ϱ� ����..
    public void WaitTimeInference(int action)
    {
        Debug.Log("WaitTimeInference");

        // Ŀ�´����̼��� �� �ǰ� ���� ���� ���� �ð��� ����ؼ� ���� �ð����� RequestDecision �Լ��� ȣ���ϰ� ��.
        if (Academy.Instance.IsCommunicatorOn)
            RequestDecision();
        else
        {
            if (m_currentTime >= DecisionWaitingTime)
            {
                m_currentTime = 0f;
                RequestDecision();
            }
            else
            {
                m_currentTime += Time.fixedDeltaTime;
            }
        }
    }


}
