using UnityEngine;

public class G1Start : MonoBehaviour
{
    [SerializeField] private GameObject gamestart;
    [SerializeField] private GameObject own;

    void Start()
    {
        if (gamestart == null || own == null)
        {
            Debug.LogError("Game objects not assigned in the inspector.");
            return;
        }

        gamestart.SetActive(false);
        own.SetActive(true);
    }

    public void Show()
    {
        if (gamestart == null || own == null) return;

        gamestart.SetActive(true);
        own.SetActive(false);
    }
}
