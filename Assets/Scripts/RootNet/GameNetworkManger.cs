using UnityEngine;

using CCGKit;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance;

    public bool IsSinglePlayer;


    private void Awake()
    {
        Instance = this;

        //  원래 시작 씬 런처에서 호출해야하지만. 현재 디버그 모드에서는 호출 - 추후 삭제해야 함.
        GameManager.Instance.Initialize();
    }

    private void Start()
    {
        gameObject.GetComponent<DemoHumanPlayer>().OnStartLocalPlayer();
    }
}
