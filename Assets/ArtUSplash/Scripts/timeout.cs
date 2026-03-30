using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class timeout : MonoBehaviour
{
    public float timeoutTime = 90;

    private float nextTimeout = -1;

    private static GameObject instance;

    private bool m_didReceiveInput = true;

    void OnEnable()
    {
        InputSystem.onEvent += OnInputEvent;
    }

    void OnDisable()
    {
        InputSystem.onEvent -= OnInputEvent;
    }

    private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        m_didReceiveInput = true;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this.gameObject;
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);
        nextTimeout = Time.realtimeSinceStartup + timeoutTime;
    }


    private void Update()
    {
        if (m_didReceiveInput)
        {
            //Debug.LogWarning("Updating timeout.");

            nextTimeout = Time.realtimeSinceStartup + timeoutTime;
            m_didReceiveInput = false;
        }
        else if (Time.realtimeSinceStartup > nextTimeout)
        {
            //Debug.LogWarning("Timeout, exiting.");
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
