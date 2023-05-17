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

    public GameObject Arrow;
    public TextMeshProUGUI TableGold_Text;
    public TextMeshProUGUI GameNumber_Text;
    public TextMeshProUGUI Result_Text;
    public int gameNumber = 1;

    public CardAgent[] Players; // 플레이어 배열
    public bool[] PlayerConditions; // 플레이어 컨디션 배열
    public bool isDone;

    private int m_CardNumber = 52;  // 총 카드 수
    public int[] CardDeck = new int[53];    // 덱 배열 (0번은 빼놓고 1부터 1로 계산하도록)
    [SerializeField] private int gameTurn, playerTurn, curIdx, tableGold;   // 턴, 덱 인덱스, 판돈

    // Methods
    public int GetTurn { get { return gameTurn; } }
    public int GetPlayerTurn { get { return playerTurn; } set { playerTurn = value; } }
    public int GetIndex { get { return curIdx; } }
    public int PlayerNum { get { return Players.Length; } }
    public int TableGold { get { return tableGold; } set { tableGold = value; } }

    // Basic Methods
    void Start()
    {
        m_ResetParams = Academy.Instance.EnvironmentParameters;

        PlayerConditions = new bool[PlayerNum];
        gameNumber = 1;
        AreaReset();
    }


    // ML-Agents Methods
    void SetEnvironment()
    {

    }

    public void AreaReset()
    {
        isDone = false;
        gameTurn = 0;
        playerTurn = 0;
        DeckReset(CardDeck);
        ResetGold();

        for (int i = 0; i < Players.Length; i++)
        {
            Players[i].AgentIndex = i;
            PlayerConditions[i] = true;
            Players[i].GetComponent<MeshRenderer>().material.color = Color.white;
            //Debug.Log(i + " make white");
        }

        GameNumber_Text.text = "Game Number : " + gameNumber;

        PokerManager();
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
        Debug.Log("Deck Reset..");

        // Init
        DeckInit(Deck);

        // Shuffle
        int DeckLength = Deck.Length;
        for (int i = 1; i < DeckLength; i++)
        {
            int j = Mathf.FloorToInt(Random.Range(1, m_CardNumber + 1));
            int tmp = Deck[i];
            Deck[i] = Deck[j];
            Deck[j] = tmp;
        }

        curIdx = 1;
    }

    public int GetCard()
    {
        // 정상범위 초과 시 -1 리턴
        if (curIdx < 1 || curIdx >= m_CardNumber)
            return -1;
        else
            return CardDeck[curIdx++];
    }

    public void PokerManager()
    {
        Debug.Log("PokerManager");

        NextPlayer();

        ActionManager(playerTurn++);
        if (playerTurn == PlayerNum)
        {
            gameTurn++;
            playerTurn = 0;
        }

        NextPlayer();
        Players[playerTurn].GetComponent<MeshRenderer>().material.color = Color.red;
        //Debug.Log(playerTurn + " make red");

        // 처음 시작할 때 (playerturn == 0 && gameturn == 0) 게임턴만 증가
        // 끝날 때 (playerturn == playernum && gameturn == 5) 게임턴만 증가
        // 진행 중 일때 (gameturn != 0 && playerturn 1~5) 플레이어턴 증가, 5가 되면 게임턴 증가 후 플레이어턴 0 초기화

    }

    void ActionManager(int idx)
    {
        switch (gameTurn)
        {
            case 0:
                playerTurn = 0;
                gameTurn++;
                break;

            // 첫 턴, 플레이어에게 3장 씩 뿌림
            case 1:
                for (int i = 0; i < 3; i++)
                {
                    for (int k=0; k<PlayerNum; k++)
                    {
                        Players[k].TakeCard(GetCard());
                    }
                }
                    //CardtoPlayers();
                gameTurn++;
                playerTurn = 0;
                break;

            // 두 번째 턴, 총 4장
            case 2:
                Players[idx].TakeCard(GetCard());
                break;

            // 세 번째 턴, 총 5장
            case 3:
                Players[idx].TakeCard(GetCard());
                break;

            // 네 번째 턴, 총 6장
            case 4:
                Players[idx].TakeCard(GetCard());
                break;

            // 다섯 번째 턴, 총 7장
            case 5:
                Players[idx].TakeCard(GetCard());
                break;

            // 끝, 정산
            case 6:
                EndGame();
                break;

        }
    }

    void CardtoPlayers()
    {
        for (int i=0; i<PlayerNum; i++)
            Players[i].TakeCard(GetCard());
    }

    void EndGame()
    {
        List<int> winners = new List<int>();
        int winner = 0;
        int winnerRank = 0;
        
        for (int i=0; i<PlayerNum; ++i)
        {
            if (PlayerConditions[i] == false)
                continue;
            
            int playerRank = (int)Players[i].agentRank;

            if (playerRank > winnerRank)
            {
                winners.Clear();
                winners.Add(i);
                winner = i;
                winnerRank = playerRank;
            }
            else if (playerRank == winnerRank)
            {
                int cnt = Players[winner].AgentRankNum.Count;

                for (int k=0; k<cnt; ++k)
                {
                    Debug.Log(Players[winner].AgentRankNum[k] + " " + Players[i].AgentRankNum[k]);
                    if (Players[winner].AgentRankNum[k] > Players[i].AgentRankNum[k])
                    {
                        if (Players[i].AgentRankNum[k] == (int)CardNumber.Ace)
                        {
                            winners.Clear();
                            winners.Add(i);
                            winner = i;
                            break;
                        }
                        else
                            break;
                    }
                    else if (Players[winner].AgentRankNum[k] < Players[i].AgentRankNum[k])
                    {
                        if (Players[winner].AgentRankNum[k] == (int)CardNumber.Ace)
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
                        // DRAW....
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
        StartCoroutine(Wait5Sec(winners));

        //for (int i = 0; i < PlayerNum; i++)
        //{
        //    Players[i].EpisodeEnd(winner, tableGold);
        //}
        //gameNumber++;

        //AreaReset();  // 여기서 AreaReset 하지 않는 이유가 뭐더라..?

    }

    IEnumerator Wait5Sec(List<int> winners)
    {

        yield return new WaitForSeconds(5.0f);
        Result_Text.gameObject.SetActive(false);
        isDone = false;
        for (int i = 0; i < PlayerNum; i++)
        {
            Players[i].EpisodeEnd(winners, tableGold);
        }
        gameNumber++;
    }


    public void BetButtons(int val)
    {
        if (isDone)
            return;
        // Half = 0
        // Call = 1
        // Die = 2
        //Debug.Log(playerTurn + " make white");
        Players[playerTurn].GetComponent<MeshRenderer>().material.color = Color.white;
        Players[playerTurn].TakeAction(val);
    }

    public void PlayerDie(int idx)
    {
        //Debug.Log(idx + " make black");
        Players[idx].GetComponent<MeshRenderer>().material.color = Color.black;
        PlayerConditions[idx] = false;
        NextPlayer();
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

    public void NextPlayer()
    {
        int cnt = 0;
        for (int i=0; i<PlayerNum; i++)
        {
            if (PlayerConditions[i] == false)
                cnt++;
        }

        if (cnt == PlayerNum - 1)
        {
            EndGame();
            //gameTurn = 6;
            return;
        }
        else
        {
            while (PlayerConditions[playerTurn] == false)
            {
                playerTurn++;
                if (playerTurn == PlayerNum)
                {
                    gameTurn++;
                    playerTurn = 0;
                }
            }

        }

        Arrow.transform.position = Players[playerTurn].GetComponent<CardAgent>().GoldText.transform.position;

        if (gameTurn == 6)
            EndGame();

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
