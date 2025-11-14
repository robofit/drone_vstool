using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VideoPlayerControlsDummy : VideoPlayerControls {


    public override void OnPlayButton() {
        base.OnPlayButton();
    }

    public override void OnPauseButton() {
        base.OnPauseButton();
    }

    public void RequestPlay() {
        ExperimentManager.Instance.RequestPlayRpc();
    }

    public void RequestPause() {
        ExperimentManager.Instance.RequestPauseRpc();
    }

    public override void OnProgressBarValueChange(float value) {
        if (Application.isMobilePlatform) {
            if ((Touchscreen.current.touches.Count > 0) && EventSystem.current.IsPointerOverGameObject()) {
                ExperimentManager.Instance.RequestSeekRpc((int) value);
            }
        } else {
            if ((Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasPressedThisFrame) && EventSystem.current.IsPointerOverGameObject()) {
                ExperimentManager.Instance.RequestSeekRpc((int) value);
            }
        }
    }

    public override void UpdateStatus(bool play) {
        if (play) {
            OnPlayButton();
        } else {
            OnPauseButton();
        }
    }

    public override void UpdateProgressBar(int value) {
        progressBar.value = value;
    }
}
