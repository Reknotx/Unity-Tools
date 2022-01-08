using UnityEngine;

namespace Mesh_Generator.Scripts
{
    public class HideOnPlay : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            gameObject.SetActive(false);
        }
    
    }
}
