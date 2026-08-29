using NesUnity;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public class TestAudioOutput
{
    [Test]
    public void TestSampleSceneHasAudioOutputConfiguration()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        NesScreenView view = Object.FindObjectOfType<NesScreenView>();
        Assert.IsNotNull(view);
        AudioSource source = view.GetComponent<AudioSource>();
        Assert.IsNotNull(source);
        Assert.False(source.playOnAwake);
        Assert.True(source.loop);
        Assert.AreEqual(0f, source.spatialBlend);
        Assert.IsNotNull(Object.FindObjectOfType<AudioListener>());
    }
}
