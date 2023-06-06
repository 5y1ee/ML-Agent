using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using TMPro;
using JetBrains.Annotations;
using Unity.VisualScripting;
using System.Reflection;
using static CardAgent;

public class CardArea : MonoBehaviour
{
    EnvironmentParameters m_ResetParams;

    public enum PlayType { Default, Heuristic };

    public GameObject Arrow;
    public PlayType m_playType = PlayType.Default;
    public TextMeshProUGUI TableGold_Text;
    public TextMeshProUGUI GameNumber_Text;
    public TextMeshProUGUI Result_Text;
    [SerializeField]
    public int gameNumber = 1, m_playerNumber, m_playerAliveNumber, m_Bet;
    Color m_oriColor;

    public List<GameAgents> PlayerAgents;

    public bool isDone;

    private int m_CardNumber = 52;  // 총 카드 수
    public int[] CardDeck = new int[53];    // 덱 배열 (0번은 빼놓고 1부터 1로 계산하도록)
    [SerializeField] private int gameTurn, playerTurn, curIdx, tableGold;   // 턴, 덱 인덱스, 판돈

    // Methods
    public List<int> GetPlayerHands(int idx) { return PlayerAgents[idx]._agentHands;  }
    public int PlayerNum { get { return m_playerNumber; } }
    public int AlivePlayerNum { get { return m_playerAliveNumber; } }
    public int GetTurn { get { return gameTurn; } }
    public int GetPlayerTurn { get { return playerTurn; } set { playerTurn = value; } }
    public int GetIndex { get { return curIdx; } }
    public int TableGold { get { return tableGold; } set { tableGold = value; } }

    // Basic Methods
    void Start()
    {
        //Debug.Log(this.name + " Start");

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        TableGold_Text.transform.position = new Vector3(Camera.main.pixelRect.center.x, Camera.main.pixelRect.center.y + 100);
        GameNumber_Text.transform.position = TableGold_Text.transform.position + new Vector3(0, 300, 0);

        int _cnt = this.transform.childCount;
        int _idx = 0;
        PlayerAgents = new List<GameAgents>();
        for (int i=0; i<_cnt; i++)
        {
            if (this.transform.GetChild(i).gameObject.activeInHierarchy == false) continue;
            PlayerAgents.Add(new GameAgents());
            PlayerAgents[_idx]._agent = this.transform.GetChild(i).GetComponent<CardAgent>();
            PlayerAgents[_idx]._agent.AgentIndex = i;
            PlayerAgents[_idx]._agentCondition = true;
            PlayerAgents[_idx]._agent.OppInit();

            _idx++;
        }
        m_playerNumber = _idx;
        
        m_oriColor = PlayerAgents[0]._agent.transform.GetChild(0).GetComponent<MeshRenderer>().material.color;

        gameNumber = 1;


        if (m_playType == PlayType.Heuristic)
        {
            for (int i = 0; i < m_playerNumber; i++)
            {
                PlayerAgents[i]._agent.OnEpisodeBegin();
            }
        }

        //AreaReset();


    }

    // ML-Agents Methods
    void SetEnvironment()
    {

    }

    //
    public void CheckReady(int _agentIdx)
    {
        PlayerAgents[_agentIdx]._ready = true;

        int _cnt = 0;
        for (int i=0; i<m_playerNumber; i++)
        {
            if (PlayerAgents[i]._ready == true) ++_cnt;
            else
                break;
        }
        if (_cnt == m_playerNumber)
        {
            
            for (int i = 0; i < m_playerNumber; i++)
                PlayerAgents[i]._ready = false;
            AreaReset();
        }
    }

    // Area 환경을 리셋하는 함수 // OnEpisodeBegin에서 한 번, Start에서 한 번 호출한다.
    public void AreaReset()
    {
        //Debug.Log(this.name + " AreaReset");

        isDone = false;
        gameTurn = 1;
        playerTurn = 0;
        m_playerAliveNumber = m_playerNumber;
        DeckReset(CardDeck);
        ResetGold();

        for (int i = 0; i < PlayerNum; i++)
        {
            PlayerAgents[i]._agentHands.Clear();
            PlayerAgents[i]._agentCondition = true;
            //PlayerAgents[i]._agent.GetComponentInChildren<MeshRenderer>().material.color = m_oriColor;
            PlayerAgents[i]._agent.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = m_oriColor;
        }
        PlayerAgents[playerTurn]._agent.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = Color.red;
        Arrow.transform.position = PlayerAgents[playerTurn]._agent.GetComponent<CardAgent>().GoldText.transform.position;

        GameNumber_Text.text = "Game Number : " + gameNumber;

        PokerManager();

    }

