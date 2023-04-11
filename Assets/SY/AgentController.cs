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
        //Debug.Log("Agent.Init");
        m_ResetParams = Academy.Instance.EnvironmentParameters;

        //Academy.Instance.DisableAutomaticStepping();
        //Academy.Instance.AutomaticSteppingEnabled = false;

    }

    // 에이전트에게 전달할 벡터 관측 정보의 요소들을 결정
    public override void CollectObservations(VectorSensor sensor)
    {
        //Debug.Log("Agent.Collect");

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
        //Debug.Log("Agent.OnAction");
        
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
        //Debug.Log("Agent.OnEpisodeBegin");
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


}
