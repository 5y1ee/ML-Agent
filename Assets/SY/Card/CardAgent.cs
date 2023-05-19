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
        BACKSTRAIGHT = 6,
        MOUNTAIN = 7,
        FLUSH = 8,
        FULLHOUSE = 9,
        FOURCARD = 10,
        STRAIGHTFLUSH = 11,
        BACKSTRAIGHTFLUSH = 12,
        ROYALSTRAIGHTFLUSH = 13,
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
        Probability();
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
        Debug.Log("Click Agent" + AgentIdx);
        Calculate();
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

    // 테이블 위 공개 카드들 제외
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
                int val_idx = (val / 100) * 13 + (val % 100);
                Remain_Cards[val_idx] = 0;
            }
        }


    }

    // 자신의 패 계산
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
        int IdxOffset = 1;
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
                        top = i + IdxOffset;
                    break;

                case 2:
                    pairNums.Add(i + IdxOffset);
                    pairCnt++;

                    if (top != (int)CardNumber.Ace)
                        top = i + IdxOffset;

                    break;

                case 3:
                    tripleNums.Add(i + IdxOffset);
                    tripleCnt++;

                    if (top != (int)CardNumber.Ace)
                        top = i + IdxOffset;

                    break;

                case 4:
                    quadra = i + IdxOffset;

                    if (top != (int)CardNumber.Ace)
                        top = i + IdxOffset;

                    break;
            }

        }

        // 페어와 트리플 수 중에 Ace 포함 시 이를 가장 큰 수로 취급하게끔 순서를 앞으로 처리
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


        // Straight Check 10 > A > 9 > 8 > ... > 2
        int straightCnt = 5, straightNum = 0;
        for (int i = 0; i < (int)CardNumber.Ten; i++)
        {
            int tmp = 0;
            for (int k = i; k < i + straightCnt; k++)
            {
                if (Hand_Num[k] > 0)
                    tmp++;

                if (i == (int)CardNumber.Ten - IdxOffset && k == (int)CardNumber.King - IdxOffset)
                {
                    if (Hand_Num[0] > 0)
                        tmp++;
                    break;  // Hand_Num[13] 은 out of range
                }
            }

            if (straightCnt == tmp)
            {
                // 10JQKA는 10스트레이트(마운틴), A2345는 A스트레이트(백스트레이트), 나머지는 시작 숫자로 처리
                if (i == (int)CardNumber.Ten - IdxOffset)
                    straightNum = (int)CardNumber.Ten;
                else if (i == (int)CardNumber.Ace - IdxOffset)
                    straightNum = (int)CardNumber.Ace;
                else
                    straightNum = i + IdxOffset;
            }

        }

        // Flush Check
        int flushCnt = 5; int flushPic = -1;
        for (int i = 0; i < Hand_Pic.Length; i++)
        {
            if (flushCnt == Hand_Pic[i])
                flushPic = i;
        }


        // Hand Rank
        if (flushPic > 0 && straightNum > 0)
        {
            if (straightNum == (int)CardNumber.Ten && Hand_Num[0] / 100 == flushPic)
            {
                bool isRSF = true;
                for (int i = straightNum - IdxOffset; i < straightNum + straightCnt - 1; i++)
                {
                    if (Hand[i] / 100 != flushPic)
                    {
                        isRSF = false;
                        break;
                    }
                    
                }

                if (isRSF)
                {
                    Rank_Num.Add(straightNum);
                    agentRank = HandRank.ROYALSTRAIGHTFLUSH;
                    RankText.text = (CardPicture)flushPic + straightNum + "Royal Straight Flush";
                    return;
                }

            }

            bool isSF = true;
            for (int i = straightNum - IdxOffset; i < straightNum + straightCnt; i++)
            {
                if (Hand_Num[i] / 100 != flushPic)
                {
                    isSF = false;
                    break;
                }
            }

            if (isSF)
            {
                Rank_Num.Add(straightNum);
                if (straightNum == (int)CardNumber.Ace)
                {
                    agentRank = HandRank.BACKSTRAIGHTFLUSH;
                    RankText.text = (CardPicture)flushPic + straightNum + "Back Straight Flush";
                }
                else
                {
                    agentRank = HandRank.STRAIGHTFLUSH;
                    RankText.text = (CardPicture)flushPic + straightNum + "Straight Flush";
                }
                return;
            }

        }

        if (quadra > 0)
        {
            Rank_Num.Add(quadra);
            agentRank = HandRank.FOURCARD;
            RankText.text = quadra + "Four Card";
            return;
        }

        if ((tripleCnt > 0 && pairCnt > 0) || tripleCnt > 1)
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
            switch(straightNum)
            {
                case (int)CardNumber.Ten:
                    agentRank = HandRank.MOUNTAIN;
                    RankText.text = straightNum + "Mountain";
                    break;
                case (int)CardNumber.Ace:
                    agentRank = HandRank.BACKSTRAIGHT;
                    RankText.text = straightNum + "Back Straight";
                    break;
                default:
                    agentRank = HandRank.STRAIGHT;
                    RankText.text = straightNum + "Straight";
                    break;
            }
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

        if (pairCnt == 1)
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

    [SerializeField]
    List<double> PairList = new List<double>();

    void Probability()
    {
        double Pair = 0, TwoPair = 0,
            Triple = 0, Straight = 0,
            Flush = 0, FullHouse = 0,
            FourCard = 0, StaraightFlush = 0,
            Prob = 0;

        
        List<double> TwoPairList = new List<double>();
        List<double> TripleList = new List<double>();
        List<double> StraightList = new List<double>();

        ulong denominator = 0, numerator = 0;   // 분모, 분자

        List<int> TargetCnt = new List<int>();
        List<int> RequireCnt = new List<int>();
        int TotalCnt = Remain_Cards.Length - 1;
        int RemainCardCnt = Remain_Cards.Length - Area.GetIndex;
        int RemainTurn = 7 - Hand.Count;

        Debug.Log(RemainTurn + "<-turn, card->" + RemainCardCnt);
        denominator = Combination(RemainCardCnt, RemainTurn);

        // Pair Prob
        for (int i=0; i<13; i++)
        {
            Prob = 0;
            if (Hand_Num[i] + RemainTurn >= 2)
            {
                numerator = 0;
                TargetCnt.Clear();
                TargetCnt.Add(0);
                RequireCnt.Clear();
                RequireCnt.Add(2 - Hand_Num[i]);

                for (int idx = 1; idx <= TotalCnt; ++idx)
                {
                    if (Remain_Cards[idx] % 100 == i + 1)
                        ++TargetCnt[0];
                }
                Debug.Log("RequireCnt : " + RequireCnt[0] + ", TargetCnt : " + TargetCnt[0]);
                numerator = Calc_Numerator(RemainCardCnt, RequireCnt, TargetCnt, RemainTurn);
                Debug.Log("numerator : " + numerator + ", denominator : " + denominator);

                Prob = (double)numerator / denominator;
                PairList.Add(Prob);
            }
        }
        foreach(var item in PairList)
            Pair += item;

        /*

        // Two Pair Prob
        for (int i = 0; i < 12; i++)
        {
            for (int k = i + 1; k < 13; k++)
            {
                Prob = 0;
                if (2-Hand_Num[i] + 2-Hand_Num[k] <= RemainTurn)
                {
                    numerator = 0;
                    TargetCnt.Clear();
                    TargetCnt.Add(0);
                    TargetCnt.Add(0);
                    RequireCnt.Clear();
                    RequireCnt.Add(2 - Hand_Num[i]);
                    RequireCnt.Add(2 - Hand_Num[k]);

                    for (int idx = 1; idx <= TotalCnt; idx++)
                    {
                        if (Remain_Cards[idx] % 100 == i + 1)
                            ++TargetCnt[0];
                    }
                    for (int idx = 1; idx <= TotalCnt; idx++)
                    {
                        if (Remain_Cards[idx] % 100 == k + 1)
                            ++TargetCnt[1];
                    }
                    numerator = Calc_Numerator(RemainCardCnt, RequireCnt, TargetCnt, RemainTurn);

                    Prob = (double)numerator / denominator;
                    TwoPairList.Add(Prob);
                }
            }
        }
        foreach (var item in TwoPairList)
            TwoPair += item;

        // Triple Prob
        for (int i = 0; i < 13; i++)
        {
            Prob = 0;
            if (Hand_Num[i] + RemainTurn >= 3)
            {
                numerator = 0;
                TargetCnt.Clear();
                TargetCnt.Add(0);
                RequireCnt.Clear();
                RequireCnt.Add(3 - Hand_Num[i]);

                for (int idx = 1; idx <= TotalCnt; ++idx)
                {
                    if (Remain_Cards[idx] % 100 == i + 1)
                        ++TargetCnt[0];
                }
                numerator = Calc_Numerator(RemainCardCnt, RequireCnt, TargetCnt, RemainTurn);

                Prob = (double)numerator / denominator;
                TripleList.Add(Prob);
            }
        }
        foreach (var item in TripleList)
            Triple += item;

        // Straight Prob
        for (int i=0; i < 10; i++)
        {
            Prob = 0;
            
        }

         */


    }

    ulong Combination(int n, int r)
    {
        if (r == 0) return 1;
        if (n - r < r)
            r = n - r;

        ulong frac = 0, denominator = 1, numerator = 1;

        for (int i = n - r + 1; i <= n; i++) numerator *= (ulong)i;
        for (int i = 1; i <= r; i++) denominator *= (ulong)i;


        // 37C4
        // 37-4+1= 34*35*36*37 
        //Debug.Log("numerator : " + numerator + ", denominator : " + denominator);

        frac = numerator / denominator;

        return frac;
    }

    // A,K 2페어를 위해서는 A 1개, K 2개가 필요
    // RequireCnt => { 1, 2 } 필요한 A, K의 수
    // TargetCnt => { 4, 3 } 남아있는 A, K의 수
    // ReaminTurn => 4 앞으로 받을 수 있는 카드의 최대 수
    ulong Calc_Numerator(int TotalCnt, List<int> RequireCnt, List<int> TargetCnt, int RemainTurn)
    {
        ulong numerator = 0, achieve = 1;
        int cnt = TargetCnt.Count;
        int Require = 0;
        foreach (var item in RequireCnt) { Require += item; }


        for (int idx = 0; idx < cnt; idx++)
        {
            // 각 타겟 별 성취요건 경우의 수 곱하고,
            achieve *= Combination(TargetCnt[idx], RequireCnt[idx]);
        }
        // 성취요건 외의 남은 카드는 랜덤하게 뽑는 경우의 수 곱하면 되는거 아닌가?
        numerator = achieve * Combination(TotalCnt - Require, RemainTurn - Require);



        //for (int i = 0; i <= RemainTurn - Require; i++) // 필요한거 다 받고 남은 카드는 어떻게 채울 것인가?
        //{
        //    // 필요한 카드 수 C 몇개 받을건지? * 필요한 카드를 제외한 전체 C 몇개 받을건지?
        //    //numerator += Combination(TargetCnt, RemainTurn - i) * Combination(TotalCnt - TargetCnt, i);
        //}

        return numerator;
    }

}