    public void DeckInit(int[] Deck)
    {
        Deck[0] = -1;
        for (int i = 0; i < m_CardNumber; i++)
        {
            int pic = i / 13;
            int num = i % 13 + 1;
            Deck[i + 1] = 100 * pic + num;
        }
    }
    void DeckReset(int[] Deck)
    {
        //Debug.Log("Deck Reset..");

        // Init
        DeckInit(Deck);

        // Shuffle
        int DeckLength = Deck.Length;
        for (int i = 1; i < DeckLength; i++)
        {
            // UnityEngine.Random과 System.Random 이 존재한다.
            int j = Mathf.FloorToInt(UnityEngine.Random.Range(1, m_CardNumber + 1));
            int tmp = Deck[i];
            Deck[i] = Deck[j];
            Deck[j] = tmp;
        }

        curIdx = 1;
    }

    void GiveCard(int idx)
    {
        int val = GetCard();

        PlayerAgents[idx]._agentHands.Add(val);
        PlayerAgents[idx]._agent.TakeCard(val);
    }

    public int GetCard()
    {
        // 정상범위 초과 시 -1 리턴
        if (curIdx < 1 || curIdx >= m_CardNumber)
            return -1;
        else
            return CardDeck[curIdx++];
    }

    void PokerManager()
    {
        //Debug.Log("Poker Manager");

        for (int idx=0; idx<PlayerNum; idx++)
        {
            PlayerAgents[idx]._agent.AgentTurn = playerTurn;
            if (PlayerAgents[idx]._agentCondition == false)
                continue;
            else
            {
                switch (gameTurn)
                {
                    case 1: // 3장
                        for (int i = 0; i < 3; i++)
                            GiveCard(idx);
                        break;
                    case 2: // 4장
                    case 3: // 5장
                    case 4: // 6장
                    case 5: // 7장
                        GiveCard(idx);
                        break;

                    case 6: // 끝, 정산
                        EndGame();
                        return;
                }
                // switch
            }
            // if~else
        }
        // for

        // countcard 시켜야함
        for (int idx = 0; idx < PlayerNum; idx++)
            PlayerAgents[idx]._agent.Calculate();

        PlayerAgents[playerTurn]._agent.InvokeRequestor();

    }

    void EndGame()
    {
        if (isDone)
            return;

        //Debug.Log("[End Game]");

        List<int> winners = new List<int>();
        int winner = 0;
        int winnerRank = 0;
        
        for (int i=0; i<PlayerNum; ++i)
        {
            if (PlayerAgents[i]._agentCondition == false)
                continue;
            
            int playerRank = (int)PlayerAgents[i]._agent.agentRank;

            if (playerRank > winnerRank)
            {
                winners.Clear();
                winners.Add(i);
                winner = i;
                winnerRank = playerRank;
            }
            else if (playerRank == winnerRank)
            {
                int cnt = PlayerAgents[winner]._agent.AgentRankNum.Count;

                for (int k=0; k<cnt; ++k)
                {
                    if (PlayerAgents[winner]._agent.AgentRankNum[k] > PlayerAgents[i]._agent.AgentRankNum[k])
                    {
                        if (PlayerAgents[i]._agent.AgentRankNum[k] == (int)CardNumber.Ace)
                        {
                            winners.Clear();
                            winners.Add(i);
                            winner = i;
                            break;
                        }
                        else
                            break;
                    }
                    else if (PlayerAgents[winner]._agent.AgentRankNum[k] < PlayerAgents[i]._agent.AgentRankNum[k])
                    {
                        if (PlayerAgents[winner]._agent.AgentRankNum[k] == (int)CardNumber.Ace)
                            break;
                        else
                        {
                            winners.Clear();
                            winners.Add(i);
                            winner = i;
                            break;
                        }
                    }
                    if (k == cnt - 1)
                    {
                        Debug.Log("DRAW..");
                        winners.Add(i);

                    }
                }

            }
        }

        Result_Text.gameObject.SetActive(true);
        string winnerStr = "";
        foreach (int k in winners)
        {
            winnerStr += k.ToString();
            winnerStr += ", ";
        }
        Result_Text.text = winnerStr + " Player wins the game. +" + tableGold;

        isDone = true;

        //StartCoroutine(Wait5Sec(winners));
        EndSetting(winners);

    }

