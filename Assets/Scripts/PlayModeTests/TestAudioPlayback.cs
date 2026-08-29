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
        Assert.AreEqual(Apu.SampleRate, source.clip.frequency);
        Assert.True(source.isPlaying);
        view.DebugInjectPulseTone();
        yield return new WaitForSecondsRealtime(0.5f);
        yield return new WaitForSecondsRealtime(1f);
        Debug.LogFormat("NES audio callbacks: reads={0}, nonZero={1}",
            view.AudioReadCount, view.AudioNonZeroReadCount);
        Assert.Greater(view.AudioReadCount, 0);
        Assert.Greater(view.AudioNonZeroReadCount, 0);
    }
}
