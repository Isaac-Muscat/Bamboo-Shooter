using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuPlay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerPrefs.SetInt("GrowthFactor", 6);
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void Shoot(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SceneManager.LoadScene("MainScene");
        }
    }
}
