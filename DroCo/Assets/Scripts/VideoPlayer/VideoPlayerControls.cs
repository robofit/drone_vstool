using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class VideoPlayerControls : MonoBehaviour {

    [SerializeField]
    protected Button playButton;
    [SerializeField]
    protected Button pauseButton;
    [SerializeField]
    protected Slider progressBar;

    private PlaybackControls controls;

    private Coroutine forwardCoroutine;
    private Coroutine backwardCoroutine;

    public bool IsPlaying {
        get; protected set;
    } = false;

    public bool IsPlayingBackward {
        get; protected set;
    } = false;

    private void Start() {
        playButton.gameObject.SetActive(true);
        pauseButton.gameObject.SetActive(false);
        progressBar.value = 0;
    }

    private void Awake() {
        controls = new PlaybackControls();

        controls.Playback.PlayPause.performed += OnPlayPause;
        controls.Playback.Forward.performed += OnForward;
        controls.Playback.Forward.canceled += OnForwardEnded;
        controls.Playback.Backward.performed += OnBackward;
        controls.Playback.Backward.canceled += OnBackwardEnded;
    }

    private void OnEnable() {
        controls.Enable();
    }

    private void OnDisable() {
        controls.Disable();
    }

    private void OnBackward(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        backwardCoroutine = StartCoroutine(GoBackward());
    }

    private void OnBackwardEnded(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        StopCoroutine(backwardCoroutine);
    }

    private IEnumerator GoBackward() {
        while (true) {
            if (progressBar.value > 0) {
                progressBar.value--;
            }
            yield return null;
        }
    }

    private void OnForward(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        forwardCoroutine = StartCoroutine(GoForward());
    }

    private void OnForwardEnded(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        StopCoroutine(forwardCoroutine);
    }

    private IEnumerator GoForward() {
        while (true) {
            if (progressBar.value < progressBar.maxValue) {
                progressBar.value++;
            }
            yield return null;
        }
    }

    private void OnPlayPause(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        if (IsPlaying) {
            OnPauseButton();
        } else {
            OnPlayButton();
        }
    }

    public virtual void OnPlayButton() {
        int maxValue = FlightLogPlayerManager.Instance.LoadedLogLines;

        if (maxValue > 0) {
            progressBar.maxValue = FlightLogPlayerManager.Instance.LoadedLogLines - 1;
            playButton.gameObject.SetActive(false);
            pauseButton.gameObject.SetActive(true);
            IsPlaying = true;
        }
    }

    public virtual void OnPauseButton() {
        playButton.gameObject.SetActive(true);
        pauseButton.gameObject.SetActive(false);
        IsPlaying = false;
        IsPlayingBackward = false;
    }

    public virtual void OnPlayBackward() {
        // Play only when progress bar already played some messages
        if (progressBar.value > 0) {
            IsPlayingBackward = true;
        }
    }

    public abstract void OnProgressBarValueChange(float value);

    public abstract void UpdateStatus(bool play);

    public abstract void UpdateProgressBar(int value);

}
