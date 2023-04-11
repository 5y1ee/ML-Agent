using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using TMPro;

public class GameArea : MonoBehaviour
{
    public List<int> nums;
    public int goalNum;
    public GameObject players;
    public TextMeshProUGUI numText;


    EnvironmentParameters m_ResetParams;


    void Start()
    {
        m_ResetParams = Academy.Instance.EnvironmentParameters;

        nums = new List<int>();
        for (int i=0; i< 10; i++)
        {
            nums.Add(i);
        }

    }

    void SetEnvironment()
    {
        ;

    }

    // 에피소드가 시작될 때마다 한 번씩 호출되는 함수
    public void AreaReset()
    {
        goalNum = Random.Range(0, nums.Count);
        numText.text = goalNum.ToString();

        //SetEnvironment();


    }



}
