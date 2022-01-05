using UnityEngine;

public class TestRef : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        int i = 0;
        Debug.Log(i);
        PassByRef(ref i);
        Debug.Log(i);
    }

    private void PassByRef(ref int i)
    {
        i++;
    }
}
