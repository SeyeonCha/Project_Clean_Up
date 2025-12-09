using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public TMP_InputField NickNameInput;
    public GameObject DisconnectPanel;
    public GameObject RespawnPanel;


    void Awake()
    {
        Screen.SetResolution(960, 540, false);
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 30;
    }

    public void Connect() => PhotonNetwork.ConnectUsingSettings();

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.LocalPlayer.NickName = NickNameInput.text;
        PhotonNetwork.JoinOrCreateRoom("Room", new RoomOptions { MaxPlayers = 2 }, null);
    }

    public override void OnJoinedRoom()
    {
        DisconnectPanel.SetActive(false);
        //StartCoroutine("DestroyParts");
        Spawn();

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.StartMasterGameLogic();
        }
    }

    // IEnumerator DestroyParts()
    // {
    //     yield return new WaitForSeconds(0.2f);
    //     foreach (GameObject GO in GameObject.FindGameObjectsWithTag("Trash")) GO.GetComponent<PhotonView>().RPC("DestroyRPC", RpcTarget.All);
    // }

    public void Spawn()
    {
        PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);
        RespawnPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        DisconnectPanel.SetActive(true);
        RespawnPanel.SetActive(false);
    }
}
