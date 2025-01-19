using UnityEngine;

public class CameraWobble : MonoBehaviour
{
    private Vector3 initialPos;
    public Light light;

    private float lightIntensity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialPos = transform.position;
        lightIntensity = light.intensity;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float chance = Random.Range(0f, 1f);
        if (chance < 0.01f)
        {
            transform.position += Vector3.up * Random.Range(-0.03f, 0.03f);
            light.intensity = 0;
        }

        transform.position = Vector3.Lerp(transform.position, initialPos, Time.fixedDeltaTime * 5);
        light.intensity = Mathf.Lerp(light.intensity, lightIntensity, Time.fixedDeltaTime * 8);
    }
}
