using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using TMPro;

public class AgentController : Agent
{
    //public float timeBetweenDecisionsAtInference;
    //float m_TimeSinceDecision;
    //public Camera renderCamera;
    //VectorSensorComponent m_GoalSensor;
    EnvironmentParameters m_ResetParams;


    public GameArea area;
    public int goalNum, agentNum, diff, actionNum;
    public TextMeshProUGUI numText;


    // 환경이 처음 시작될 때 한번만 호출되는 함수로 에이전트에 필요한 값들을 초기화
    public override void Initialize()
    {
        Debug.Log("Agent.Init");
        m_ResetParams = Academy.Instance.EnvironmentParameters;

        //Academy.Instance.DisableAutomaticStepping();

        //Academy.Instance.AutomaticSteppingEnabled = false;

        // Academy.Instance.AgentPreStep : 에이전트가 매 스텝을 수행하기 이전에 호출되는 일종의 이벤트, 발생할 때마다 작성한 WaitTimeInference 함수가 호출됨.
        Academy.Instance.AgentPreStep += WaitTimeInference;
    }

    // 에이전트에게 전달할 벡터 관측 정보의 요소들을 결정
    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("Agent.Collect");

        sensor.AddObservation(goalNum);
        sensor.AddObservation(agentNum);
        sensor.AddObservation(diff);
        
    }

    //  게임 범위를 벗어나는 행동 값을 가질 수 없도록 마스킹한 행동 결과를 에이전트에 전달
    //public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    //{
    //    Debug.Log("Agent.Write");
        //base.WriteDiscreteActionMask(actionMask);
    //}

    // 행동을 환경에서 수행하는 역할
    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log("Agent.OnAction");
        
        AddReward(-0.01f);
        
        var action = actions.DiscreteActions[0];

        switch(action)
        {
            case 0:
                agentNum--;
                break;
            case 1:
                agentNum++;
                break;
        }

        //agentNum += action;
        diff = Mathf.Abs(agentNum - goalNum);

        //Debug.Log("Action is : " + action.ToString());
        numText.text = action.ToString();
        actionNum = action;
        
        if (diff == 0)
        {
            SetReward(1f);
            EndEpisode();
        }
        else if(diff >= 10)
        {
            SetReward(-1f);
            EndEpisode();
        }
        else
            SetReward(-1f * diff);

    }

    //
    private void ProvideReward(int num)
    {
        //Debug.Log("Agent.ProvideReward");
        if (goalNum == num)
        {
            SetReward(1f);
        }
        else
        {
            SetReward(-1f);
        }
    }

    // 
    //public override void Heuristic(in ActionBuffers actionsOut)
    //{
    //    Debug.Log("Agent.Heuristic");
    //    //base.Heuristic(actionsOut);
    //}

    // 에피소드가 시작될 때마다 호출되는 함수
    public override void OnEpisodeBegin()
    {
        Debug.Log("Agent.OnEpisodeBegin");
        area.AreaReset();

        goalNum = area.goalNum;
        agentNum = Random.Range(0, 10);
        diff = Mathf.Abs(agentNum - goalNum);

    }

    public void onClick()
    {
        //Agent.RequestDecision();
        Debug.Log("onClick");
        Academy.Instance.EnvironmentStep();
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
