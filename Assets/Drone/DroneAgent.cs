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

        // Academy.Instance.AgentPreStep : 에이전트가 매 스텝을 수행하기 이전에 호출되는 일종의 이벤트, 발생할 때마다 작성한 WaitTimeInference 함수가 호출됨.
        Academy.Instance.AgentPreStep += WaitTimeInference;
    }

    // 매 스텝 에이전트에 대한 상태를 입력해 주는 함수,,
    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("CollectObservations");

        // .AddObservation : Unity.MLAgents.Sensors 에 정의되어있는 함수로 환경에서 사용하는 벡터 상태 값을 추가하는 함수
        // 이 값은 하나의 VectorSensor로 파이썬 코드로 전달되며, 이 벡터의 값들을 유니티 ML-Agents의 벡터 관측으로 이용

        // 거리벡터
        sensor.AddObservation(agentTrans.position - goalTrans.position);

        // 속도벡터
        sensor.AddObservation(agent_Rigidbody.velocity);

        // 각속도벡터
        sensor.AddObservation(agent_Rigidbody.angularVelocity);
    }


    // OnActionReceived : Unity.MLAgents의 Agent 클래스에 정의된 함수, 매 스텝 파이썬 코드로부터 행동 결과를 전달받았을 때 에이전트의 행동을 처리하고 보상을 제공, 에피소드 종료 여부를 결정하는 함수
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        Debug.Log("OnActionReceived");

        // 매 스텝 -0.01의 보상을 주어, 빨리 도달했을 때 보상의 총합을 크게 함.
        // 파이썬 코드에서 전달된 액션은 ActionBuffers 구조로 유니티 코드로 전달됨.
        // ActionBuffers : Unity.MLAgents.Actuators 에 정의된 구조체
        AddReward(-0.01f);

        var actions = actionBuffers.ContinuousActions;

        float moveX = Mathf.Clamp(actions[0], -1, 1f);
        float moveY = Mathf.Clamp(actions[1], -1, 1f);
        float moveZ = Mathf.Clamp(actions[2], -1, 1f);

        // 드론 제어
        doScript.DriveInput(moveX);
        doScript.StrafeInput(moveY);
        doScript.LiftInput(moveZ);

        // 보상 및 게임 종료
        // 드론과 목적지 사이의 상대거리 벡터 크기
        float distance = Vector3.Magnitude(goalTrans.position - agentTrans.position);

        if (distance <= 0.5f)
        {
            // 성공한 경우
            SetReward(1f);
            EndEpisode();
        }
        else if(distance > 10f)
        {
            // 실패한 경우
            SetReward(-1f);
            EndEpisode();
        }
        else
        {
            // 이도 저도 아닌 경우, 거리 변화량만큼 보상을 받고 preDist를 distance로 업데이트 한 후 계속 진행
            float reward = preDist - distance;
            SetReward(reward);
            preDist = distance;
        }

        //
    }

    // OnEpisodeBegin : Unity.MLAgents의 Agent 클래스에 정의돼 있으며 에피소드가 시작할 때 환경을 초기화하는 함수
    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin");

        //
        area.AreaSetting();

        preDist = Vector3.Magnitude(goalTrans.position - agentTrans.position);
    }

    // C# 의 in 예약어가 붙은 매개변수는 레퍼런스를 전달함. 레퍼런스로 전달받은 ActionBuffers 형태의 actionsOut에 사용자의 커스텀 입력값을 저장
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
    // WaitTimeInference 함수에서는 DecisionRequest 함수를 추가함. DecisionRequest 호출을 받아야 실제 행동 수행이 가능하기 때문..
    public void WaitTimeInference(int action)
    {
        Debug.Log("WaitTimeInference");

        // 커뮤니케이션이 안 되고 있을 때는 직접 시간을 계산해서 일정 시간마다 RequestDecision 함수를 호출하게 됨.
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
