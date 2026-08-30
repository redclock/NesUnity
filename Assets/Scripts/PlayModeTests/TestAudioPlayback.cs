using System.Collections;
using NesUnity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TestAudioPlayback
{
    [UnityTest]
    public IEnumerator SampleSceneStartsStreamingApuAudio()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        yield return null;

        NesScreenView view = Object.FindObjectOfType<NesScreenView>();
        Assert.IsNotNull(view);
        AudioSource source = view.GetComponent<AudioSource>();
        Assert.IsNotNull(source);
        Assert.IsNotNull(source.clip);
        Assert.AreEqual(view.AudioOutputSampleRate, source.clip.frequency);
        Assert.True(source.isPlaying);
        Assert.AreEqual(512, AudioSettings.GetConfiguration().dspBufferSize);
        // Runtime audio devices may reject the requested 44.1 kHz and stay at
        // a supported rate such as 48 kHz; APU and clip must follow that rate.
        Assert.AreEqual(view.AudioOutputSampleRate, AudioSettings.outputSampleRate);
        view.DebugInjectPulseTone();
        yield return new WaitForSecondsRealtime(0.5f);
        yield return new WaitForSecondsRealtime(1.5f);
        Debug.LogFormat("NES audio callbacks: reads={0}, nonZero={1}",
            view.AudioReadCount, view.AudioNonZeroReadCount);
        Debug.LogFormat("NES audio buffer: pending={0}, underruns={1}, overruns={2}, dsp={3}",
            view.AudioPendingSampleCount, view.AudioUnderrunCount, view.AudioOverrunCount,
            AudioSettings.GetConfiguration().dspBufferSize);
        Assert.Greater(view.AudioReadCount, 0);
        Assert.Greater(view.AudioNonZeroReadCount, 0);
        // Batchmode does not run Unity's audio and game threads in wall-clock
        // real time, so queue watermarks are only meaningful in Editor playback.
        if (!Application.isBatchMode)
        {
            Assert.AreEqual(0, view.AudioOverrunCount);
            Assert.GreaterOrEqual(view.AudioPendingSampleCount, 2940);
            Assert.LessOrEqual(view.AudioUnderrunCount, 1);
        }
    }
}