    IEnumerator Wait5Sec(List<int> winners)
    {
        yield return new WaitForSeconds(5.0f);

        EndSetting(winners);
    }

    void EndSetting(List<int> winners)
    {
        Result_Text.gameObject.SetActive(false);
        isDone = false;
        for (int i = 0; i < PlayerNum; i++)
            PlayerAgents[i]._agent.EpisodeEnd(winners, tableGold);

        gameNumber++;
    }

    public void BetButtons(int val)
    {
        if (isDone)
            return;

        //PlayerAgents[playerTurn]._agent.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = m_oriColor;
        //PlayerAgents[playerTurn]._agent.TakeAction(val);

        //PlayerAgents[playerTurn]._agent.HeuristicAction(val);
        //NextPlayer();

        m_Bet = val;
        PlayerAgents[playerTurn]._agent.func();

    }

    public void PlayerDie(int idx)
    {
        PlayerAgents[idx]._agent.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = Color.black;
        PlayerAgents[idx]._agentCondition = false;
        m_playerAliveNumber--;
    }


    public void NextPlayer()
    {
        //Debug.Log("Current Player is " + playerTurn);

        int dead_cnt = 0, formerPlayer = playerTurn;
        for (int i=0; i<PlayerNum; i++)
        {
            if (PlayerAgents[i]._agentCondition == false)
                dead_cnt++;
        }

        if (dead_cnt == PlayerNum - 1)
        {
            EndGame();
            return;
        }
        else
        {
            if (++playerTurn == PlayerNum)
            {
                gameTurn++;
                playerTurn = 0;
                PokerManager();
            }

            while (PlayerAgents[playerTurn]._agentCondition == false)
            {
                playerTurn++;
                if (playerTurn == PlayerNum)
                {
                    gameTurn++;
                    playerTurn = 0;
                    PokerManager();
                }
            }

        }

        for (int i=0; i<PlayerNum; ++i)
            PlayerAgents[i]._agent.AgentTurn = playerTurn;

        PlayerAgents[formerPlayer]._agent.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = m_oriColor;
        PlayerAgents[playerTurn]._agent.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = Color.red;
        Arrow.transform.position = PlayerAgents[playerTurn]._agent.GetComponent<CardAgent>().GoldText.transform.position;

        if (gameTurn == 6)
        {
            EndGame();
            return;
        }

        //Debug.Log("Next Player is " + playerTurn);
    }

    public void BetGold(int val)
    {
        tableGold += val;
        TableGold_Text.text = "TableGold : " + tableGold.ToString();
    }

    public void ResetGold()
    {
        tableGold = 0;
        TableGold_Text.text = "TableGold : " + tableGold.ToString();
    }

}

[System.Serializable]
public class GameAgents
{
    public CardAgent _agent;
    public bool _agentCondition;
    public List<int> _agentHands;
    public bool _ready;

    public GameAgents()
    {
        _agentHands = new List<int>();
        _ready = false;
        _agentCondition = false;
    }
}

[System.Serializable]
public class OpponentAgents
{
    public int AgentIdx;
    public RankProbability Probs;
    public List<int> Hands;
    public int[] Hand_Num, Hand_Pic;
    public List<int> Bets;

    public OpponentAgents(int _idx)
    {
        AgentIdx = _idx;
        Probs = new RankProbability();
        Hands = new List<int>();
        Bets = new List<int>();
        Hand_Num = new int[13];
        Hand_Pic = new int[4];
    }

    public void Reset()
    {
        Probs.ProbInit();
        Hands.Clear();
        Bets.Clear();
        Array.Clear(Hand_Num, 0, 13);
        Array.Clear(Hand_Pic, 0, 4);
    }
}

public class CircularPlayer<T>
{
    public T Data { get; set; }
    public CircularPlayer<T> Next { get; set; }

    public CircularPlayer(T data, CircularPlayer<T> next)
    {
        this.Data = data;
        this.Next = next;
    }

    private CircularPlayer<T> head;

    public void Add(CircularPlayer<T> player)
    {
        if (head == null)
        {
            head = player;
            head.Next = head;
        }
        else
        {
            player.Next = head;
        }
    }

    public CircularPlayer<T> GetNode(int index)
    {
        if (head == null) return null;
        int cnt = 0;
        var current = head;
        while(cnt < index)
        {
            current = current.Next;
            cnt++;
            if (current == head) return null;
        }

        return current;
    }

}
