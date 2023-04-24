using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using TMPro;

public partial class CardAgent : Agent
{
    EnvironmentParameters m_ResetParams;

    public CardArea Area;
    public TextMeshProUGUI HandText;
    public TextMeshProUGUI GoldText;


    // 환경이 처음 시작될 때 한번만 호출되는 함수로 에이전트에 필요한 값들을 초기화
    public override void Initialize()
    {
        m_ResetParams = Academy.Instance.EnvironmentParameters;

        AgentGold(100);
        Hand = new List<int>();
        OppHands = new List<List<int>>();
        OppBets = new List<List<int>>();
        OppCondition = new List<List<bool>>();

        if (agentType == AgentType.Heuristic)
            Academy.Instance.AutomaticSteppingEnabled = false;

    }

    public void HeuristicAction(int action)
    {

        switch (action)
        {
            // die
            case 0:
                agentCondition = AgentCondition.Dead;
                break;

            // call
            case 1:
                break;

        }

        Academy.Instance.EnvironmentStep();

    }

    // 에이전트에게 전달할 벡터 관측 정보의 요소들을 결정
    public override void CollectObservations(VectorSensor sensor)
    {

        sensor.AddObservation(1);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        AddReward(-0.01f);

    }

    private void ProvideReward(int num)
    {

    }

    // 에피소드가 시작될 때마다 호출되는 함수
    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin");

        AgentReset();

        agentCondition = AgentCondition.Alive;
        if(AgentIdx == Area.PlayerNum-1)
            Area.AreaReset();
    }



}

public partial class CardAgent
{
    public enum AgentType { Auto, Heuristic, TypeEnd };
    public enum AgentCondition { Alive, Dead, ConditionEnd };

    public AgentType agentType = AgentType.Auto;
    public AgentCondition agentCondition = AgentCondition.Alive;

    [SerializeField] int Gold, AgentIdx;
    [SerializeField] double WinRate;
    [SerializeField] List<int> Hand;
    [SerializeField] List<List<int>> OppHands;
    [SerializeField] List<List<int>> OppBets;
    [SerializeField] List<List<bool>> OppCondition;

    //
    public int AgentIndex { get { return AgentIdx; } set { AgentIdx = value; } }

    // Card Methods
    public void TakeCard(int val)
    {
        Hand.Add(val);

        string str = "";
        for (int i=0; i<Hand.Count; i++)
        {
            str += Hand[i].ToString() + ", ";
        }
        HandText.text = str;
    }

    public void TakeAction(int val)
    {
        //Debug.Log("Click..");

        switch(val)
        {
            // Half
            case 0:
                Area.BetGold(2);
                AgentGold(-2);
                break;

            // Call
            case 1:
                Area.BetGold(1);
                AgentGold(-1);
                break;

            // Die
            case 2:
                agentCondition = AgentCondition.Dead;
                Area.PlayerDie(AgentIdx);
                return;
        }

        Area.PokerManager();
    }


    private void OnMouseDown()
    {
        if (Area.GetPlayerTurn == AgentIdx)
        {
            Debug.Log("Click.....");
            TakeAction(0);
        }

    }

    void AgentReset()
    {
        Hand.Clear();
        OppHands.Clear();
        OppBets.Clear();
        OppCondition.Clear();

        HandText.text = "QWER";

    }

    public void EpisodeEnd(int winner, int gold)
    {

        if (AgentIdx == winner)
        {
            AgentGold(gold);
            SetReward(1f);
        }
        else
            SetReward(-1f);

        EndEpisode();
    }

    public void AgentGold(int gold)
    {
        Gold += gold;
        GoldText.text = "Gold : " + Gold.ToString();
    }


    // Poker Methods

    void HandRanking()
    {
        // Card Value : 1~52
        // 1~13 / 14~26 / 27~39 / 40~52 (스 > 다 > 하 > 클)








    }

}