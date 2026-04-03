using UnityEngine;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance;

    public bool IsSinglePlayer;


    private void Awake()
    {
        Instance = this;
    }
}
