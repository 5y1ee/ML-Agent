using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using TMPro;
using System;
using static CardAgent;

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
        RoundGold = 0;
        Remain_Cards = new int[53];
        Hand = new List<int>();
        Hand_Num = new int[13];
        Hand_Pic = new int[4];
        OppHands = new List<List<int>>();
        OppBets = new List<List<int>>();
        OppCondition = new List<List<bool>>();

        AgentReset();

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

    // 에이전트에게 전달할 벡터 관측 정보의 요소들을 결정 (필수 요소#1)
    public override void CollectObservations(VectorSensor sensor)
    {

        sensor.AddObservation(1);
    }

    // (필수 요소#2)
    public override void OnActionReceived(ActionBuffers actions)
    {

        AddReward(-0.01f);

    }

    private void ProvideReward(int num)
    {

    }

    // 에피소드가 시작될 때마다 호출되는 함수 (필수 요소#3)
    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin");

        AgentReset();

        if(AgentIdx == Area.PlayerNum-1)
            Area.AreaReset();
    }



}

public partial class CardAgent
{
    public enum AgentType { Auto, Heuristic, TypeEnd };
    public enum AgentCondition { Alive, Dead, ConditionEnd };
    enum CardPicture { Spade, Diamond, Heart, Clover };
    public enum HandRank
    {
        None,
        TOP = 1,
        ONEPAIR = 2,
        TWOPAIR = 3,
        TRIPLE = 4,
        STRAIGHT = 5,
        FLUSH = 6,
        FULLHOUSE = 7,
        FOURCARD = 8,
        STRAIGHTFLUSH = 9,
        RANKEND
    };
    public enum CardNumber
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        NUMBEREND
    }

    public AgentType agentType = AgentType.Auto;
    public AgentCondition agentCondition = AgentCondition.Alive;
    public HandRank agentRank = HandRank.None;

    [SerializeField] int Gold, RoundGold, AgentIdx;
    [SerializeField] double WinRate;
    [SerializeField] List<int> Hand, Rank_Num;
    [SerializeField] int[] Hand_Num, Hand_Pic, Remain_Cards;
    [SerializeField] int Hand_cnt;
    [SerializeField] List<List<int>> OppHands;
    [SerializeField] List<List<int>> OppBets;
    [SerializeField] List<List<bool>> OppCondition;

    //
    public int AgentIndex { get { return AgentIdx; } set { AgentIdx = value; } }

    public List<int> AgentRankNum { get { return Rank_Num; } }

    // Card Methods
    public void TakeCard(int val)
    {
        int picVal = val / 100, numVal = val % 100;
        Hand.Add(val);
        Hand_Pic[picVal]++;
        Hand_Num[(numVal) - 1]++;
        Hand_cnt = Hand.Count;
        Remain_Cards[picVal * 13 + numVal] = 0;

        string str = "";
        for (int i = 0; i < Hand_cnt; i++)
        {
            str += Hand[i].ToString() + ", ";
        }
        HandText.text = str;
    }

    public void Calculate()
    {
        CountCard();
        HandRanking();
    }

    public void TakeAction(int val)
    {
        //Debug.Log("Click..");

        switch (val)
        {
            // Half
            case 0:
                Area.BetGold(2);
                RoundGold += 2;
                AgentGold(-2);
                break;

            // Call
            case 1:
                Area.BetGold(1);
                RoundGold += 1;
                AgentGold(-1);
                break;

            // Die
            case 2:
                agentCondition = AgentCondition.Dead;
                Area.PlayerDie(AgentIdx);
                return;
        }

    }


    private void OnMouseDown()
    {
        if (Area.GetPlayerTurn == AgentIdx)
        {
            Debug.Log("Click.....");
            //TakeAction(0);
        }

    }

    void AgentReset()
    {
        agentCondition = AgentCondition.Alive;
        agentRank = HandRank.None;

        RoundGold = 0;

        Area.DeckInit(Remain_Cards);
        Hand.Clear();
        Rank_Num.Clear();
        Array.Clear(Hand_Pic, 0, 4);
        Array.Clear(Hand_Num, 0, 13);
        Hand_cnt = 0;

        OppHands.Clear();
        OppBets.Clear();
        OppCondition.Clear();

        HandText.text = "QWER";

    }

    public void EpisodeEnd(List<int> winners, int gold)
    {
        int cnt = winners.Count;

        if(winners.Contains(AgentIdx))
        {
            AgentGold(gold / cnt);
            SetReward(gold / cnt);
        }
        else
            SetReward(-1 * RoundGold);

        EndEpisode();
    }

    public void AgentGold(int gold)
    {
        Gold += gold;
        GoldText.text = "Gold : " + Gold.ToString();
    }


    // Poker Methods

    void CountCard()
    {
        int _cnt = Area.PlayerAgents.Count;
        if (_cnt == 7) return;  // 마지막 카드는 히든 // 이미 상대의 1,2번 째 카드는 카운팅하고 있지 않음

        for (int i=0; i< _cnt; i++)
        {
            if (i == AgentIdx) continue;
            if (Area.PlayerAgents[i]._agentCondition == true)
            {
                int val = Area.PlayerAgents[i]._agentHands[Hand_cnt - 1];
                val = (val / 100) * 13 + (val % 100);
                Remain_Cards[val] = 0;
            }
        }


    }

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
        Rank_Num.Clear();
        // Pair Check pairNum = 0, tripleNum = 0, 
        int top = 0, pairCnt = 0, tripleCnt = 0, quadra = 0;
        List<int> pairNums = new List<int>();
        List<int> tripleNums = new List<int>();

        for (int i = 0; i < Hand_Num.Length; i++)
        {
            int num = Hand_Num[i];

            switch (num)
            {
                case 1:
                    if (top != (int)CardNumber.Ace)
                        top = i + 1;
                    break;

                case 2:
                    pairNums.Add(i + 1);
                    pairCnt++;

                    if (top != (int)CardNumber.Ace)
                        top = i + 1;

                    break;

                case 3:
                    tripleNums.Add(i + 1);
                    tripleCnt++;

                    if (top != (int)CardNumber.Ace)
                        top = i + 1;

                    break;

                case 4:
                    quadra = i + 1;

                    if (top != (int)CardNumber.Ace)
                        top = i + 1;

                    break;
            }

        }

        if (pairCnt > 0 && pairNums[0] == (int)CardNumber.Ace)
        {
            pairNums.RemoveRange(0, 1);
            pairNums.Add((int)CardNumber.Ace);
        }
        if (tripleCnt > 0 && tripleNums[0] == (int)CardNumber.Ace)
        {
            tripleNums.RemoveRange(0, 1);
            tripleNums.Add((int)CardNumber.Ace);
        }


        // Straight Check 10 > A > 9876...2
        // 최적화 가능?
        int straightCnt = 0, straightNum = 0;
        double[] straightProb = new double[10];
        for (int i = 0; i < 10; i++)
        {
            int tmp = 0;
            for (int k = i; k < i + 5; k++)
            {
                if (k == 13)
                {
                    if (Hand_Num[0] > 0)
                        tmp++;
                }

                else if (Hand_Num[k] > 0)
                    tmp++;
                
            }


            if (tmp > straightCnt)
            {
                straightCnt = tmp;
            }
            else if (tmp == straightCnt)
            {
                // TODO
                // 달성 확률이 높은 straight 계산
            }

            if (straightCnt == 5)
            {
                if (i == 10)
                    straightNum = (int)CardNumber.Ace;
                else
                    straightNum = i + 1;
            }

        }

        // Flush Check
        int flushCnt = 0; int flushPic = -1;
        for (int i = 0; i < Hand_Pic.Length; i++)
        {
            if (flushCnt < Hand_Pic[i])
                flushCnt = Hand_Pic[i];
            if (flushCnt == 5)
                flushPic = i;
        }


        // Hand Rank
        if (flushCnt == 5 && straightCnt == 5)
        {
            for (int i = straightNum; i < straightNum + 5; i++)
            {
                if (Hand[i] / 100 != flushPic)
                    break;
                if (i == straightNum + 4)
                {
                    for (int j = 4; j >= 0; --j)
                    {
                        Rank_Num.Add(straightNum + j);
                    }
                    agentRank = HandRank.STRAIGHTFLUSH;
                    RankText.text = (CardPicture)flushPic + straightNum + "Straight Flush";
                    return;
                }
            }
        }

        if (quadra > 0)
        {
            Rank_Num.Add(quadra);
            agentRank = HandRank.FOURCARD;
            RankText.text = quadra + "Four Card";
            return;
        }

        if (tripleCnt > 0 && pairCnt > 0 || tripleCnt > 1)
        {
            if (tripleCnt > 1)
            {
                Rank_Num.Add(tripleNums[tripleCnt - 1]);
                Rank_Num.Add(tripleNums[tripleCnt - 2]);
            }
            else
            {
                Rank_Num.Add(tripleNums[tripleCnt - 1]);
                Rank_Num.Add(pairNums[pairCnt - 1]);
            }
            agentRank = HandRank.FULLHOUSE;
            RankText.text = Rank_Num[0] + ", " + Rank_Num[1] + "Full House";
            return;
        }

        if (flushPic > 0)
        {
            Rank_Num.Add(top);
            agentRank = HandRank.FLUSH;
            RankText.text = (CardPicture)flushPic + "Flush";
            return;
        }

        if (straightNum > 0)
        {
            agentRank = HandRank.STRAIGHT;
            RankText.text = straightNum + "Straight";
            return;
        }
        
        if (tripleCnt > 0)
        {
            Rank_Num.Add(tripleNums[tripleCnt - 1]);
            agentRank = HandRank.TRIPLE;
            RankText.text = Rank_Num[0] + "Triple";
            return;
        }

        if (pairCnt > 1)
        {
            Rank_Num.Add(pairNums[pairCnt - 1]);
            Rank_Num.Add(pairNums[pairCnt - 2]);
            agentRank = HandRank.TWOPAIR;
            RankText.text = Rank_Num[0] + ", " + Rank_Num[1] + "Two Pair";
            return;
        }

        if (pairCnt > 0)
        {
            Rank_Num.Add(pairNums[pairCnt - 1]);
            agentRank = HandRank.ONEPAIR;
            RankText.text = Rank_Num[0] + "One Pair";
            return;
        }

        Rank_Num.Add(top);
        agentRank = HandRank.TOP;
        RankText.text = top + "Top";
        return;


    }

}