using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ActivateAllDisplays : MonoBehaviour
{
    private void Start() {
#if !UNITY_EDITOR
        Display.displays[1].Activate();
#endif
    }
}
