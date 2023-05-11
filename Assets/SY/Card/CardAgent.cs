using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using TMPro;
using System;

public partial class CardAgent : Agent
{
    EnvironmentParameters m_ResetParams;

    public CardArea Area;
    public TextMeshProUGUI HandText;
    public TextMeshProUGUI RankText;
    public TextMeshProUGUI GoldText;


    // 환경이 처음 시작될 때 한번만 호출되는 함수로 에이전트에 필요한 값들을 초기화
    public override void Initialize()
    {
        m_ResetParams = Academy.Instance.EnvironmentParameters;

        AgentGold(100);
        Hand = new List<int>();
        Hand_Num = new int[13];
        Hand_Pic = new int[4];
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
    [SerializeField] int[] Hand_Num, Hand_Pic;
    [SerializeField] int Hand_cnt;
    [SerializeField] List<List<int>> OppHands;
    [SerializeField] List<List<int>> OppBets;
    [SerializeField] List<List<bool>> OppCondition;

    //
    public int AgentIndex { get { return AgentIdx; } set { AgentIdx = value; } }

    // Card Methods
    public void TakeCard(int val)
    {
        Hand.Add(val);
        Hand_Pic[val / 100]++;
        Hand_Num[(val % 100)-1]++;
        Hand_cnt = Hand.Count;

        HandRanking();

        string str = "";
        for (int i=0; i<Hand_cnt; i++)
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
        Array.Clear(Hand_Pic, 0, 4);
        Array.Clear(Hand_Num, 0, 13);
        Hand_cnt = 0;
        
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

    enum CardPicture { Spade, Diamond, Heart, Clover };
    enum HandRank {  };

    void HandRanking()
    {
        // Card Value : 1~13 / 101~113 / 201~213 / 301~313 (스 > 다 > 하 > 클)

        /*
        1. 탑 (17.4%)
        2. 원 페어 (43.8%)
        3. 투 페어 (23.5%)
        4. 트리플 (4.83)
        5. 스트레이트 (4.55%)
        6. 백 스트레이트 (0.45%) : A 2 3 4 5의 스트레이트 (1시작)
        7. 마운틴 (0.45%) : A K Q J 10의 스트레이트 (10 시작)
        8. 플러시 (3.03%)
        9. 풀 하우스 (2.60%)
        10. 포카드 (0.168%)
        11. 스트레이트 플러스 (0.0215%)
        12. 백 스트레이트 플러시 (0.0032%)
        13. 로열 스트레이트 플러스 (0.0032%)
        */

        // Pair Check
        int top = 0, pairCnt = 0, pairNum = 0, tripleCnt = 0, tripleNum = 0, quadra = 0;
        for (int i=0; i<Hand_Num.Length; i++)
        {
            switch(Hand_Num[i])
            {
                case 1:
                    if (top != 1)
                        top = i + 1;
                    break;

                case 2:
                    if (pairNum != 1)
                        pairNum = i + 1;
                    pairCnt++;
                    break;

                case 3:
                    if (tripleNum != 1)
                        tripleNum = i + 1;
                    tripleCnt++;
                    break;

                case 4:
                    quadra = i + 1;
                    break;
            }

        }

        // Straight Check 10 > A > 9876...2
        // 최적화 가능?
        int straightCnt = 0, straightNum = 0;
        double[] straightProb = new double[10];
        for (int i=0; i<10; i++)
        {
            int tmp = 0;
            for (int k=i; k<i+5; k++)
            {
                if (k == 13)
                {
                    if (Hand_Num[0] > 0)
                        tmp++;
                }

                else if (Hand_Num[k] > 0)
                    tmp++;
                else
                {
                    ;
                }
            }


            if (tmp > straightCnt) straightCnt = tmp;
            else if (tmp == straightCnt)
            {
                // TODO
                // 달성 확률이 높은 straight 계산
            }

            if (straightCnt == 5)
                straightNum = i;

        }


        int flushCnt = 0; int flushPic = -1;
        for (int i=0; i<Hand_Pic.Length; i++)
        {
            if (flushCnt < Hand_Pic[i])
                flushCnt = Hand_Pic[i];
            if (flushCnt == 5)
                flushPic = i;
        }


        // Hand Rank
        if (flushCnt == 5 && straightCnt == 5)
        {
            for (int i=straightNum; i<straightNum+5; i++)
            {
                if (Hand[i] / 100 != flushPic)
                    break;
                if (i==straightNum+4)
                {
                    RankText.text = "Straight Flush";
                    return;
                }
            }
        }

        if (quadra > 0)
        {
            RankText.text = quadra + "Four Card";
            return;
        }

        if (tripleNum > 0 && pairNum > 0) 
        {
            RankText.text = tripleNum + "Full House";
            return;
        }

        if (flushPic > 0)
        {
            RankText.text = (CardPicture)flushPic + "Flush";
            return;
        }

        if (straightNum > 0)
        {
            RankText.text = straightNum + "Straight";
            return;
        }
        
        if (tripleNum > 0)
        {
            RankText.text = tripleNum + "Triple";
            return;
        }

        if (pairCnt > 1)
        {
            RankText.text = pairNum + "Two Pair";
            return;
        }

        if (pairCnt > 0)
        {
            RankText.text = pairNum + "One Pair";
            return;
        }

        RankText.text = top + "Top";
        return;


    }

}