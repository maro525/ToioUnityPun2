using UnityEngine;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using toio;

public class SimplePun : MonoBehaviourPunCallbacks
{
    public Cube cube;

    async void Start()
    {
        //旧バージョンでは引数必須でしたが、PUN2では不要です。
        PhotonNetwork.ConnectUsingSettings();

        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "player" + Random.Range(1, 99999);
        }

        Debug.Log("initialized pun2");

        var peripheral = await new NearestScanner().Scan();
        cube = await new CubeConnecter().Connect(peripheral);

        Debug.Log("initialized toio");
    }

    public void ReturnAccess() {
        Debug.Log("Simple Pun Class Access Succeeded!");
    }

    void OnGUI()
    {
        //ログインの状態を画面上に出力
        GUILayout.Label(PhotonNetwork.NetworkClientState.ToString());
    }


    //ルームに入室前に呼び出される
    public override void OnConnectedToMaster()
    {
        // "room"という名前のルームに参加する（ルームが無ければ作成してから参加する）
        PhotonNetwork.JoinOrCreateRoom("room", new RoomOptions(), TypedLobby.Default);
    }

    //ルームに入室後に呼び出される
    public override void OnJoinedRoom()
    {
        //キャラクターを生成
        GameObject chara= PhotonNetwork.Instantiate("chara", new Vector3(0.0f,0.5f,0.0f), Quaternion.identity, 0);
        //自分だけが操作できるようにスクリプトを有効にする
        CharaScript charaScript = chara.GetComponent<CharaScript>();
        charaScript.enabled = true;
    }

    public void moveToio() {
        cube.Move(50, 50, 200);
        Debug.Log(cube.x + " - " + cube.y);
    } 
}