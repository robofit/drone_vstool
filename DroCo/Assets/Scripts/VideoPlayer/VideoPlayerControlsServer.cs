using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VideoPlayerControlsServer : VideoPlayerControls {

    private int changeStickCommandInterval = 0;

    public override void OnPlayButton() {
        base.OnPlayButton();

        StartCoroutine(PlayFlightLog());

        ExperimentManager.Instance.OnPlayButtonPressed();
    }

    private IEnumerator PlayFlightLog() {
        while (IsPlaying) {
            //changeStickCommandInterval++;
            //if (changeStickCommandInterval > 200) {
            //    changeStickCommandInterval = 0;
            //    ExperimentManager.Instance.SetNewStickConfiguration();
            //}

            progressBar.value += 1;
            if (progressBar.value == progressBar.maxValue) {
                progressBar.value = 0;
                OnPauseButton();
            }

            //yield return new WaitForSeconds(0.05f);
            yield return new WaitForSeconds(0.033f);
        }
    }

    public override void OnPlayBackward() {
        base.OnPlayBackward();

        StartCoroutine(PlayFlightLogBackward());
    }

    private IEnumerator PlayFlightLogBackward() {
        while (IsPlayingBackward) {

            progressBar.value -= 1;
            if (progressBar.value == 0) {
                OnPauseButton();
            }

            yield return new WaitForSeconds(0.033f);
        }
    }

    public override void OnPauseButton() {
        base.OnPauseButton();

        ExperimentManager.Instance.OnPauseButtonPressed();
    }

    public override void OnProgressBarValueChange(float value) {
        if (progressBar.value == progressBar.maxValue) {
            OnPauseButton();
        } else {
            //Debug.Log("Current frame: " + (int) value);
            ExperimentManager.Instance.SyncVideoPlayerControls((int) value);
            DroneFlightData currentLogMessage = FlightLogPlayerManager.Instance.PlayLogMessage((int) value);
            VideoFramePlayer.Instance.PlayFrame((int) value);
            if (currentLogMessage != null) {
                //ExperimentManager.Instance.SetNewStickConfiguration(currentLogMessage, FlightLogPlayerManager.Instance.GetLogMessagesInterval((int) value));
                ExperimentManager.Instance.SetNewStickConfiguration(currentLogMessage.sticks);
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
