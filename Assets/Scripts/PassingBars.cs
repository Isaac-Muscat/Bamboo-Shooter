using UnityEngine;

public class PassingBars : MonoBehaviour
{
    public GameObject bar;
    

    // Update is called once per frame
    void Update()
    {
        bar.transform.position -= Vector3.up*Time.deltaTime*8;
        if (bar.transform.position.y < -1) bar.transform.position = new Vector3(bar.transform.position.x, 6, bar.transform.position.z);
    }
}
