using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
//using UnityEditor.Build.Content;

// ȯ���� �����ϴ� ��ũ��Ʈ, ���Ǽҵ尡 ���۵� ������ ȯ���� ��ҵ��� �ʱ�ȭ
public class DroneSetting : MonoBehaviour
{
    public GameObject DroneAgent;
    public GameObject Goal;

    Vector3 areaInitPos, droneInitPos;
    Quaternion droneInitRot;

    EnvironmentParameters m_ResetParams;

    private Transform AreaTrans, DroneTrans, GoalTrans;

    private Rigidbody DroneAgent_Rigidbody;

    void Start()
    {
        Debug.Log(m_ResetParams);

        AreaTrans = gameObject.transform;
        DroneTrans = DroneAgent.transform;
        GoalTrans = Goal.transform;

        areaInitPos = AreaTrans.position;
        droneInitPos = DroneTrans.position;
        droneInitRot = DroneTrans.rotation;

        DroneAgent_Rigidbody = DroneAgent.GetComponent<Rigidbody>();       
    }

    public void AreaSetting()
    {
        Debug.Log("AreaSetting");

        DroneAgent_Rigidbody.velocity = Vector3.zero;
        DroneAgent_Rigidbody.angularVelocity = Vector3.zero;

        DroneTrans.position = droneInitPos;
        DroneTrans.rotation = droneInitRot;

        GoalTrans.position = areaInitPos + new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
    }

    void Update()
    {
        
    }
}
