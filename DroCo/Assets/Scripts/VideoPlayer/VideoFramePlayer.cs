using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoFramePlayer : Singleton<VideoFramePlayer> {

    public VideoPlayer VideoPlayer;

    private bool isSeeking = false;

    public int frameOffset = 0;

    private void Start() {
        VideoPlayer.prepareCompleted += OnVideoPrepared;
        VideoPlayer.frameReady += OnFrameReady;
    }

    public void LoadMission1() {
        string videoPath = Application.persistentDataPath + "/flightVideos/flight1.mp4";
        VideoPlayer.url = "file://" + videoPath;
        VideoPlayer.Prepare();
        frameOffset = 350;
    }

    public void LoadMission2() {
        string videoPath = Application.persistentDataPath + "/flightVideos/flight2.mp4";
        VideoPlayer.url = "file://" + videoPath;
        VideoPlayer.Prepare();
    }

    public void LoadMissionTraining() {
        string videoPath = Application.persistentDataPath + "/flightVideos/flight1.mp4";
        VideoPlayer.url = "file://" + videoPath;
        VideoPlayer.Prepare();
        frameOffset = 13400;
    }

    private void OnVideoPrepared(VideoPlayer vp) {
        VideoPlayer.Play();
        VideoPlayer.Pause(); // Start paused so we can seek
        Debug.Log("Frames + " + VideoPlayer.frameCount);
    }

    public void PlayFrame(float value) {
        if (!VideoPlayer.isPrepared)
            return;

        isSeeking = true;
        VideoPlayer.frame = (long) value + frameOffset;
        VideoPlayer.Play();
        VideoPlayer.Pause();
        isSeeking = false;
    }

    private void OnFrameReady(VideoPlayer vp, long frame) {

    }
}
